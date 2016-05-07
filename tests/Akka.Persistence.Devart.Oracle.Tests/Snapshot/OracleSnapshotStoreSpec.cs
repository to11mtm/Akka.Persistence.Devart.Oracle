using System.Configuration;
using System.Xml.Serialization;
using Akka.Configuration;
using Akka.Persistence.TestKit.Snapshot;
using Xunit.Abstractions;

namespace Akka.Persistence.Devart.Oracle.Snapshot
{
    public class OracleSnapshotStoreSpec:SnapshotStoreSpec
    {
        private static readonly Config SpecConfig;

        static OracleSnapshotStoreSpec()
        {
            var specString = (@"
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
                        }").Replace("{TABLE_NAME}", OracleSpecs.TableInfo.SnapShotTableName).Replace("{SCHEMA_NAME}",OracleSpecs.TableInfo.SchemaName);

            SpecConfig = ConfigurationFactory.ParseString(specString);


            //need to make sure db is created before the tests start
            //DbUtils.Initialize();
            DbUtils.Clean(OracleSpecs.TableInfo.SnapShotTableName);
        }

        public OracleSnapshotStoreSpec(ITestOutputHelper output)
            : base(SpecConfig, "OracleSnapshotStoreSpec", output)
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean(OracleSpecs.TableInfo.SnapShotTableName);
        }
    }
}
