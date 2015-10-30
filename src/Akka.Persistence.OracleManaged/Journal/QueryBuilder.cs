namespace Akka.Persistence.OracleManaged.Journal
{
    using System.Data;
    using System.Data.Common;
    using System.Text;

    using Akka.Persistence.Sql.Common.Journal;

    using Oracle.ManagedDataAccess.Client;

    internal class DefaultJournalQueryBuilder : IJournalQueryBuilder
    {
        private readonly string _schemaName;
        private readonly string _tableName;

        private readonly string _selectHighestSequenceNrSql;
        private readonly string _insertMessagesSql;

        public DefaultJournalQueryBuilder(string tableName, string schemaName)
        {
            _tableName = tableName;
            _schemaName = schemaName;

            _insertMessagesSql = "INSERT INTO {0}.{1} (PersistenceID, SequenceNr, IsDeleted, PayloadType, Payload) VALUES (@PersistenceId, @SequenceNr, @IsDeleted, @PayloadType, @Payload)"
                .QuoteSchemaAndTable(_schemaName, _tableName);
            _selectHighestSequenceNrSql = @"SELECT MAX(SequenceNr) FROM {0}.{1} WHERE CS_PID = ORA_HASH(@pid)".QuoteSchemaAndTable(_schemaName, _tableName);
        }

        public DbCommand SelectMessages(string persistenceId, long fromSequenceNr, long toSequenceNr, long max)
        {
            var sql = BuildSelectMessagesSql(fromSequenceNr, toSequenceNr, max);
            var command = new OracleCommand(sql)
            {
                Parameters = { PersistenceIdToSqlParam(persistenceId) }
            };

            return command;
        }

        public DbCommand SelectHighestSequenceNr(string persistenceId)
        {
            var command = new OracleCommand(_selectHighestSequenceNrSql)
            {
                Parameters = { PersistenceIdToSqlParam(persistenceId) }
            };

            return command;
        }

        public DbCommand InsertBatchMessages(IPersistentRepresentation[] messages)
        {
            var command = new OracleCommand(_insertMessagesSql);
            command.Parameters.Add("@PersistenceId", SqlDbType.NVarChar);
            command.Parameters.Add("@SequenceNr", SqlDbType.BigInt);
            command.Parameters.Add("@IsDeleted", SqlDbType.Char);
            command.Parameters.Add("@PayloadType", SqlDbType.NVarChar);
            command.Parameters.Add("@Payload", SqlDbType.VarBinary);

            return command;
        }

        public DbCommand DeleteBatchMessages(string persistenceId, long toSequenceNr, bool permanent)
        {
            var sql = BuildDeleteSql(toSequenceNr, permanent);
            var command = new OracleCommand(sql)
            {
                Parameters = { PersistenceIdToSqlParam(persistenceId) }
            };

            return command;
        }

        private string BuildDeleteSql(long toSequenceNr, bool permanent)
        {
            var sqlBuilder = new StringBuilder();

            if (permanent)
            {
                sqlBuilder.Append("DELETE FROM {0}.{1} ".QuoteSchemaAndTable(_schemaName, _tableName));
            }
            else
            {
                sqlBuilder.Append("UPDATE {0}.{1} SET IsDeleted = 'Y' ".QuoteSchemaAndTable(_schemaName, _tableName));
            }

            sqlBuilder.Append("WHERE CS_PID = ORA_HASH(@pid)");

            if (toSequenceNr != long.MaxValue)
            {
                sqlBuilder.Append(" AND SequenceNr <= ").Append(toSequenceNr);
            }

            var sql = sqlBuilder.ToString();
            return sql;
        }

        private string BuildSelectMessagesSql(long fromSequenceNr, long toSequenceNr, long max)
        {
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat(
                @"SELECT {0}
                    PersistenceID,
                    SequenceNr,
                    IsDeleted,
                    PayloadType,
                    Payload ", max != long.MaxValue ? "TOP " + max : string.Empty)
                .Append(" FROM {0}.{1} WHERE CS_PID = ORA_HASH(@pid)".QuoteSchemaAndTable(_schemaName, _tableName));

            // since we guarantee type of fromSequenceNr, toSequenceNr and max
            // we can inline them without risk of SQL injection

            if (fromSequenceNr > 0)
            {
                if (toSequenceNr != long.MaxValue)
                    sqlBuilder.Append(" AND SequenceNr BETWEEN ")
                        .Append(fromSequenceNr)
                        .Append(" AND ")
                        .Append(toSequenceNr);
                else
                    sqlBuilder.Append(" AND SequenceNr >= ").Append(fromSequenceNr);
            }

            if (toSequenceNr != long.MaxValue)
                sqlBuilder.Append(" AND SequenceNr <= ").Append(toSequenceNr);

            var sql = sqlBuilder.ToString();
            return sql;
        }

        private static OracleParameter PersistenceIdToSqlParam(string persistenceId, string paramName = null)
        {
            return new OracleParameter(paramName ?? "@pid", OracleDbType.NVarchar2, persistenceId.Length) { Value = persistenceId };
        }
    }
}