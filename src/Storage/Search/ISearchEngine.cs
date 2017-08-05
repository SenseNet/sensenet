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
        IEnumerable<IIndexFieldInfo> GetIndexFieldInfos(out string textExtract);
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
        object GetIndexDocumentInfo(Node node, bool skipBinaries, bool isNew, out bool hasBinary);
        object CompleteIndexDocumentInfo(Node node, object baseDocumentInfo);
    }
    public interface ISearchEngine
    {
        bool IndexingPaused { get; }
        void PauseIndexing();
        void ContinueIndexing();
        void WaitIfIndexingPaused();

        IIndexPopulator GetPopulator();

        IDictionary<string, Type> GetAnalyzers();

        void SetIndexingInfo(object indexingInfo);

        object DeserializeIndexDocumentInfo(byte[] indexDocumentInfoBytes);
    }
    public class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();

        public bool IndexingPaused { get { return false; } }
        public void PauseIndexing()
        {
            // do nothing;
        }
        public void ContinueIndexing()
        {
            // do nothing;
        }
        public void WaitIfIndexingPaused()
        {
            // do nothing;
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
        public object DeserializeIndexDocumentInfo(byte[] IndexDocumentInfoBytes)
        {
            return null;
        }
    }

}
