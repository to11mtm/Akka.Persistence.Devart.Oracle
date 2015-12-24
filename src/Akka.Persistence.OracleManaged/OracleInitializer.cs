using System.Reflection;

namespace Akka.Persistence.OracleManaged
{
    using System;
    using Oracle.ManagedDataAccess.Client;

    internal static class OracleInitializer
    {
        private static readonly string OracleJournalTemplate = typeof(OracleInitializer).Assembly.GetEmbeddedResourceText("Akka.Persistence.OracleManaged.Sql.create_journal_table.sql");

        private const string OracleSnapshotStoreFormat = @"
            DECLARE
            --IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{2}' AND TABLE_NAME = '{3}')
            BEGIN
                CREATE TABLE {0}.{1} (
	                PersistenceID NVARCHAR2(200) NOT NULL,
	                SequenceNr NUMBER(19) NOT NULL,
                    Timestamp Timestamp NOT NULL,
                    Manifest NVARCHAR(500) NOT NULL,
	                Snapshot LONG RAW NOT NULL
                    CONSTRAINT PK_{3} PRIMARY KEY (PersistenceID, SequenceNr)
                );
                CREATE INDEX IX_{3}_SequenceNr ON {0}.{1}(SequenceNr);
                CREATE INDEX IX_{3}_Timestamp ON {0}.{1}(Timestamp);
            END;
            ";

        /// <summary>
        /// Initializes a Oracle journal-related tables according to 'schema-name', 'table-name' 
        /// and 'connection-string' values provided in 'akka.persistence.journal.oracle-managed' config.
        /// </summary>
        internal static void CreateOracleJournalTables(string connectionString, string schemaName, string tableName)
        {
            var sql = InitJournalSql(tableName, schemaName);
            System.Diagnostics.Debug.Print("SQL for CreateOracleJournalTables is {0}\t{1}", Environment.NewLine, sql);
            ExecuteSql(connectionString, sql, schemaName);
        }

        /// <summary>
        /// Initializes a Oracle snapshot store related tables according to 'schema-name', 'table-name' 
        /// and 'connection-string' values provided in 'akka.persistence.snapshot-store.oracle-managed' config.
        /// </summary>
        internal static void CreateOracleSnapshotStoreTables(string connectionString, string schemaName, string tableName)
        {
            var sql = InitSnapshotStoreSql(tableName, schemaName);
            ExecuteSql(connectionString, sql, schemaName);
        }

        private static string InitJournalSql(string tableName, string schemaName)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "Akka.Persistence.OracleManaged journal table name is required");
            if (string.IsNullOrEmpty(schemaName)) throw new ArgumentNullException("schemaName", "Akka.Persistence.OracleManaged journal schema name is required");
            var sql = OracleJournalTemplate.Replace("{{SCHEMA_NAME}}", schemaName).Replace("{{TABLE_NAME}}",tableName);
            return sql;
        }

        private static string InitSnapshotStoreSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "Akka.Persistence.SqlServer snapshot store table name is required");
            schemaName = schemaName ?? "dbo";

            var cb = new OracleCommandBuilder();
            return string.Format(OracleSnapshotStoreFormat, cb.QuoteIdentifier(schemaName), cb.QuoteIdentifier(tableName), cb.UnquoteIdentifier(schemaName), cb.UnquoteIdentifier(tableName));
        }

        private static void ExecuteSql(string connectionString, string sql, string schemaName = null)
        {
            using (var conn = new OracleConnection(connectionString))
            using (var command = conn.CreateCommand())
            {
                conn.Open();
                if (!string.IsNullOrWhiteSpace(schemaName))
                {
                    command.CommandText = "ALTER SESSION SET CURRENT_SCHEMA=" + schemaName;
                    command.ExecuteNonQuery();
                }
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }
}