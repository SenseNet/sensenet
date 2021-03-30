using System;
// ReSharper disable CheckNamespace

namespace SenseNet.Configuration
{
    public class Data : SnConfig
    {
        private const string SectionName = "sensenet/data";

        /// <summary>
        /// Gets the configured SQL command timeout value in seconds.
        /// </summary>
        public static int DbCommandTimeout { get; internal set; } = GetInt(SectionName, "DbCommandTimeout", 120, 5);
        /// <summary>
        /// Gets the configured Sql command timeout value in seconds.
        /// </summary>
        [Obsolete("Use DbCommandTimeout instead.", true)]
        public static int SqlCommandTimeout { get; internal set; } = GetInt(SectionName, "SqlCommandTimeout", DbCommandTimeout);
        /// <summary>
        /// Maximum execution time of transactions.
        /// </summary>
        public static double TransactionTimeout { get; internal set; } = GetDouble(SectionName, "TransactionTimeout", DbCommandTimeout, DbCommandTimeout);
        /// <summary>
        /// Maximum execution time of long-running transactions. Use this in exceptional cases, 
        /// e.g. when copying a huge stream or performing a batch db operation.
        /// </summary>
        public static double LongTransactionTimeout { get; internal set; } = GetDouble(SectionName, "LongTransactionTimeout",
            TransactionTimeout, TransactionTimeout);
    }

    public class DataOptions
    {
        //TODO: [DIREF] remove legacy data configuration when upper layers are ready.
        /// <summary>
        /// DO NOT USE THIS IN YOUR CODE. This method is intended for internal use only and will be removed in the near future.
        /// </summary>
        /// <returns>A new instance of data options filled with static configuration values.</returns>
        [Obsolete]
        public static DataOptions GetLegacyConfiguration()
        {
            return new DataOptions()
            {
                DbCommandTimeout = Data.DbCommandTimeout,
                TransactionTimeout = Data.TransactionTimeout,
                LongTransactionTimeout = Data.LongTransactionTimeout
            };
        }

        /// <summary>
        /// Gets the configured db command timeout value in seconds.
        /// </summary>
        public int DbCommandTimeout { get; set; } = 120;
        /// <summary>
        /// Maximum execution time of transactions.
        /// </summary>
        public double TransactionTimeout { get; set; } = 120;
        /// <summary>
        /// Maximum execution time of long-running transactions. Use this in exceptional cases, 
        /// e.g. when copying a huge stream or performing a batch db operation.
        /// </summary>
        public double LongTransactionTimeout { get; set; } = 120;
    }
}
