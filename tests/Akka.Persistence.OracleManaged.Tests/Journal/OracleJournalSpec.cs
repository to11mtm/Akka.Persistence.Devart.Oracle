namespace Akka.Persistence.OracleManaged.Journal
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
                                class = ""Akka.Persistence.OracleManaged.Journal.OracleJournal, Akka.Persistence.OracleManaged""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = EventJournal
                                schema-name = dbo
                                auto-initialize = on
                                connection-string = ""Data Source=localhost\\SQLEXPRESS;Database=akka_persistence_tests;User Id=akkadotnet;Password=akkadotnet;""
                            }
                        }
                    }";

            SpecConfig = ConfigurationFactory.ParseString(specString);
        }

        public OracleJournalSpec():base(Config)
        {
            Initialize();
        }
    }
}
