using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Akka.Persistence.Sql.Common.Snapshot;
using Akka.Serialization;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle.Snapshot
{
    //TODO: END THE TERRIBLE INHERITANCE/IMPLEMENTATION ABUSE
    //Here's the deal; Some of the methods in the base class are NOT overridable, but we must change their behavior.
    //To do this we re-flag that we are implementing the interface to make them called by 
    //whatever is consuming this expecting ISnapshotQueryExecutor
    public class OracleSnapshotQueryExecutor : AbstractQueryExecutor, ISnapshotQueryExecutor
    {
        private readonly string _createSnapshotTableSql;
        private readonly string _insertSnapshotSql;
        private readonly string _deleteSnapshotRangeSql;
        private readonly string _deleteSnapshotSql;
        private readonly string _selectSnapshotSql;
        private readonly Akka.Serialization.Serialization _serialization;

        public OracleSnapshotQueryExecutor(QueryConfiguration configuration,
            Akka.Serialization.Serialization serialization) : base(configuration, serialization)
        {
            _serialization = serialization;
            var createStatement = String.Format(@"
                CREATE TABLE  {0} (
                    {1} VARCHAR2(255) NOT NULL,
                    {2} NUMBER(18) NOT NULL,
                    {3} NUMBER(18) NOT NULL,
                    {4} VARCHAR2(255) NOT NULL,
                    {5} BLOB NOT NULL,
                    PRIMARY KEY ({6}, {7})
                )", configuration.FullSnapshotTableName, configuration.PersistenceIdColumnName,
                configuration.SequenceNrColumnName, configuration.TimestampColumnName, configuration.ManifestColumnName,
                configuration.PayloadColumnName, configuration.PersistenceIdColumnName,
                configuration.SequenceNrColumnName);
            _createSnapshotTableSql = InternalExtensions.WrapOptimisticCreateIfNotExists(createStatement);

            
            
            _selectSnapshotSql = String.Format(@"
                SELECT {0},
                    {1}, 
                    {2}, 
                    {3}, 
                    {4}   
                FROM {5} 
                WHERE {6} = :PersistenceId 
                    AND {7} <= :SequenceNr
                    AND {8} <= :Timestamp
                ORDER BY {9} DESC", Configuration.PersistenceIdColumnName, Configuration.SequenceNrColumnName,
                Configuration.TimestampColumnName, Configuration.ManifestColumnName, Configuration.PayloadColumnName,
                Configuration.FullSnapshotTableName, Configuration.PersistenceIdColumnName,
                Configuration.SequenceNrColumnName, Configuration.TimestampColumnName,
                Configuration.SequenceNrColumnName);

            _deleteSnapshotSql = String.Format(@"
                DELETE FROM {0}
                WHERE {1} = :PersistenceId
                    AND {2} = :SequenceNr", Configuration.FullSnapshotTableName, Configuration.PersistenceIdColumnName,
                Configuration.SequenceNrColumnName);

            _deleteSnapshotRangeSql = String.Format(@"
                DELETE FROM {0}
                WHERE {1} = :PersistenceId
                    AND {2} <= :SequenceNr
                    AND {3} <= :Timestamp", Configuration.FullSnapshotTableName, Configuration.PersistenceIdColumnName,
                Configuration.SequenceNrColumnName, Configuration.TimestampColumnName);

            var saneInsertStatement = String.Format(@"
                INSERT INTO {0} (
                    {1}, 
                    {2}, 
                    {3}, 
                    {4}, 
                    {5}) VALUES (:PersistenceId, :SequenceNr, :Timestamp, :Manifest, :Payload);",
                Configuration.FullSnapshotTableName, Configuration.PersistenceIdColumnName,
                Configuration.SequenceNrColumnName, Configuration.TimestampColumnName, Configuration.ManifestColumnName,
                Configuration.PayloadColumnName);
            var saneUpdateStatement = String.Format(@"
                UPDATE {0}
                  SET {1} = :Timestamp, {2} = :Manifest, {3} = :Payload
                  WHERE {4} = :PersistenceId AND {5} = :SequenceNr;", Configuration.FullSnapshotTableName,
                configuration.TimestampColumnName, configuration.ManifestColumnName, configuration.PayloadColumnName,
                configuration.PersistenceIdColumnName, configuration.SequenceNrColumnName);

            var optimisticUpsert = InternalExtensions.AsOptimisticUpsert(saneInsertStatement, saneUpdateStatement);
            _insertSnapshotSql = optimisticUpsert;

        }
        

        protected override string SelectSnapshotSql
        {
            get { return _selectSnapshotSql; }
        }

        protected override string DeleteSnapshotSql
        {
            get { return _deleteSnapshotSql; }
        }

        protected override string DeleteSnapshotRangeSql
        {
            get { return _deleteSnapshotRangeSql; }
        }

        protected override string InsertSnapshotSql
        {
            get { return _insertSnapshotSql; }
        }

        protected override string CreateSnapshotTableSql
        {
            get { return _createSnapshotTableSql; }
        }

        protected override DbCommand CreateCommand(DbConnection connection)
        {
            return connection.CreateCommand();
        }

        protected override void SetTimestampParameter(DateTime timestamp, DbCommand command)
        {
            AddParameter(command, ":Timestamp", DbType.Int64, timestamp.Ticks);
        }




        protected override void SetSequenceNrParameter(long sequenceNr, DbCommand command)
        {
            AddParameter(command, ":SequenceNr", DbType.Int64, sequenceNr);
        }

        protected override void SetPersistenceIdParameter(string persistenceId, DbCommand command)
        {
            AddParameter(command, ":PersistenceId", DbType.String, persistenceId);
        }

        protected override void SetPayloadParameter(object snapshot, DbCommand command)
        {
            var snapshotType = snapshot.GetType();
            var serializer = _serialization.FindSerializerForType(snapshotType);

            var binary = serializer.ToBinary(snapshot);
            AddParameter(command, ":Payload", DbType.Binary, binary);
        }

        public new async Task CreateTableAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using (var command = GetCommand(connection, CreateSnapshotTableSql))
            using (var tx = connection.BeginTransaction())
            {
                command.Transaction = tx;
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.ToString());
                    tx.Rollback();
                    throw;
                }
                
                tx.Commit();
            }
        }


        protected override void SetManifestParameter(Type snapshotType, DbCommand command)
        {
            AddParameter(command, ":Manifest", DbType.String, snapshotType.AssemblyQualifiedName);
        }

        public new async Task InsertAsync(DbConnection connection, CancellationToken cancellationToken, object snapshot,
            SnapshotMetadata metadata)
        {
            await base.InsertAsync(connection, cancellationToken, snapshot, metadata);
        }

        public new async Task<SelectedSnapshot> SelectSnapshotAsync(DbConnection connection,
            CancellationToken cancellationToken, string persistenceId,
            long maxSequenceNr, DateTime maxTimestamp)
        {
            using (var command = GetCommand(connection, SelectSnapshotSql))
            {
                SetPersistenceIdParameter(persistenceId, command);
                SetSequenceNrParameter(maxSequenceNr, command);
                SetTimestampParameter(maxTimestamp, command);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return ReadSnapshot(reader);
                    }
                }
            }

            return null;
        }

        protected override SelectedSnapshot ReadSnapshot(DbDataReader reader)
        {
            var persistenceId = reader.GetString(0);
            var sequenceNr = reader.GetInt64(1);
            var timestamp = new DateTime(reader.GetInt64(2));

            var metadata = new SnapshotMetadata(persistenceId, sequenceNr, timestamp);
            var snapshot = GetSnapshot(reader);

            return new SelectedSnapshot(metadata, snapshot);
        }


        public new async Task DeleteAsync(DbConnection connection, CancellationToken cancellationToken, string persistenceId,
            long sequenceNr,
            DateTime? timestamp)
        {
            var sql = timestamp.HasValue
                ? DeleteSnapshotRangeSql + string.Format(" AND {0} = :Timestamp", Configuration.TimestampColumnName)
                : DeleteSnapshotSql;

            using (var command = GetCommand(connection, sql))
            using (var tx = connection.BeginTransaction())
            {
                command.Transaction = tx;

                SetPersistenceIdParameter(persistenceId, command);
                SetSequenceNrParameter(sequenceNr, command);

                if (timestamp.HasValue)
                {
                    SetTimestampParameter(timestamp.Value, command);
                }

                await command.ExecuteNonQueryAsync(cancellationToken);

                tx.Commit();
            }
        }
    }

}