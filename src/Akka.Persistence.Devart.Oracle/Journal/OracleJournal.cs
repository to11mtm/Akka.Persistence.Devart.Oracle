using System;
using System.Data.Common;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Sql.Common.Journal;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle.Journal
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
            QueryMapper = new OracleJournalQueryMapper(system.Serialization);
        }

        protected override string JournalConfigPath { get { return OracleJournalSettings.ConfigPath; } }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new OracleConnection(connectionString) { NumberMappings = { new OracleNumberMapping(OracleNumberType.Number, 19, typeof(long))}};
        }

        protected override void CopyParamsToCommand(DbCommand sqlCommand, JournalEntry entry)
        {
            sqlCommand.Parameters["Persistence_Id"].Value = entry.PersistenceId;
            sqlCommand.Parameters["Sequence_Nr"].Value = entry.SequenceNr;
            sqlCommand.Parameters["Is_Deleted"].Value = entry.IsDeleted ? 'Y' : 'N';
            sqlCommand.Parameters["Manifest"].Value = entry.Manifest;
            sqlCommand.Parameters["Time_stamp"].Value = entry.Timestamp;
            sqlCommand.Parameters["Payload"].Value = entry.Payload;
        
        
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