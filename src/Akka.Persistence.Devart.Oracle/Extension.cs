using System;
using System.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sql.Common;

namespace Akka.Persistence.Devart.Oracle
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
    public class OraclePersistence : IExtension
    {
        /// <summary>
        /// Returns a default configuration for akka persistence Oracle-based journals and snapshot stores.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<OraclePersistence>("Akka.Persistence.Oracle.oracle-managed.conf");
        }

        public static OraclePersistence Get(ActorSystem system)
        {
            return system.WithExtension<OraclePersistence, OraclePersistenceProvider>();
        }

        /// <summary>
        /// Journal-related settings loaded from HOCON configuration.
        /// </summary>
        public readonly OracleJournalSettings JournalSettings;

        /// <summary>
        /// Snapshot store related settings loaded from HOCON configuration.
        /// </summary>
        public readonly OracleSnapshotSettings SnapshotSettings;

        public OraclePersistence(ExtendedActorSystem system)
        {
            system.Settings.InjectTopLevelFallback(DefaultConfiguration());

            JournalSettings = new OracleJournalSettings(system.Settings.Config.GetConfig(OracleJournalSettings.ConfigPath));
            SnapshotSettings = new OracleSnapshotSettings(system.Settings.Config.GetConfig(OracleSnapshotSettings.ConfigPath));

            if (JournalSettings.AutoInitialize)
            {
                var connectionString = string.IsNullOrEmpty(JournalSettings.ConnectionString)
                    ? ConfigurationManager.ConnectionStrings[JournalSettings.ConnectionStringName].ConnectionString
                    : JournalSettings.ConnectionString;

                OracleInitializer.CreateOracleJournalTables(connectionString, JournalSettings.SchemaName, JournalSettings.TableName);
            }

            if (SnapshotSettings.AutoInitialize)
            {
                var connectionString = string.IsNullOrEmpty(JournalSettings.ConnectionString)
                    ? ConfigurationManager.ConnectionStrings[JournalSettings.ConnectionStringName].ConnectionString
                    : JournalSettings.ConnectionString;

                OracleInitializer.CreateOracleSnapshotStoreTables(connectionString, SnapshotSettings.SchemaName, SnapshotSettings.TableName);
            }
        }
    }

    /// <summary>
    /// Singleton class used to setup Oracle backend for akka persistence plugin.
    /// </summary>
    public class OraclePersistenceProvider : ExtensionIdProvider<OraclePersistence>
    {        
        /// <summary>
        /// Creates an actor system extension for akka persistence SQL Server support.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public override OraclePersistence CreateExtension(ExtendedActorSystem system)
        {
            return new OraclePersistence(system);
        }        
    }
}