namespace Akka.Persistence.OracleManaged.Journal
{
    using System;
    using System.Data.Common;
    using Sql.Common;
    using Akka.Persistence.Sql.Common.Journal;
    using Akka.Serialization;

    public class OracleJournal : JournalDbEngine
    {

        protected OracleJournal(JournalSettings settings, Serialization serialization) : base(settings, serialization)
        {

        }

        protected override void CopyParamsToCommand(DbCommand sqlCommand, JournalEntry entry)
        {
            throw new NotImplementedException();
        }

        protected override DbConnection CreateDbConnection()
        {
            throw new NotImplementedException();
        }
    }
}
