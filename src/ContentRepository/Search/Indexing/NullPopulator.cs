using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SenseNet.Configuration;
using STT= System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal class NullPopulator : IIndexPopulator
    {
        public static NullPopulator Instance = new NullPopulator();

        private static readonly object PopulatorData = new object();

        public STT.Task ClearAndPopulateAllAsync(CancellationToken cancellationToken, TextWriter consoleWriter = null)
        {
            return STT.Task.CompletedTask;
        }

        public STT.Task RebuildIndexDirectlyAsync(string path, CancellationToken cancellationToken,
            IndexRebuildLevel level = IndexRebuildLevel.IndexOnly)
        {
            return STT.Task.CompletedTask;
        }

        public STT.Task AddTreeAsync(string path, int nodeId, CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }
        public object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath) { return PopulatorData; }

        public STT.Task CommitPopulateNodeAsync(object data, IndexDocumentData indexDocument,
            CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }

        public STT.Task FinalizeTextExtractingAsync(object data, IndexDocumentData indexDocument,
            CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }

        public STT.Task DeleteTreeAsync(string path, int nodeId, CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }
#pragma warning disable 0067
        // suppressed because it is not used but the interface declares.
        public event EventHandler<NodeIndexedEventArgs> NodeIndexed;
        public event EventHandler<NodeIndexedEventArgs> IndexDocumentRefreshed;
        public event EventHandler<NodeIndexingErrorEventArgs> IndexingError;
#pragma warning restore 0067
        public STT.Task DeleteForestAsync(IEnumerable<int> idSet, CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }

        public STT.Task DeleteForestAsync(IEnumerable<string> pathSet, CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }

        public async STT.Task RebuildIndexAsync(Node node, CancellationToken cancellationToken, bool recursive = false,
            IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            // do nothing in case of IndexOnly level, because this is a NULL populator
            if (rebuildLevel == IndexRebuildLevel.IndexOnly)
                return;

            using (var op = SnTrace.Index.StartOperation("NullPopulator.RefreshIndex. Version: {0}, VersionId: {1}, recursive: {2}, level: {3}", node.Version, node.VersionId, recursive, rebuildLevel))
            {
                using (new Storage.Security.SystemAccount())
                {
                    if (recursive)
                    {
                        using (await TreeLock.AcquireAsync(cancellationToken, node.Path).ConfigureAwait(false))
                        {
                            foreach (var n in NodeEnumerator.GetNodes(node.Path))
                                await Providers.Instance.DataStore
                                    .SaveIndexDocumentAsync(node, false, false, CancellationToken.None)
                                    .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await TreeLock.AssertFreeAsync(cancellationToken, node.Path).ConfigureAwait(false);
                        await Providers.Instance.DataStore
                            .SaveIndexDocumentAsync(node, false, false, CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }
                op.Successful = true;
            }
        }
    }
}
