using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Akka.Actor;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.OracleManaged.Journal
{    
    /// <summary>
    /// Specialization of the <see cref="JournalDbEngine"/> which uses Oracle as it's sql backend database.
    /// </summary>
    public class OracleJournalEngine : JournalDbEngine
    {
        public OracleJournalEngine(ActorSystem system)
            : base(system)
        {
            QueryBuilder = new OracleJournalQueryBuilder(Settings.TableName, Settings.SchemaName);
        }

        protected override string JournalConfigPath { get { return OracleJournalSettings.ConfigPath; } }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new OracleConnection(connectionString);
        }

        protected override void CopyParamsToCommand(DbCommand sqlCommand, JournalEntry entry)
        {
            sqlCommand.Parameters["@PersistenceId"].Value = entry.PersistenceId;
            sqlCommand.Parameters["@SequenceNr"].Value = entry.SequenceNr;
            sqlCommand.Parameters["@IsDeleted"].Value = entry.IsDeleted;
            sqlCommand.Parameters["@Manifest"].Value = entry.Manifest;
            sqlCommand.Parameters["@Timestamp"].Value = entry.Timestamp;
            sqlCommand.Parameters["@Payload"].Value = entry.Payload;
        }
    }

    /// <summary>
    /// Persistent journal actor using Oracle as persistence layer. It processes write requests
    /// one by one in asynchronous manner, while reading results asynchronously.
    /// </summary>
    public class OracleJournal : SqlJournal
    {
        public readonly OraclePersistence Extension = OraclePersistence.Get(Context.System);
        public OracleJournal() : base(new OracleJournalEngine(Context.System))
        {
        }
    }    
}