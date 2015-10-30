namespace Akka.Persistence.OracleManaged
{
    using System;
    using Oracle.ManagedDataAccess.Client;

    internal static class OracleInitializer
    {
        private const string OracleJournalFormat = @"
            DECLARE
            --IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{2}' AND TABLE_NAME = '{3}')
            BEGIN
                CREATE TABLE {0}.{1} (
	                PersistenceID NVARCHAR(200) NOT NULL,
                    CS_PID AS (ORA_HASH(PersistenceID)),
	                SequenceNr NUMBER(19) NOT NULL,
	                IsDeleted CHAR(1) NOT NULL,
                    PayloadType NVARCHAR2(500) NOT NULL,
	                Payload LONG RAW NOT NULL,
                    CONSTRAINT PK_{3} PRIMARY KEY (PersistenceID, SequenceNr),
                    CONSTRAINT CK_{3}_IsDeleted IsDeleted IN ('Y','N')
                );
                CREATE INDEX IX_{3}_CS_PID ON {0}.{1}(CS_PID);
                CREATE INDEX IX_{3}_SequenceNr ON {0}.{1}(SequenceNr);
            END
            ";

        private const string OracleSnapshotStoreFormat = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{2}' AND TABLE_NAME = '{3}')
            BEGIN
                CREATE TABLE {0}.{1} (
	                PersistenceID NVARCHAR(200) NOT NULL,
                    CS_PID AS CHECKSUM(PersistenceID),
	                SequenceNr BIGINT NOT NULL,
                    Timestamp DATETIME2 NOT NULL,
                    SnapshotType NVARCHAR(500) NOT NULL,
	                Snapshot VARBINARY(MAX) NOT NULL
                    CONSTRAINT PK_{3} PRIMARY KEY (PersistenceID, SequenceNr)
                );
                CREATE INDEX IX_{3}_CS_PID ON {0}.{1}(CS_PID);
                CREATE INDEX IX_{3}_SequenceNr ON {0}.{1}(SequenceNr);
                CREATE INDEX IX_{3}_Timestamp ON {0}.{1}(Timestamp);
            END
            ";

        /// <summary>
        /// Initializes a Oracle journal-related tables according to 'schema-name', 'table-name' 
        /// and 'connection-string' values provided in 'akka.persistence.journal.oracle-managed' config.
        /// </summary>
        internal static void CreateOracleJournalTables(string connectionString, string schemaName, string tableName)
        {
            var sql = InitJournalSql(tableName, schemaName);
            ExecuteSql(connectionString, sql);
        }

        /// <summary>
        /// Initializes a Oracle snapshot store related tables according to 'schema-name', 'table-name' 
        /// and 'connection-string' values provided in 'akka.persistence.snapshot-store.oracle-managed' config.
        /// </summary>
        internal static void CreateOracleSnapshotStoreTables(string connectionString, string schemaName, string tableName)
        {
            var sql = InitSnapshotStoreSql(tableName, schemaName);
            ExecuteSql(connectionString, sql);
        }

        private static string InitJournalSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "Akka.Persistence.OracleManaged journal table name is required");
            schemaName = schemaName ?? "dbo";

            var cb = new OracleCommandBuilder();
            return string.Format(OracleJournalFormat, cb.QuoteIdentifier(schemaName), cb.QuoteIdentifier(tableName), cb.UnquoteIdentifier(schemaName), cb.UnquoteIdentifier(tableName));
        }

        private static string InitSnapshotStoreSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "Akka.Persistence.SqlServer snapshot store table name is required");
            schemaName = schemaName ?? "dbo";

            var cb = new OracleCommandBuilder();
            return string.Format(OracleSnapshotStoreFormat, cb.QuoteIdentifier(schemaName), cb.QuoteIdentifier(tableName), cb.UnquoteIdentifier(schemaName), cb.UnquoteIdentifier(tableName));
        }

        private static void ExecuteSql(string connectionString, string sql)
        {
            using (var conn = new OracleConnection(connectionString))
            using (var command = conn.CreateCommand())
            {
                conn.Open();

                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }
}