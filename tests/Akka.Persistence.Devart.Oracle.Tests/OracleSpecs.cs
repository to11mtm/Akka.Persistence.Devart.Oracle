namespace Akka.Persistence.Devart.Oracle
{
    public class OracleSpecs
    {
        public class TableInfo
        {
            public const string JournalTableName = "Spec_EventJournal";
            public const string SchemaName = "akka_persist_tests";
            public const string SnapShotTableName = "Spec_SnapshotStore";
            public const string JournalMetaDataTableName = "spec_journal_metadata";

        }
    }
}