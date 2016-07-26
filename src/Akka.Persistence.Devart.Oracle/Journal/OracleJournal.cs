using System;
using System.Data.Common;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sql.Common.Journal;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle.Journal
{    
    /// <summary>
    /// Persistent journal actor using Oracle as persistence layer. It processes write requests
    /// one by one in asynchronous manner, while reading results asynchronously.
    /// </summary>
    public class OracleJournal : SqlJournal
    {
        public static readonly OraclePersistence Extension = OraclePersistence.Get(Context.System);
        private readonly IJournalQueryExecutor _queryExecutor;

        public OracleJournal(Config journalConfig) : base(journalConfig.WithFallback(Extension.DefaultJournalConfig))
        {
            var config = journalConfig.WithFallback(Extension.DefaultJournalConfig);
            _queryExecutor = new OracleJournalQueryExecutor(new QueryConfiguration(
                schemaName: config.GetString("schema-name"),
                journalEventsTableName: config.GetString("table-name"),
                metaTableName: config.GetString("metadata-table-name"),
                persistenceIdColumnName: config.GetString("persistenceid-col-name"),
                sequenceNrColumnName: config.GetString("sequencenr-col-name"),
                payloadColumnName: config.GetString("payload-col-name"),
                manifestColumnName: config.GetString("manifest-col-name"),
                timestampColumnName: config.GetString("timestamp-col-name"),
                isDeletedColumnName: config.GetString("isdeleted-col-name"),
                tagsColumnName: config.GetString("tags-col-name"),
                timeout: config.GetTimeSpan("connection-timeout")),
                    Context.System.Serialization,
                    GetTimestampProvider(config.GetString("timestamp-provider")));
        }

        public override IJournalQueryExecutor QueryExecutor
        {
            get { return _queryExecutor; }
        }

        protected override string JournalConfigPath
        {
            get { return OracleJournalSettings.ConfigPath; }
        }

        protected override DbConnection CreateDbConnection(string connectionString)
        {

            return new global::Devart.Data.Oracle.OracleConnection(connectionString)
            {
               //Prevents issues dealing with numbers going back and forth on MaxSequenceNumberSQL.
               NumberMappings = {new OracleNumberMapping(OracleNumberType.Number, 18, typeof(long))}
            };
        }

        protected override void PreStart()
        {
            base.PreStart();
        }

        protected override void PostStop()
        {
            base.PostStop();
        }
    }
}