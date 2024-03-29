﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    public class RestoreActivity : IndexingActivityBase
    {
        public override string TraceMessage => $"NodeId: {NodeId}, VersionId: {VersionId}, Path: {Path}";

        protected override Task<bool> ProtectedExecuteAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return System.Threading.Tasks.Task.FromResult(true);
        }

        protected override string GetExtension()
        {
            return null;
        }
        protected override void SetExtension(string value)
        {
            // do nothing
        }
    }
}
