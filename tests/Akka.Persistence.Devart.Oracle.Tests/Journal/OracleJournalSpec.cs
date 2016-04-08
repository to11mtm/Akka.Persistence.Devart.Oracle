using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Devart.Oracle.Journal
{
    using Akka.Configuration;
    using Akka.Persistence.TestKit.Journal;
    public class OracleJournalSpec:JournalSpec
    {
        private static readonly Config SpecConfig;

        static OracleJournalSpec()
        {
            var specString = @"
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.oracle-managed""
                            oracle-managed {
                                class = ""Akka.Persistence.Devart.Oracle.Journal.OracleJournal, Akka.Persistence.Devart.Oracle""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = Spec-EventJournal
                                schema-name = akka_persist_tests
                                auto-initialize = on
                                connection-string-name = ""TestDb""
                            }
                        }
                    }";

            SpecConfig = ConfigurationFactory.ParseString(specString);
        }

        public OracleJournalSpec(ITestOutputHelper output) :base(Config, "OracleJournalSpec", output)
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean("Spec-EventJournal");
        }
    }
}
