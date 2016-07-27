using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Sql.Common.Journal;
using Akka.Persistence.Sql.Common.Queries;
using Devart.Data.Oracle;
#pragma warning disable 612
#pragma warning disable 618

namespace Akka.Persistence.Devart.Oracle.Journal
{
    //TODO: END THE TERRIBLE INHERITANCE/IMPLEMENTATION ABUSE
    //Here's the deal; Some of the methods in the base class are NOT overridable, but we must change their behavior.
    //To do this we re-flag that we are implementing the interface to make them called by 
    //whatever is consuming this expecting IJournalQueryExecutor
    internal class OracleJournalQueryExecutor : AbstractQueryExecutor, IJournalQueryExecutor
    {
        private readonly string _createEventsJournalSql;
        private readonly string _createMetaTableSql;
        private readonly string _byTagSql;
        private readonly string _allPersistenceIdsSql;
        private readonly string _insertEventSql;
        private readonly string _byPersistenceIdSql;
        private readonly string _highestSequenceNrSql;
        private readonly string _deleteBatchSql;
        private readonly string _updateSequenceNrSql;
        public OracleJournalQueryExecutor(QueryConfiguration configuration,
            Akka.Serialization.Serialization serialization, ITimestampProvider timestampProvider)
            : base(configuration, serialization, timestampProvider)
        {
            var createStatement = String.Format(@"
                CREATE TABLE {0} (
                    {1} VARCHAR2(255) NOT NULL,
                    {2} NUMBER(18) NOT NULL,
                    {3} NUMBER(1) NOT NULL,
                    {4} VARCHAR2(255) NULL,
                    {5} NUMBER(18) NOT NULL,
                    {6} BLOB NOT NULL,
                    {7} VARCHAR2(2000) NULL,
                    PRIMARY KEY ({8}, {9})
                )", configuration.FullJournalTableName, configuration.PersistenceIdColumnName,
                configuration.SequenceNrColumnName, configuration.IsDeletedColumnName, configuration.ManifestColumnName,
                configuration.TimestampColumnName, configuration.PayloadColumnName, configuration.TagsColumnName,
                configuration.PersistenceIdColumnName, configuration.SequenceNrColumnName);


            _createEventsJournalSql = InternalExtensions.WrapOptimisticCreateIfNotExists(createStatement); 

            var createMetaStatement = String.Format(@"
                CREATE TABLE {0} (
                    {1} VARCHAR(255) NOT NULL,
                    {2} NUMBER(18) NOT NULL,
                    PRIMARY KEY ({3}, {4})
                )", configuration.FullMetaTableName, configuration.PersistenceIdColumnName,
                configuration.SequenceNrColumnName, configuration.PersistenceIdColumnName,
                configuration.SequenceNrColumnName);
            _createMetaTableSql = InternalExtensions.WrapOptimisticCreateIfNotExists(createMetaStatement);

            AllEventColumnNames = String.Format(@"e.{0} as PersistenceId, 
                e.{1} as SequenceNr, 
                e.{2} as Timestamp, 
                e.{3} as IsDeleted, 
                e.{4} as Manifest, 
                e.{5} as Payload", Configuration.PersistenceIdColumnName, Configuration.SequenceNrColumnName,
                Configuration.TimestampColumnName, Configuration.IsDeletedColumnName, Configuration.ManifestColumnName,
                Configuration.PayloadColumnName);

            AllEventColumnNamesforRownum = String.Format(@"rn.{0} as PersistenceId, 
                rn.{1} as SequenceNr, 
                rn.{2} as Timestamp, 
                rn.{3} as IsDeleted, 
                rn.{4} as Manifest, 
                rn.{5} as Payload", Configuration.PersistenceIdColumnName, Configuration.SequenceNrColumnName,
                Configuration.TimestampColumnName, Configuration.IsDeletedColumnName, Configuration.ManifestColumnName,
                Configuration.PayloadColumnName);

            _allPersistenceIdsSql = String.Format(@"SELECT DISTINCT e.{0} as PersistenceId FROM {1} e",
                Configuration.PersistenceIdColumnName, Configuration.FullJournalTableName);

            //CAST() is required for Oracle SQL on MAX for typing is to be correct for Provider
            _highestSequenceNrSql = String.Format(@"SELECT CAST(MAX(u.SeqNr) AS NUMBER(18)) as SequenceNr 
                FROM (
                    SELECT e.{0} as SeqNr FROM {1} e WHERE e.{2} = :PersistenceId
                    UNION
                    SELECT m.{3} as SeqNr FROM {4} m WHERE m.{5} = :PersistenceId) u",
                Configuration.SequenceNrColumnName, Configuration.FullJournalTableName,
                Configuration.PersistenceIdColumnName, Configuration.SequenceNrColumnName,
                Configuration.FullMetaTableName, Configuration.PersistenceIdColumnName);

            _deleteBatchSql = String.Format(@"DELETE FROM {0} 
                WHERE {1} = :PersistenceId AND {2} <= :ToSequenceNr", Configuration.FullJournalTableName,
                Configuration.PersistenceIdColumnName, Configuration.SequenceNrColumnName);

            _updateSequenceNrSql = String.Format(@"INSERT INTO {0} ({1}, {2}) 
                VALUES (:PersistenceId, :SequenceNr)", Configuration.FullMetaTableName,
                Configuration.PersistenceIdColumnName, Configuration.SequenceNrColumnName);

            _byPersistenceIdSql =
                String.Format(@"SELECT {0}
                FROM {1} e
                WHERE e.{2} = :PersistenceId
                AND e.{3} BETWEEN :FromSequenceNr AND :ToSequenceNr
                ORDER BY e.{3}", AllEventColumnNames,
                    Configuration.FullJournalTableName, Configuration.PersistenceIdColumnName,
                    Configuration.SequenceNrColumnName);

            var bytaginnerselect = string.Format(@"SELECT {0},
                row_number() over (ORDER BY {2},{3}) as skiptake
                FROM {1} e
                WHERE e.{2} LIKE :Tag
                ORDER BY {3}, {4}", AllEventColumnNames, Configuration.FullJournalTableName,
                    Configuration.TagsColumnName, Configuration.PersistenceIdColumnName,
                    Configuration.SequenceNrColumnName);
            _byTagSql = string.Format(@"select {0} from ({1}) rn where skiptake > :Skip and skiptake <= :Take",
                AllEventColumnNamesforRownum, bytaginnerselect);
                

            _insertEventSql = String.Format(@"INSERT INTO {0} (
                    {1},
                    {2},
                    {3},
                    {4},
                    {5},
                    {6},
                    {7}
                ) VALUES (
                    :PersistenceId, 
                    :SequenceNr,
                    :Timestamp,
                    :IsDeleted,
                    :Manifest,
                    :Payload,
                    :Tag
                )", Configuration.FullJournalTableName, Configuration.PersistenceIdColumnName,
                Configuration.SequenceNrColumnName, Configuration.TimestampColumnName, Configuration.IsDeletedColumnName,
                Configuration.ManifestColumnName, Configuration.PayloadColumnName, Configuration.TagsColumnName);



        }

