using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class TestSearchEngine : ISearchEngine
    {
        public bool IndexingPaused => false;

        public void PauseIndexing()
        {
            // do nothing
        }
        public void ContinueIndexing()
        {
            // do nothing
        }
        public void WaitIfIndexingPaused()
        {
            // do nothing
        }

        public IIndexPopulator GetPopulator()
        {
            return new DocumentPopulator();
        }
        public IDictionary<string, Type> GetAnalyzers()
        {
            throw new NotImplementedException();
        }
        public void SetIndexingInfo(object indexingInfo)
        {
            throw new NotImplementedException();
        }

        public object DeserializeIndexDocumentInfo(byte[] indexDocumentInfoBytes)
        {
            throw new NotImplementedException();
        }
    }
}
