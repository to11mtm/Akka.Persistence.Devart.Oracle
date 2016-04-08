using System.Configuration;
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
            var specString = @"
                        akka.persistence {
                            publish-plugin-commands = on
                            snapshot-store {
                                plugin = ""akka.persistence.snapshot-store.oracle-managed""
                                oracle-managed {
                                    class = ""Akka.Persistence.Devart.Oracle.Snapshot.OracleSnapshotStore, Akka.Persistence.Devart.Oracle""
                                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                                    table-name = Spec-SnapshotStore
                                    schema-name = akka_persist_tests
                                    auto-initialize = on
                                    connection-string-name = ""TestDb""
                                }
                            }
                        }";

            SpecConfig = ConfigurationFactory.ParseString(specString);


            //need to make sure db is created before the tests start
            //DbUtils.Initialize();
            DbUtils.Clean("Spec-SnapshotStore");
        }

        public OracleSnapshotStoreSpec(ITestOutputHelper output)
            : base(SpecConfig, "OracleSnapshotStoreSpec", output)
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean("Spec-SnapshotStore");
        }
    }
}
