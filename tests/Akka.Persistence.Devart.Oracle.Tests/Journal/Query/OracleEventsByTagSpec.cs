using Akka.Configuration;
using Akka.Persistence.Sql.TestKit;
using Xunit.Abstractions;

namespace Akka.Persistence.Devart.Oracle.Journal.Query
{
    public class OracleEventsByTagSpec : EventsByTagSpec
    {
        public static Config Config()
        {
            var specString = @"
                    akka.test.single-expect-default = 10s
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
                                refresh-interval = 1s
                                event-adapters {
                                                  color-tagger  = ""Akka.Persistence.Sql.TestKit.ColorTagger, Akka.Persistence.Sql.TestKit""
                                                }
                                event-adapter-bindings = {
                                                  ""System.String"" = color-tagger
                                                }
                            }
                        }
                    }".Replace("{TABLE_NAME}", OracleSpecs.TableInfo.JournalTableName)
                .Replace("{SCHEMA_NAME}", OracleSpecs.TableInfo.SchemaName)
                .Replace("{JOURNAL_METADATA}", OracleSpecs.TableInfo.JournalMetaDataTableName);

            return ConfigurationFactory.ParseString(specString);

        }

        protected override void Dispose(bool disposing)
        {
            DbUtils.Clean(OracleSpecs.TableInfo.JournalTableName, OracleSpecs.TableInfo.SnapShotTableName, OracleSpecs.TableInfo.JournalMetaDataTableName);
            base.Dispose(disposing);
        }

        public OracleEventsByTagSpec(ITestOutputHelper output) : base(Config(), output)
        {
        }
    }
}