using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using System;
using System.Threading;
using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public class TreeLock : IDisposable
    {
        private readonly SnTrace.Operation _logOp;
        private readonly IDataStore _dataStore;
        private readonly int[] _lockIds;

        internal TreeLock(SnTrace.Operation logOp, IDataStore dataStore, params int[] lockIds)
        {
            this._logOp = logOp;
            _dataStore = dataStore;
            _lockIds = lockIds;
        }

        public void Dispose()
        {
            //TODO: find a better design instead of calling an asynchronous method
            // synchronously inside a Dispose method.
            _dataStore.ReleaseTreeLockAsync(_lockIds, CancellationToken.None).GetAwaiter().GetResult();
            if (_logOp != null)
            {
                _logOp.Successful = true;
                _logOp.Dispose();
            }
        }
    }
}
