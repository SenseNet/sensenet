using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Search
{
    public interface IIndexDocumentProvider
    {
        object GetIndexDocumentInfo(Node node, bool skipBinaries, bool isNew, out bool hasBinary);
        object CompleteIndexDocumentInfo(Node node, object baseDocumentInfo);
    }
    public interface ISearchEngine
    {
        void SetConfiguration(IDictionary<string, object> configuration);

        bool IndexingPaused { get; }
        void PauseIndexing();
        void ContinueIndexing();
        void WaitIfIndexingPaused();

        IIndexPopulator GetPopulator();

        IEnumerable<int> Execute(NodeQuery nodeQuery);
        IDictionary<string, Type> GetAnalyzers();

        void SetIndexingInfo(object indexingInfo);

        object DeserializeIndexDocumentInfo(byte[] IndexDocumentInfoBytes);
    }
    public class InternalSearchEngine : ISearchEngine
    {
        public static InternalSearchEngine Instance = new InternalSearchEngine();

        public void SetConfiguration(IDictionary<string, object> configuration)
        {
            // do nothing;
        }

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
        public IEnumerable<int> Execute(NodeQuery nodeQuery)
        {
            throw new NotSupportedException();
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