        public string AllEventColumnNames { get; protected set; }

        public string AllEventColumnNamesforRownum { get; protected set; }

        protected override DbCommand CreateCommand(DbConnection connection)
        {
            return connection.CreateCommand();
        }

        protected override string ByTagSql
        {
            get { return _byTagSql; }
        }

        protected override string UpdateSequenceNrSql
        {
            get { return _updateSequenceNrSql; }
        }

        protected override string DeleteBatchSql
        {
            get { return _deleteBatchSql; }
        }

        protected override string HighestSequenceNrSql
        {
            get { return _highestSequenceNrSql; }
        }

        protected override string AllPersistenceIdsSql
        {
            get { return _allPersistenceIdsSql; }
        }

        protected override string InsertEventSql
        {
            get { return _insertEventSql; }
        }

        protected override string ByPersistenceIdSql
        {
            get { return _byPersistenceIdSql; }
        }

        protected override string CreateEventsJournalSql
        {
            get { return _createEventsJournalSql; }
        }

        protected override string CreateMetaTableSql
        {
            get { return _createMetaTableSql; }
        }


        public override async Task<ImmutableArray<string>> SelectAllPersistenceIdsAsync(DbConnection connection,
            CancellationToken cancellationToken)
        {
            try
            {


                using (var command = GetCommand(connection, AllPersistenceIdsSql))
                using (var reader = command.ExecuteReader())
                {
                    var builder = ImmutableArray.CreateBuilder<string>();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        builder.Add(reader.GetString(0));
                    }

                    return builder.ToImmutable();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                throw;
            }
        }

        

