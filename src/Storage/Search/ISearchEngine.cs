using System;
using System.Collections.Generic;
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
    public interface IIndexableField
    {
        string Name { get; }
        bool IsInIndex { get; }
        bool IsBinaryField { get; }
        IEnumerable<IndexField> GetIndexFields(out string textExtract);
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

        void SetIndexingInfo(object indexingInfo); //UNDONE: tusmester REFACTOR API: IDictionary<string, IPerFieldIndexingInfo> instead of object

        IIndexPopulator GetPopulator(); //UNDONE: REFACTOR: tusmester not SearchEngine responsibility: GetPopulator()
    }
    public class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();

        public IIndexingEngine IndexingEngine
        {
            get { throw new SnNotSupportedException(); }
        }

        public IQueryEngine QueryEngine
        {
            get { throw new SnNotSupportedException(); }
        }

        public IIndexPopulator GetPopulator()
        {
            return NullPopulator.Instance;
        }
        public IDictionary<string, Type> GetAnalyzers()
        {
            return null;
        }
        public void SetIndexingInfo(object indexingInfo)
        {
            // do nothing
        }
    }

}
