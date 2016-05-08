using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.Devart.Oracle.Journal
{

    /// <summary>
    /// Devart.Oracle implementation of <see cref="IJournalQueryMapper"/> used for mapping data 
    /// returned from ADO.NET data readers back to <see cref="IPersistentRepresentation"/> messages.
    /// </summary>
    internal class OracleJournalQueryMapper : IJournalQueryMapper
    {
        public const int PersistenceIdIndex = 0;
        public const int SequenceNrIndex = 1;
        public const int IsDeletedIndex = 2;
        public const int ManifestIndex = 3;
        public const int PayloadIndex = 4;

        private readonly Akka.Serialization.Serialization _serialization;

        public OracleJournalQueryMapper(Akka.Serialization.Serialization serialization)
        {
            _serialization = serialization;
        }

        public IPersistentRepresentation Map(DbDataReader reader, IActorRef sender = null)
        {
            var persistenceId = reader.GetString(PersistenceIdIndex);
            var sequenceNr = reader.GetInt64(SequenceNrIndex);
            var isDeleted = (reader.GetString(IsDeletedIndex) ?? "N")=="Y" ;
            var manifest = reader.GetString(ManifestIndex);

            // timestamp is SQL-journal specific field, it's not a part of casual Persistent instance  
            var payload = GetPayload(reader, manifest);

            return new Persistent(payload, sequenceNr: sequenceNr, persistenceId: persistenceId, manifest: manifest, isDeleted: isDeleted, sender: sender);
        }

        private object GetPayload(DbDataReader reader, string manifest)
        {
            var type = Type.GetType(manifest, true);
            var binary = (byte[])reader[PayloadIndex];

            var serializer = _serialization.FindSerializerForType(type);
            return serializer.FromBinary(binary, type);
        }
    }
}
