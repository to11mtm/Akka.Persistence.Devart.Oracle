using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sql.Common;

namespace Akka.Persistence.OracleManaged
{

    public class OracleJournalSettings : JournalSettings
    {
        public const string ConfigPath = "akka.persistence.journal.oracle-managed";

        /// <summary>
        /// Flag determining in in case of event journal table missing, it should be automatically initialized.
        /// </summary>
        public bool AutoInitialize { get; private set; }

        public OracleJournalSettings(Config config) : base(config)
        {
            AutoInitialize = config.GetBoolean("auto-initialize");
        }
    }

    public class OracleSnapshotSettings : SnapshotStoreSettings
    {
        public const string ConfigPath = "akka.persistence.snapshot-store.oracle-managed";

        /// <summary>
        /// Flag determining in in case of snapshot store table missing, it should be automatically initialized.
        /// </summary>
        public bool AutoInitialize { get; private set; }

        public OracleSnapshotSettings(Config config) : base(config)
        {
            AutoInitialize = config.GetBoolean("auto-initialize");
        }
    }

    /// <summary>
    /// An actor system extension initializing support for SQL Server persistence layer.
    /// </summary>
    public class OraclePersistenceExtension : IExtension
    {
        /// <summary>
        /// Journal-related settings loaded from HOCON configuration.
        /// </summary>
        public readonly OracleJournalSettings JournalSettings;

        /// <summary>
        /// Snapshot store related settings loaded from HOCON configuration.
        /// </summary>
        public readonly OracleSnapshotSettings SnapshotStoreSettings;

        public OraclePersistenceExtension(ExtendedActorSystem system)
        {
            system.Settings.InjectTopLevelFallback(OraclePersistence.DefaultConfiguration());

            JournalSettings = new OracleJournalSettings(system.Settings.Config.GetConfig(OracleJournalSettings.ConfigPath));
            SnapshotStoreSettings = new OracleSnapshotSettings(system.Settings.Config.GetConfig(OracleSnapshotSettings.ConfigPath));

            if (JournalSettings.AutoInitialize)
            {
                OracleInitializer.CreateOracleJournalTables(JournalSettings.ConnectionString, JournalSettings.SchemaName, JournalSettings.TableName);
            }

            if (SnapshotStoreSettings.AutoInitialize)
            {
                OracleInitializer.CreateOracleSnapshotStoreTables(SnapshotStoreSettings.ConnectionString, SnapshotStoreSettings.SchemaName, SnapshotStoreSettings.TableName);
            }
        }
    }

    /// <summary>
    /// Singleton class used to setup SQL Server backend for akka persistence plugin.
    /// </summary>
    public class OraclePersistence : ExtensionIdProvider<OraclePersistenceExtension>
    {
        public static readonly OraclePersistence Instance = new OraclePersistence();

        /// <summary>
        /// Initializes a SQL Server persistence plugin inside provided <paramref name="actorSystem"/>.
        /// </summary>
        public static void Init(ActorSystem actorSystem)
        {
            Instance.Apply(actorSystem);
        }

        private OraclePersistence() { }
        
        /// <summary>
        /// Creates an actor system extension for akka persistence SQL Server support.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public override OraclePersistenceExtension CreateExtension(ExtendedActorSystem system)
        {
            return new OraclePersistenceExtension(system);
        }

        /// <summary>
        /// Returns a default configuration for akka persistence SQL Server-based journals and snapshot stores.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<OraclePersistence>("Akka.Persistence.SqlServer.sql-server.conf");
        }
    }
}