        public override async Task SelectByPersistenceIdAsync(DbConnection connection,
            CancellationToken cancellationToken, string persistenceId, long fromSequenceNr, long toSequenceNr,
            long max, Action<IPersistentRepresentation> callback)
        {
            using (var command = GetCommand(connection, ByPersistenceIdSql))
            {
                AddParameter(command, ":PersistenceId", DbType.String, persistenceId);
                AddParameter(command, ":FromSequenceNr", DbType.Int64, fromSequenceNr);
                AddParameter(command, ":ToSequenceNr", DbType.Int64, toSequenceNr);

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    var i = 0L;
                    while ((i++) < max && await reader.ReadAsync(cancellationToken))
                    {
                        var persistent = ReadEvent(reader);
                        callback(persistent);
                    }
                }
            }
        }

        public override async Task<long> SelectByTagAsync(DbConnection connection, CancellationToken cancellationToken,
            string tag, long fromOffset, long toOffset, long max,
            Action<ReplayedTaggedMessage> callback)
        {
            using (var command = GetCommand(connection, ByTagSql))
            {
                fromOffset = Math.Max(1, fromOffset);
                var take = Math.Min(toOffset - fromOffset, max);
                AddParameter(command, ":Tag", DbType.String, "%;" + tag + ";%");
                AddParameter(command, ":Skip", DbType.Int64, fromOffset - 1);
                AddParameter(command, ":Take", DbType.Int64, take);

                try
                {

                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    var maxSequenceNr = 0L;
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var persistent = ReadEvent(reader);
                        maxSequenceNr = Math.Max(maxSequenceNr, persistent.SequenceNr);
                        callback(new ReplayedTaggedMessage(persistent, tag, fromOffset));
                        fromOffset++;
                    }

                    return maxSequenceNr;
                }

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                    throw;
                }
            }
        }

        public override async Task<long> SelectHighestSequenceNrAsync(DbConnection connection,
            CancellationToken cancellationToken, string persistenceId)
        {
            using (var command = GetCommand(connection, HighestSequenceNrSql))
            {
                AddParameter(command, ":PersistenceId", DbType.String, persistenceId);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is long ? Convert.ToInt64(result) : 0L;
            }
        }

        public override async Task InsertBatchAsync(DbConnection connection, CancellationToken cancellationToken,
            WriteJournalBatch write)
        {
            using (var command = GetCommand(connection, InsertEventSql))
            using (var tx = connection.BeginTransaction())
            {
                command.Transaction = tx;

                foreach (var entry in write.EntryTags)
                {
                    var evt = entry.Key;
                    var tags = entry.Value;

                    WriteEvent(command, evt, tags);
                    await command.ExecuteScalarAsync(cancellationToken);
                    command.Parameters.Clear();
                }

                tx.Commit();
            }
        }

        public override async Task DeleteBatchAsync(DbConnection connection, CancellationToken cancellationToken,
            string persistenceId, long toSequenceNr)
        {
            using (var deleteCommand = GetCommand(connection, DeleteBatchSql))
            using (var highestSeqNrCommand = GetCommand(connection, HighestSequenceNrSql))
            {
                AddParameter(highestSeqNrCommand, ":PersistenceId", DbType.String, persistenceId);

                AddParameter(deleteCommand, ":PersistenceId", DbType.String, persistenceId);
                AddParameter(deleteCommand, ":ToSequenceNr", DbType.Int64, toSequenceNr);

                using (var tx = connection.BeginTransaction())
                {
                    deleteCommand.Transaction = tx;
                    highestSeqNrCommand.Transaction = tx;

                    var res = await highestSeqNrCommand.ExecuteScalarAsync(cancellationToken);
                    var highestSeqNr = res is long ? Convert.ToInt64(res) : 0L;

                    await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

                    if (highestSeqNr <= toSequenceNr)
                    {
                        using (var updateCommand = GetCommand(connection, UpdateSequenceNrSql))
                        {
                            updateCommand.Transaction = tx;

                            AddParameter(updateCommand, ":PersistenceId", DbType.String, persistenceId);
                            AddParameter(updateCommand, ":SequenceNr", DbType.Int64, highestSeqNr);

                            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                            tx.Commit();
                        }
                    }
                    else tx.Commit();
                }
            }
        }

        public override async Task CreateTablesAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using (var command = GetCommand(connection, CreateEventsJournalSql))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
                command.CommandText = CreateMetaTableSql;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        protected override void WriteEvent(DbCommand command, IPersistentRepresentation e, IImmutableSet<string> tags)
        {
            var manifest = string.IsNullOrEmpty(e.Manifest) ? e.Payload.GetType().AssemblyQualifiedName : e.Manifest;
            var serializer = Serialization.FindSerializerFor(e.Payload);
            var binary = serializer.ToBinary(e.Payload);

            AddParameter(command, ":PersistenceId", DbType.String, e.PersistenceId);
            AddParameter(command, ":SequenceNr", DbType.Int64, e.SequenceNr);
            var TS = TimestampProvider.GenerateTimestamp(e);
            AddParameter(command, ":Timestamp", DbType.Int64, TS);
            AddParameter(command, ":IsDeleted", DbType.Boolean, false);
            AddParameter(command, ":Manifest", DbType.String, manifest);
            AddParameter(command, ":Payload", DbType.Binary, binary);

            if (tags.Count != 0)
            {
                var tagBuilder = new StringBuilder(";", tags.Sum(x => x.Length) + tags.Count + 1);
                foreach (var tag in tags)
                {
                    tagBuilder.Append(tag).Append(';');
                }

                AddParameter(command, ":Tag", DbType.String, tagBuilder.ToString());
            }
            else AddParameter(command, ":Tag", DbType.String, DBNull.Value);
        }

        protected override IPersistentRepresentation ReadEvent(DbDataReader reader)
        {
            var persistenceId = reader.GetString(PersistenceIdIndex);
            var sequenceNr = reader.GetInt64(SequenceNrIndex);
            var timestamp = reader.GetInt64(TimestampIndex);
            var isDeleted = reader.GetBoolean(IsDeletedIndex);
            var manifest = reader.GetString(ManifestIndex);
            var payload = reader[PayloadIndex];

            var type = Type.GetType(manifest, true);
            var deserializer = Serialization.FindSerializerForType(type);
            var deserialized = deserializer.FromBinary((byte[]) payload, type);

            return new Persistent(deserialized, sequenceNr, persistenceId, manifest, isDeleted, ActorRefs.NoSender, null);
        }

        public new async Task SelectEventsAsync(DbConnection connection, CancellationToken cancellationToken, IEnumerable<IHint> hints, Action<IPersistentRepresentation> callback)

        {
            using (var command = GetCommand(connection, QueryEventsSql))
            {
                command.CommandText += string.Join(" AND ", hints.Select(h => HintToSql(h, command)));

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var persistent = ReadEvent(reader);
                            callback(persistent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                    throw;
                }
                
            }
        }

        private string HintToSql(IHint hint, DbCommand command)
        {
            if (hint is TimestampRange)
            {
                var range = (TimestampRange)hint;
                var sb = new StringBuilder();

                if (range.From.HasValue)
                {
                    sb.Append(" e.").Append(Configuration.TimestampColumnName).Append(" >= :TimestampFrom ");
                    AddParameter(command, ":TimestampFrom", DbType.Int64, range.From.Value);
                }
                if (range.From.HasValue && range.To.HasValue) sb.Append("AND");
                if (range.To.HasValue)
                {
                    sb.Append(" e.").Append(Configuration.TimestampColumnName).Append(" < :TimestampTo ");
                    AddParameter(command, ":TimestampTo", DbType.Int64, range.To.Value);
                }

                return sb.ToString();
            }
            if (hint is PersistenceIdRange)
            {
                var range = (PersistenceIdRange)hint;
                var sb = new StringBuilder(string.Format("( e.{0} IN (",Configuration.PersistenceIdColumnName));
                var i = 0;
                foreach (var persistenceId in range.PersistenceIds)
                {
                    var paramName = ":Pid" + (i++);
                    if (i<1 && i%1000 == 0) // LOL, Oracle
                    {
                        sb.AppendFormat(") or e.{0} IN ({1}", Configuration.PersistenceIdColumnName,paramName);
                    }
                    else if (i>1)
                    {
                        sb.AppendFormat(",{0}", paramName);
                    }
                    else
                    {
                        sb.AppendFormat("{0}", paramName);
                    }
                    AddParameter(command, paramName, DbType.String, persistenceId);
                }
                sb.Append(") )");
                return range.PersistenceIds.Count == 0
                    ? string.Empty
                    : sb.ToString();
            }
            else if (hint is WithManifest)
            {
                var manifest = (WithManifest)hint;
                AddParameter(command, ":Manifest", DbType.String, manifest.Manifest);
                return String.Format(" e.{0} = :Manifest", Configuration.ManifestColumnName);
            }
            else throw new NotSupportedException(String.Format("Oracle journal doesn't support query with hint [{0}]",
                hint.GetType()));
        }

    }
}