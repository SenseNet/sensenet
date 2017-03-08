namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines a factory class for constructing a platform-specific transaction.
    /// </summary>
    public interface ITransactionFactory
    {
        /// <summary>
        /// Creates a platform-specific transaction.
        /// </summary>
        ITransactionProvider CreateTransaction();
    }
}
