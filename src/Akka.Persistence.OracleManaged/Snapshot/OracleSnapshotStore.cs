using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Akka.Persistence.Sql.Common;
using Akka.Persistence.Sql.Common.Snapshot;

namespace Akka.Persistence.OracleManaged.Snapshot
{
    /// <summary>
    /// Actor used for storing incoming snapshots into persistent snapshot store backed by SQL Server database.
    /// </summary>
    public class OracleSnapshotStore : SqlSnapshotStore
    {
        private readonly OraclePersistence _extension;

        public OracleSnapshotStore() : base()
        {
            _extension = OraclePersistence.Get(Context.System);
            QueryBuilder = new OracleSnapshotQueryBuilder(_extension.SnapshotSettings);
            QueryMapper = new OracleQueryMapper(Context.System.Serialization);
        }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new OracleConnection(Settings.ConnectionString);
        }
        protected override SnapshotStoreSettings Settings { get { return _extension.SnapshotSettings; } }        
    }
}