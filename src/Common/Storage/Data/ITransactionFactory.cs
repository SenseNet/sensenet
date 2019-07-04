using System;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines a factory class for constructing a platform-specific transaction.
    /// </summary>
    [Obsolete("##", true)]
    public interface ITransactionFactory //UNDONE:DB: Delte this class and all implementations.
    {
        /// <summary>
        /// Creates a platform-specific transaction.
        /// </summary>
        ITransactionProvider CreateTransaction();
    }
}
