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

    public interface ISearchEngine
    {
        /// <summary>
        /// Gets an IIndexingEngine implementation. The instance is not changed during the repository's lifetime. 
        /// </summary>
        IIndexingEngine IndexingEngine { get; }

        /// <summary>
        /// Gets an IQueryEngine implementation. The instance is not changed during the repository's lifetime.
        /// </summary>
        IQueryEngine QueryEngine { get; }

        IDictionary<string, Type> GetAnalyzers();

        void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo);
    }

    public class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();

        public IIndexingEngine IndexingEngine => InternalIndexingEngine.Instance;

        public IQueryEngine QueryEngine
        {
            get { throw new SnNotSupportedException(); }
        }

        public IDictionary<string, Type> GetAnalyzers()
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

            public IIndexingActivityStatus ReadActivityStatusFromIndex()
            {
                throw new SnNotSupportedException();
            }

            public void WriteActivityStatusToIndex(IIndexingActivityStatus state)
            {
                throw new SnNotSupportedException();
            }

            public void WriteIndex(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates)
            {
                throw new SnNotSupportedException();
            }

            public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition)
            {
                throw new SnNotSupportedException();
            }
        }
    }

}
