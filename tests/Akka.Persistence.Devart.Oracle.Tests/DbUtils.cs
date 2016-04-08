using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle
{
    public static class DbUtils
    {
        public static void Clean(params string[] tableNames)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            var connectionBuilder = new OracleConnectionStringBuilder(connectionString);
            var schemaName = connectionBuilder.UserId;
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                DropTables(conn, schemaName, tableNames);
            }
        }

        public static bool CheckIfTableExists(string tableName)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            var connectionBuilder = new OracleConnectionStringBuilder(connectionString);
            var databaseName = connectionBuilder.UserId;
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                return CheckIfTableExists(conn, databaseName, tableName);
            }
        }

        private static bool CheckIfTableExists(OracleConnection conn, string schema, string tableName)
        {
            using (var cmd = new OracleCommand())
            {
                cmd.Connection = conn;
                if (!string.IsNullOrWhiteSpace(schema))
                {
                    cmd.CommandText = "ALTER SESSION SET CURRENT_SCHEMA=" + schema;
                    cmd.ExecuteNonQuery();
                }

                try
                {
                    cmd.CommandText = string.Format(@"SELECT 1 from {0} where 1=2", tableName);
                    cmd.ExecuteReader();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        private static void DropTables(OracleConnection conn, string schemaName, params string[] tableNames)
        {
            using (var cmd = new OracleCommand())
            {
                cmd.Connection = conn;
                if (!string.IsNullOrWhiteSpace(schemaName))
                {
                    cmd.CommandText = string.Format("ALTER SESSION SET CURRENT_SCHEMA = {0}", schemaName);
                    cmd.ExecuteNonQuery();
                }

                foreach (var tableName in tableNames)
                {
                    cmd.CommandText = string.Format(@"
                    BEGIN 
                        EXECUTE IMMEDIATE 'DROP TABLE {0}';
                        EXCEPTION WHEN OTHERS THEN NULL;
                    END;",
                    tableName);

                    cmd.ExecuteNonQuery();
                }                
            }
        }
    }
}
