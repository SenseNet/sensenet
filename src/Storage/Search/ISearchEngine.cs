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
        IIndexPopulator GetPopulator(); //UNDONE: not SearchEngine responsibility: GetPopulator()

        IDictionary<string, Type> GetAnalyzers();

        void SetIndexingInfo(object indexingInfo);

        IIndexingEngine GetIndexingEngine();

        IQueryEngine GetQueryEngine();
    }
    public class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();

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

        public IIndexingEngine GetIndexingEngine()
        {
            throw new SnNotSupportedException();
        }

        public IQueryEngine GetQueryEngine()
        {
            throw new SnNotSupportedException();
        }
    }

}
