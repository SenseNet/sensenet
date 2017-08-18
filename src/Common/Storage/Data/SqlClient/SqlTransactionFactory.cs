
namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// Factory class for creating Transact-SQL database transaction.
    /// </summary>
    public class SqlTransactionFactory : ITransactionFactory //UNDONE: SqlTransactionFactory can be deleted. The interface is implemented by DataProvider instances.
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
