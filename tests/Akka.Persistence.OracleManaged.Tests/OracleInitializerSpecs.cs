using System.Configuration;
using System.Data;
using FluentAssertions;
using Oracle.ManagedDataAccess.Client;
using TestStack.BDDfy;
using Xunit;

namespace Akka.Persistence.OracleManaged
{
    public class OracleInitializerSpecs
    {
        public class CreatingJournalTables
        {
            private string _connectionString;
            private const string TableName = "EventJournal_Test";

            public void GivenTestDbConnectionDetails()
            {
                _connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            }

            public void WhenCreatingJournalTables()
            {
                OracleInitializer.CreateOracleJournalTables(_connectionString, "akka_persist_tests",TableName);
            }

            public void ThenTheTableShouldExist()
            {
                DbUtils.CheckIfTableExists(TableName).Should().BeTrue();
            }

            public void TearDown()
            {
                DbUtils.Clean(TableName);
            }

            [Fact]
            public void Execute()
            {
                this.BDDfy();
            }
        }

        public class CreatingSnapshotTables
        {
            private string _connectionString;
            private const string TableName = "SnapshotStore_Test";

            public void GivenTestDbConnectionDetails()
            {
                _connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            }

            public void WhenCreatingSnapshotTable()
            {
                OracleInitializer.CreateOracleSnapshotStoreTables(_connectionString, "akka_persist_tests", TableName);
            }

            public void ThenTheTableShouldExist()
            {
                DbUtils.CheckIfTableExists(TableName).Should().BeTrue();
            }

            public void TearDown()
            {
                DbUtils.Clean(TableName);
            }

            [Fact]
            public void Execute()
            {
                this.BDDfy();
            }
        }
    }
}