using System.Data.Common;
using Akka.Configuration;
using Akka.Persistence.Sql.Common;
using Akka.Persistence.Sql.Common.Snapshot;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle.Snapshot
{
    /// <summary>
    /// Actor used for storing incoming snapshots into persistent snapshot store backed by an Oracle database.
    /// </summary>
    public class OracleSnapshotStore : SqlSnapshotStore
    {
        protected readonly OraclePersistence Extension = OraclePersistence.Get(Context.System);
        private readonly ISnapshotQueryExecutor _queryExecutor;

        public OracleSnapshotStore(Config snapshotConfig) : base(snapshotConfig)
        {
            var config = snapshotConfig.WithFallback(Extension.DefaultSnapshotConfig);
            _queryExecutor = new OracleSnapshotQueryExecutor(new QueryConfiguration(
                schemaName: config.GetString("schema-name"),
                snapshotTableName: config.GetString("table-name"),
                persistenceIdColumnName: config.GetString("persistenceid-col-name"),
                sequenceNrColumnName: config.GetString("sequencenr-col-name"),
                payloadColumnName: config.GetString("payload-col-name"),
                manifestColumnName: config.GetString("manifest-col-name"),
                timestampColumnName: config.GetString("timestamp-col-name"),
                timeout: config.GetTimeSpan("connection-timeout")),
                Context.System.Serialization);
        }


        public override ISnapshotQueryExecutor QueryExecutor
        {
            get { return _queryExecutor; }
        }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new OracleConnection(connectionString);
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