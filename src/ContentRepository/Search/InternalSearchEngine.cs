using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search
{
    internal class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();
        private static readonly IIndexingEngine IndexingEngineInstance = new InternalIndexingEngine();

        public IIndexingEngine IndexingEngine => IndexingEngineInstance;

        public IQueryEngine QueryEngine => throw new SnNotSupportedException();

        public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
        {
            return null;
        }
        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            // do nothing
        }

        public object GetConfigurationForHealthDashboard()
        {
            return "This provider has no configuration.";
        }

        public Task<object> GetHealthAsync(CancellationToken cancel)
        {
            return Task.FromResult(new object());
        }

        private class InternalIndexingEngine : IIndexingEngine
        {
            public bool Running => false;

            public bool IndexIsCentralized => false;

            public Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
            {
                // do nothing
                return Task.CompletedTask;
            }

            public Task ShutDownAsync(CancellationToken cancellationToken)
            {
                // do nothing
                return Task.CompletedTask;
            }

            public Task<BackupResponse> BackupAsync(string target, CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }

            public Task<BackupResponse> QueryBackupAsync(CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }

            public Task<BackupResponse> CancelBackupAsync(CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }

            public Task ClearIndexAsync(CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }

            public Task<IndexingActivityStatus> ReadActivityStatusFromIndexAsync(CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }

            public Task WriteActivityStatusToIndexAsync(IndexingActivityStatus state, CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }

            public Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates,
                IEnumerable<IndexDocument> additions, CancellationToken cancellationToken)
            {
                throw new SnNotSupportedException();
            }
            public IndexProperties GetIndexProperties()
            {
                throw new System.NotImplementedException();
            }

            public Task<IDictionary<string, IDictionary<string, List<int>>>> GetInvertedIndexAsync(CancellationToken cancel)
            {
                throw new System.NotImplementedException();
            }

            public Task<IDictionary<string, List<int>>> GetInvertedIndexAsync(string fieldName, CancellationToken cancel)
            {
                throw new System.NotImplementedException();
            }

            public IDictionary<string, string> GetIndexDocumentByVersionId(int versionId)
            {
                throw new System.NotImplementedException();
            }

            public IDictionary<string, string> GetIndexDocumentByDocumentId(int documentId)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
