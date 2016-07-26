using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Devart.Data.Oracle;

namespace Akka.Persistence.Devart.Oracle
{

    internal static class InternalExtensions
    {

        public static string WrapOptimisticCreateIfNotExists(string createStatement)
        {
            return String.Format(@"BEGIN
    BEGIN
        EXECUTE IMMEDIATE '{0}';
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -955 THEN
                null; --Already exists, we're fine.
            END IF;
    END;
END;", createStatement);
        }

        /// <summary>
        /// Takes an Insert Statement and Updates Statement and wraps them into an optimistic (Insert, Update if Not Present)
        /// Upsert Statement.
        /// We do this because it's the 'fastest' and 'most clear' upsert in oracle.
        /// </summary>
        /// <param name="saneInsertStatement">The Insert Statement to use</param>
        /// <param name="saneUpdateStatement">The Update Statement to use when insert fails</param>
        /// <returns></returns>
        public static string AsOptimisticUpsert(string saneInsertStatement, string saneUpdateStatement)
        {
            return string.Format(@"BEGIN
    BEGIN
        {0}
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE = -1 THEN
              {1}
            END IF;
    END;
END;", saneInsertStatement, saneUpdateStatement);
        }
    }
}