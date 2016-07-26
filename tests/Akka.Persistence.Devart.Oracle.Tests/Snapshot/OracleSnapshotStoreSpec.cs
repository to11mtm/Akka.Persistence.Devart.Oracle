using Akka.Configuration;
using Akka.Persistence.TestKit.Snapshot;
using Xunit.Abstractions;

namespace Akka.Persistence.Devart.Oracle.Snapshot
{



    public class OracleSnapshotStoreSpec : SnapshotStoreSpec
    {

        public OracleSnapshotStoreSpec(ITestOutputHelper output)
            : base(
                CreateSpecConfig(""), "OracleSnapshotStoreSpec",output)
        {
            OraclePersistence.Get(Sys);

            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            DbUtils.Clean(OracleSpecs.TableInfo.JournalTableName, OracleSpecs.TableInfo.SnapShotTableName, OracleSpecs.TableInfo.JournalMetaDataTableName);
            base.Dispose(disposing);
        }

        private static Config CreateSpecConfig(string smth)
        {
            var specString =
                (@"
                            akka.persistence {
                                publish-plugin-commands = on
                                snapshot-store {
                                    plugin = ""akka.persistence.snapshot-store.devart-oracle""
                                    devart-oracle {
                                        class = ""Akka.Persistence.Devart.Oracle.Snapshot.OracleSnapshotStore, Akka.Persistence.Devart.Oracle""
                                        plugin-dispatcher = ""akka.actor.default-dispatcher""
                                        table-name = {TABLE_NAME}
                                        schema-name = {SCHEMA_NAME}
                                        auto-initialize = on
                                        connection-string-name = ""TestDb""
                                    }
                                }
                            }").Replace("{TABLE_NAME}", OracleSpecs.TableInfo.SnapShotTableName)
                    .Replace("{SCHEMA_NAME}", OracleSpecs.TableInfo.SchemaName);
            return ConfigurationFactory.ParseString(specString);
        }

    }
}

