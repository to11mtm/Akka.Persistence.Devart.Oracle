namespace Akka.Persistence.OracleManaged
{
    using Oracle.ManagedDataAccess.Client;

    internal static class InternalExtensions
    {
        public static string QuoteSchemaAndTable(this string sqlQuery, string schemaName, string tableName)
        {
            var cb = new OracleCommandBuilder();
            return string.Format(sqlQuery, cb.QuoteIdentifier(schemaName), cb.QuoteIdentifier(tableName));
        }

        /// <summary>
        /// Adds a value to the end of the <see cref="T:Oracle.ManagedDataAccess.Client.OracleParameterCollection"/>.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.ManagedDataAccess.Client.OracleParameter"/> object.
        /// </returns>
        /// <param name="this"></param>
        /// <param name="parameterName">The name of the parameter.</param><param name="value">The value to be added. Use <see cref="F:System.DBNull.Value"/> instead of null, to indicate a null value.</param><filterpriority>2</filterpriority> 
        public static OracleParameter AddWithValue(this OracleParameterCollection @this, string parameterName, object value)
        {
            return @this.Add(new OracleParameter(parameterName, value));
        }
    }
}