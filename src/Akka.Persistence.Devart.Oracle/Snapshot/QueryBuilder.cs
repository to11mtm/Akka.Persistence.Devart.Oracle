using System;
using System.Data;
using System.Data.Common;
using System.Text;
using Akka.Persistence.Sql.Common.Snapshot;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle.Snapshot
{
    internal class OracleSnapshotQueryBuilder : ISnapshotQueryBuilder
    {
        private readonly string _deleteSql;
        private readonly string _insertSql;
        private readonly string _selectSql;

        public OracleSnapshotQueryBuilder(OracleSnapshotSettings settings)
        {
            var schemaName = settings.SchemaName;
            var tableName = settings.TableName;
            _deleteSql = string.Format(@"DELETE FROM {0}.{1} WHERE PersistenceId = :PersistenceId ", schemaName, tableName);
            _insertSql = string.Format(@"INSERT INTO {0}.{1} (PersistenceId, SequenceNr, Timestamp, Manifest, Snapshot) VALUES (:PersistenceId, :SequenceNr, :Timestamp, :Manifest, :Snapshot)", schemaName, tableName);
            _selectSql = string.Format(@"SELECT PersistenceId, SequenceNr, Timestamp, Manifest, Snapshot FROM {0}.{1} WHERE PersistenceId = :PersistenceId",schemaName, tableName);
        }

        public DbCommand DeleteOne(string persistenceId, long sequenceNr, DateTime timestamp)
        {
            var oracleCommand = new OracleCommand();
            oracleCommand.Parameters.Add(new OracleParameter(":PersistenceId", OracleDbType.NVarChar, persistenceId.Length) { Value = persistenceId });
            var sb = new StringBuilder(_deleteSql);

            if (sequenceNr < long.MaxValue && sequenceNr > 0)
            {
                sb.Append(@"AND SequenceNr = :SequenceNr ");
                oracleCommand.Parameters.Add(new OracleParameter(":SequenceNr", OracleDbType.Number) { Value = sequenceNr });
            }

            if (timestamp > DateTime.MinValue && timestamp < DateTime.MaxValue)
            {
                sb.Append(@"AND Timestamp = :Timestamp");
                oracleCommand.Parameters.Add(new OracleParameter(":Timestamp", OracleDbType.TimeStampTZ) { Value = timestamp });
            }

            oracleCommand.CommandText = sb.ToString();

            return oracleCommand;
        }

        public DbCommand DeleteMany(string persistenceId, long maxSequenceNr, DateTime maxTimestamp)
        {
            var oracleCommand = new OracleCommand();
            oracleCommand.Parameters.Add(new OracleParameter(":PersistenceId", OracleDbType.NVarChar, persistenceId.Length) { Value = persistenceId });
            var sb = new StringBuilder(_deleteSql);

            if (maxSequenceNr < long.MaxValue && maxSequenceNr > 0)
            {
                sb.Append(@" AND SequenceNr <= :SequenceNr ");
                oracleCommand.Parameters.Add(new OracleParameter(":SequenceNr", OracleDbType.Number) { Value = maxSequenceNr });
            }

            if (maxTimestamp > DateTime.MinValue && maxTimestamp < DateTime.MaxValue)
            {
                sb.Append(@" AND Timestamp <= :Timestamp");
                oracleCommand.Parameters.Add(new OracleParameter(":Timestamp", OracleDbType.TimeStampTZ) { Value = maxTimestamp });
            }

            oracleCommand.CommandText = sb.ToString();

            return oracleCommand;
        }

        public DbCommand InsertSnapshot(SnapshotEntry entry)
        {
            var oracleCommand = new OracleCommand(_insertSql)
            {
                Parameters =
                {
                    new OracleParameter(":PersistenceId", OracleDbType.NVarChar, entry.PersistenceId.Length) { Value = entry.PersistenceId },
                    new OracleParameter(":SequenceNr", OracleDbType.Number) { Value = entry.SequenceNr },
                    new OracleParameter(":Timestamp", OracleDbType.TimeStampTZ) { Value = entry.Timestamp },
                    new OracleParameter(":Manifest", OracleDbType.NVarChar, entry.SnapshotType.Length) { Value = entry.SnapshotType },
                    new OracleParameter(":Snapshot", OracleDbType.LongRaw, entry.Snapshot.Length) { Value = entry.Snapshot }
                }
            };

            return oracleCommand;
        }

        public DbCommand SelectSnapshot(string persistenceId, long maxSequenceNr, DateTime maxTimestamp)
        {
            var oracleCommand = new OracleCommand();
            oracleCommand.Parameters.Add(new OracleParameter(":PersistenceId", OracleDbType.NVarChar, persistenceId.Length) { Value = persistenceId });

            var sb = new StringBuilder(_selectSql);
            if (maxSequenceNr > 0 && maxSequenceNr < long.MaxValue)
            {
                sb.Append(" AND SequenceNr <= :SequenceNr ");
                oracleCommand.Parameters.Add(new OracleParameter(":SequenceNr", OracleDbType.Number) { Value = maxSequenceNr });
            }

            if (maxTimestamp > DateTime.MinValue && maxTimestamp < DateTime.MaxValue)
            {
                sb.Append(" AND Timestamp <= :Timestamp ");
                oracleCommand.Parameters.Add(new OracleParameter(":Timestamp", OracleDbType.TimeStampTZ) { Value = maxTimestamp });
            }

            sb.Append(" ORDER BY SequenceNr DESC");
            oracleCommand.CommandText = sb.ToString();
            return oracleCommand;
        }
    }
}