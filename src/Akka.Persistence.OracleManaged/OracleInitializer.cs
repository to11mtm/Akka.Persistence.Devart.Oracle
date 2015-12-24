using System.Reflection;

namespace Akka.Persistence.OracleManaged
{
    using System;
    using Oracle.ManagedDataAccess.Client;

    internal static class OracleInitializer
    {
        private static readonly string OracleJournalTemplate = typeof(OracleInitializer).Assembly.GetEmbeddedResourceText("Akka.Persistence.OracleManaged.Sql.create_journal_table.sql");

        private static readonly string OracleSnapshotStoreTemplate = typeof(OracleInitializer).Assembly.GetEmbeddedResourceText("Akka.Persistence.OracleManaged.Sql.create_snapshot_table.sql");

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

        private static string InitJournalSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "Akka.Persistence.OracleManaged journal table name is required");
            var sql = OracleJournalTemplate.Replace("{{TABLE_NAME}}",tableName);
            return sql;
        }

        private static string InitSnapshotStoreSql(string tableName, string schemaName = null)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "Akka.Persistence.OracleManaged snapshot store table name is required");

            var sql = OracleSnapshotStoreTemplate.Replace("{{TABLE_NAME}}", tableName);
            return sql;
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