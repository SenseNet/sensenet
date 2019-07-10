using System;
using System.Data.Common;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public interface IDbCommandFactory
    {
        DbConnection CreateConnection();
        DbCommand CreateCommand();
        DbParameter CreateParameter();
        TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction, TimeSpan timeout = default(TimeSpan));
    }
}