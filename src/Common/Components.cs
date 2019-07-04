using System;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Central class for holding system-wide pluggable components.
    /// </summary>
    public static class CommonComponents
    {
        /// <summary>
        /// Factory instance responsible for creating platform-specific transactions.
        /// </summary>
        [Obsolete("##", true)]
        public static ITransactionFactory TransactionFactory { get; set; }
    }
}
