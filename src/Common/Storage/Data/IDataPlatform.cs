using System;
using System.Data.Common;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{ 
    public interface IDataPlatform<out TConnection, out TCommand, out TParameter>
    {
        TConnection CreateConnection();
        TCommand CreateCommand();
        TParameter CreateParameter();
        TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction, CancellationToken cancellationToken, TimeSpan timeout = default(TimeSpan));
    }
}
