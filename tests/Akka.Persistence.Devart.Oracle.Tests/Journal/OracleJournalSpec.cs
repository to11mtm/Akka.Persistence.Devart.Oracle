using Akka.Configuration;
using Akka.Persistence.TestKit.Journal;
using Xunit.Abstractions;

namespace Akka.Persistence.Devart.Oracle.Journal
{

    public class OracleJournalSpec : JournalSpec
    {

        public OracleJournalSpec(ITestOutputHelper output)
            : base(CreateSpecConfig(null), "OracleJournalSpec", output)
        {
            OraclePersistence.Get(Sys);

            Initialize();
        }


        protected override void Dispose(bool disposing)
        {
            DbUtils.Clean(OracleSpecs.TableInfo.JournalTableName, OracleSpecs.TableInfo.SnapShotTableName, OracleSpecs.TableInfo.JournalMetaDataTableName);
            base.Dispose(disposing);
        }

        private static Config CreateSpecConfig(string connectionString)
        {
            var specString = @"
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
                    }".Replace("{TABLE_NAME}", OracleSpecs.TableInfo.JournalTableName).Replace("{SCHEMA_NAME}", OracleSpecs.TableInfo.SchemaName).Replace("{JOURNAL_METADATA}",OracleSpecs.TableInfo.JournalMetaDataTableName);

            return ConfigurationFactory.ParseString(specString);
            
        }
    }

    
}
