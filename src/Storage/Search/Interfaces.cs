using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Parser;

namespace SenseNet.ContentRepository.Storage.Search
{
    public enum IndexRebuildLevel { IndexOnly, DatabaseAndIndex };

    public interface IIndexableDocument
    {
        IEnumerable<IIndexableField> GetIndexableFields();
    }
    public interface IIndexValueConverter<T>
    {
        T GetBack(string fieldValue);
    }
    public interface IIndexValueConverter
    {
        object GetBack(string fieldValue);
    }

    public interface IIndexDocumentProvider
    {
        IndexDocument GetIndexDocument(Node node, bool skipBinaries, bool isNew, out bool hasBinary);
        IndexDocument CompleteIndexDocument(Node node, IndexDocument baseDocument);
    }


    public class InternalSearchEngine : ISearchEngine
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
