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
            _deleteSql = string.Format(@"DELETE FROM {0}.{1} WHERE Persistence_Id = :Persistence_Id ", schemaName, tableName);
            _insertSql = string.Format(@"INSERT INTO {0}.{1} (Persistence_Id, Sequence_Nr, Time_stamp, Manifest, Snapshot) VALUES (:Persistence_Id, :Sequence_Nr, :Time_stamp, :Manifest, :Snapshot)", schemaName, tableName);
            _selectSql = string.Format(@"SELECT Persistence_Id, Sequence_Nr, Time_stamp, Manifest, Snapshot FROM {0}.{1} WHERE Persistence_Id = :Persistence_Id",schemaName, tableName);
        }

        public DbCommand DeleteOne(string persistenceId, long sequenceNr, DateTime timestamp)
        {
            var oracleCommand = new OracleCommand();
            oracleCommand.Parameters.Add(new OracleParameter(":Persistence_Id", OracleDbType.NVarChar, persistenceId.Length) { Value = persistenceId });
            var sb = new StringBuilder(_deleteSql);

            if (sequenceNr < long.MaxValue && sequenceNr > 0)
            {
                sb.Append(@"AND Sequence_Nr = :Sequence_Nr ");
                oracleCommand.Parameters.Add(new OracleParameter(":Sequence_Nr", OracleDbType.Number) { Value = sequenceNr });
            }

            if (timestamp > DateTime.MinValue && timestamp < DateTime.MaxValue)
            {
                sb.Append(@"AND Time_stamp = :Time_stamp");
                oracleCommand.Parameters.Add(new OracleParameter(":Time_stamp", OracleDbType.TimeStamp) { Value = timestamp });
            }

            oracleCommand.CommandText = sb.ToString();

            return oracleCommand;
        }

        public DbCommand DeleteMany(string persistenceId, long maxSequenceNr, DateTime maxTimestamp)
        {
            var oracleCommand = new OracleCommand();
            oracleCommand.Parameters.Add(new OracleParameter(":Persistence_Id", OracleDbType.NVarChar, persistenceId.Length) { Value = persistenceId });
            var sb = new StringBuilder(_deleteSql);

            if (maxSequenceNr < long.MaxValue && maxSequenceNr > 0)
            {
                sb.Append(@" AND Sequence_Nr <= :Sequence_Nr ");
                oracleCommand.Parameters.Add(new OracleParameter(":Sequence_Nr", OracleDbType.Number) { Value = maxSequenceNr });
            }

            if (maxTimestamp > DateTime.MinValue && maxTimestamp < DateTime.MaxValue)
            {
                sb.Append(@" AND Time_stamp <= :Time_stamp");
                oracleCommand.Parameters.Add(new OracleParameter(":Time_stamp", OracleDbType.TimeStamp) { Value = maxTimestamp });
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
                    new OracleParameter(":Persistence_Id", OracleDbType.NVarChar, entry.PersistenceId.Length) { Value = entry.PersistenceId },
                    new OracleParameter(":Sequence_Nr", OracleDbType.Number) { Value = entry.SequenceNr },
                    new OracleParameter(":Time_stamp", OracleDbType.TimeStamp) { Value = entry.Timestamp },
                    new OracleParameter(":Manifest", OracleDbType.NVarChar, entry.SnapshotType.Length) { Value = entry.SnapshotType },
                    new OracleParameter(":Snapshot", OracleDbType.Blob, entry.Snapshot.Length) { Value = entry.Snapshot }
                }
            };

            return oracleCommand;
        }

        public DbCommand SelectSnapshot(string persistenceId, long maxSequenceNr, DateTime maxTimestamp)
        {
            var oracleCommand = new OracleCommand();
            oracleCommand.Parameters.Add(new OracleParameter(":Persistence_Id", OracleDbType.NVarChar, persistenceId.Length) { Value = persistenceId });

            var sb = new StringBuilder(_selectSql);
            if (maxSequenceNr > 0 && maxSequenceNr < long.MaxValue)
            {
                sb.Append(" AND Sequence_Nr <= :Sequence_Nr ");
                oracleCommand.Parameters.Add(new OracleParameter(":Sequence_Nr", OracleDbType.Number) { Value = maxSequenceNr });
            }

            if (maxTimestamp > DateTime.MinValue && maxTimestamp < DateTime.MaxValue)
            {
                sb.Append(" AND Time_stamp <= :Time_stamp ");
                oracleCommand.Parameters.Add(new OracleParameter(":Time_stamp", OracleDbType.TimeStamp) { Value = maxTimestamp });
            }

            sb.Append(" ORDER BY Sequence_Nr DESC");
            oracleCommand.CommandText = sb.ToString();
            return oracleCommand;
        }
    }
}