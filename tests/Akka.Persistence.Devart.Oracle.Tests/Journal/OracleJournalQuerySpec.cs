using System.Configuration;
using Akka.Configuration;
using Akka.Persistence.Sql.TestKit;
using Xunit.Abstractions;

namespace Akka.Persistence.Devart.Oracle.Journal
{
    public class OracleJournalQuerySpec : SqlJournalQuerySpec
    {
        private static readonly Config SpecConfig;

        static OracleJournalQuerySpec()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;

            var specString = @"
                    akka.test.single-expect-default = 3s
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.devart-oracle""
                            devart-oracle {
                                class = ""Akka.Persistence.Devart.Oracle.Journal.OracleJournal, Akka.Persistence.Devart.Oracle""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = {TABLE_NAME}
                                schema-name = {SCHEMA_NAME}
                                metadata-table-name = {JOURNAL_METADATA}
                                auto-initialize = on
                                connection-string-name = ""TestDb""
                            }
                        }
                    }".Replace("{TABLE_NAME}", OracleSpecs.TableInfo.JournalTableName).Replace("{SCHEMA_NAME}", OracleSpecs.TableInfo.SchemaName).Replace("{JOURNAL_METADATA}", OracleSpecs.TableInfo.JournalMetaDataTableName)
                    + TimestampConfig("akka.persistence.journal.devart-oracle");

            SpecConfig = ConfigurationFactory.ParseString(specString);

            //DbUtils.Initialize();
        }


        public OracleJournalQuerySpec(ITestOutputHelper output) : base(SpecConfig, "OracleJournalQuerySpec", output)
        {
            OraclePersistence.Get(Sys);

            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            DbUtils.Clean(OracleSpecs.TableInfo.JournalTableName, OracleSpecs.TableInfo.JournalMetaDataTableName,
                OracleSpecs.TableInfo.SnapShotTableName);
            base.Dispose(disposing);
            
        }
    }
}