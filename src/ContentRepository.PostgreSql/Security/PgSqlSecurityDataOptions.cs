namespace SenseNet.ContentRepository.Storage.Data.PgSqlClient.Security
{
    /// <summary>
    /// Configuration options for the PostgreSQL security data provider.
    /// </summary>
    public class PgSqlSecurityDataOptions
    {
        /// <summary>
        /// SQL command timeout in seconds. Default: 120.
        /// </summary>
        public int SqlCommandTimeout { get; set; } = 120;

        /// <summary>
        /// PostgreSQL connection string for the security database.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
