using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using SenseNet.Configuration;
using IsolationLevel = System.Data.IsolationLevel;

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
