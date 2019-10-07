// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Data : SnConfig
    {
        private const string SectionName = "sensenet/data";

        /// <summary>
        /// Gets the configured SQL command timeout value in seconds.
        /// </summary>
        public static int SqlCommandTimeout { get; internal set; } = GetInt(SectionName, "SqlCommandTimeout", 120, 5);
        /// <summary>
        /// Maximum execution time of transactions.
        /// </summary>
        public static double TransactionTimeout { get; internal set; } = GetDouble(SectionName, "TransactionTimeout", SqlCommandTimeout, SqlCommandTimeout);
        /// <summary>
        /// Maximum execution time of long-running transactions. Use this in exceptional cases, 
        /// e.g. when copying a huge stream or performing a batch db operation.
        /// </summary>
        public static double LongTransactionTimeout { get; internal set; } = GetDouble(SectionName, "LongTransactionTimeout",
            TransactionTimeout, TransactionTimeout);
    }
}
