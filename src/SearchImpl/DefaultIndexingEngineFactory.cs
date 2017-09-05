using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search
{
    public class DefaultIndexingEngineFactory : IIndexingEngineFactory
    {
        private readonly IIndexingEngine _instance;

        public IIndexingEngine CreateIndexingEngine()
        {
            return _instance;
        }

        public DefaultIndexingEngineFactory()
        {
            _instance = StorageContext.Search.SearchEngine.GetIndexingEngine();
        }

        public DefaultIndexingEngineFactory(IIndexingEngine indexingEngine)
        {
            _instance = indexingEngine;
        }
    }
}
