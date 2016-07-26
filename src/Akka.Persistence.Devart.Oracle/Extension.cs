using System;
using System.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sql.Common;

namespace Akka.Persistence.Devart.Oracle
{

    public class OracleJournalSettings : JournalSettings
    {
        public const string ConfigPath = "akka.persistence.journal.devart-oracle";

        public OracleJournalSettings(Config config) : base(config)
        {
        }
    }

    public class OracleSnapshotSettings : SnapshotStoreSettings
    {
        public const string ConfigPath = "akka.persistence.snapshot-store.devart-oracle";

        public OracleSnapshotSettings(Config config) : base(config)
        {
        }
    }

    /// <summary>
    /// An actor system extension initializing support for Oracle persistence layer.
    /// </summary>
    public class OraclePersistence : IExtension
    {
        /// <summary>
        /// Returns a default configuration for akka persistence Oracle-based journals and snapshot stores.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<OraclePersistence>("Akka.Persistence.Devart.Oracle.devart-oracle.conf");
        }

        public static OraclePersistence Get(ActorSystem system)
        {
            return system.WithExtension<OraclePersistence, OraclePersistenceProvider>();
        }


        /// <summary>
        /// Journal-related settings loaded from HOCON configuration.
        /// </summary>
        public readonly Config DefaultJournalConfig;

        /// <summary>
        /// Snapshot store related settings loaded from HOCON configuration.
        /// </summary>
        public readonly Config DefaultSnapshotConfig;

        public OraclePersistence(ExtendedActorSystem system)
        {
            var defaultConfig = DefaultConfiguration();
            system.Settings.InjectTopLevelFallback(defaultConfig);

            DefaultJournalConfig = defaultConfig.GetConfig(OracleJournalSettings.ConfigPath);
            DefaultSnapshotConfig = defaultConfig.GetConfig(OracleSnapshotSettings.ConfigPath);
        }
    }

    /// <summary>
    /// Singleton class used to setup Oracle backend for akka persistence plugin.
    /// </summary>
    public class OraclePersistenceProvider : ExtensionIdProvider<OraclePersistence>
    {        
        /// <summary>
        /// Creates an actor system extension for akka persistence Oracle support.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public override OraclePersistence CreateExtension(ExtendedActorSystem system)
        {
            return new OraclePersistence(system);
        }        
    }
}