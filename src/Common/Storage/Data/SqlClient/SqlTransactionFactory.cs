
namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// Factory class for creating Transact-SQL database transaction.
    /// </summary>
    public class SqlTransactionFactory : ITransactionFactory
    {
        /// <summary>
        /// Creates a Transact-SQL database transaction.
        /// </summary>
        public ITransactionProvider CreateTransaction()
        {
            return new Transaction();
        }
    }
}
