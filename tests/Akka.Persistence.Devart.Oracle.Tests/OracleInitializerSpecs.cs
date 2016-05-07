using System.Configuration;
using System.Data;
using FluentAssertions;
using TestStack.BDDfy;
using Xunit;

namespace Akka.Persistence.Devart.Oracle
{
    public class OracleInitializerSpecs
   {
        
        public class CreatingJournalTables
        {
            private string _connectionString;
            

            public void GivenTestDbConnectionDetails()
            {
                _connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            }

            public void WhenCreatingJournalTables()
            {
                OracleInitializer.CreateOracleJournalTables(_connectionString,OracleSpecs.TableInfo.SchemaName ,OracleSpecs.TableInfo.JournalTableName);
            }

            public void ThenTheTableShouldExist()
            {
                DbUtils.CheckIfTableExists(OracleSpecs.TableInfo.JournalTableName).Should().BeTrue();
            }

            public void TearDown()
            {
                DbUtils.Clean(OracleSpecs.TableInfo.JournalTableName);
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
            

            public void GivenTestDbConnectionDetails()
            {
                _connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            }

            public void WhenCreatingSnapshotTable()
            {
                OracleInitializer.CreateOracleSnapshotStoreTables(_connectionString, OracleSpecs.TableInfo.SchemaName, OracleSpecs.TableInfo.SnapShotTableName);
            }

            public void ThenTheTableShouldExist()
            {
                DbUtils.CheckIfTableExists(OracleSpecs.TableInfo.SnapShotTableName).Should().BeTrue();
            }

            public void TearDown()
            {
                DbUtils.Clean(OracleSpecs.TableInfo.SnapShotTableName);
            }

            [Fact]
            public void Execute()
            {
                this.BDDfy();
            }
        }
    }
}