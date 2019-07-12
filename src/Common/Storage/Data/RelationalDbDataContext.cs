using System;
using System.Data.Common;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public class RelationalDbDataContext : SnDataContext<DbConnection, DbCommand, DbParameter, DbDataReader>
    {
        private readonly IDataPlatform<DbConnection, DbCommand, DbParameter> _dataPlatform;

        public RelationalDbDataContext(IDataPlatform<DbConnection, DbCommand, DbParameter> dataPlatform,
            CancellationToken cancellationToken = default(CancellationToken))
            : base(cancellationToken)
        {
            _dataPlatform = dataPlatform;
        }

        public override DbConnection CreateConnection()
        {
            return _dataPlatform.CreateConnection();
        }
        public override DbCommand CreateCommand()
        {
            return _dataPlatform.CreateCommand();
        }
        public override DbParameter CreateParameter()
        {
            return _dataPlatform.CreateParameter();
        }
        public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction
            , CancellationToken cancellationToken, TimeSpan timeout = default(TimeSpan))
        {
            return _dataPlatform.WrapTransaction(underlyingTransaction, cancellationToken, timeout);
        }
    }
}
