using System;
using System.Data.Common;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryDataContext : SnDataContext
    {
        public InMemoryDataContext(CancellationToken cancellationToken) : base(new DataOptions(), null, cancellationToken)
        {
        }

        public override DbConnection CreateConnection()
        {
            throw new NotImplementedException();
        }

        public override DbCommand CreateCommand()
        {
            throw new NotImplementedException();
        }

        public override DbParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public override TransactionWrapper WrapTransaction(DbTransaction underlyingTransaction, CancellationToken cancellationToken,
            TimeSpan timeout = default(TimeSpan))
        {
            throw new NotImplementedException();
        }
    }
}
