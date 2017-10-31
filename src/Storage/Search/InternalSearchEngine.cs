using System.Collections.Generic;
using System.IO;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search
{
    internal class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();

        public IIndexingEngine IndexingEngine => InternalIndexingEngine.Instance;

        public IQueryEngine QueryEngine
        {
            get { throw new SnNotSupportedException(); }
        }

        public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
        {
            return null;
        }
        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            // do nothing
        }

        private class InternalIndexingEngine : IIndexingEngine
        {
            public static IIndexingEngine Instance = new InternalIndexingEngine();

            public bool Running => false;

            public bool IndexIsCentralized => false;

            public void Start(TextWriter consoleOut)
            {
                // do nothing
            }

            public void ShutDown()
            {
                // do nothing
            }

            public void ClearIndex()
            {
                throw new SnNotSupportedException();
            }

            public IndexingActivityStatus ReadActivityStatusFromIndex()
            {
                throw new SnNotSupportedException();
            }

            public void WriteActivityStatusToIndex(IndexingActivityStatus state)
            {
                throw new SnNotSupportedException();
            }

            public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> addition)
            {
                throw new SnNotSupportedException();
            }
        }
    }
}
