using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search
{
    public class DefaultIndexingEngineFactory : IIndexingEngineFactory
    {
        private IIndexingEngine _defaultEngine;

        public IIndexingEngine CreateIndexingEngine()
        {
            return _defaultEngine;
        }

        public DefaultIndexingEngineFactory()
        {
            _defaultEngine = new Lucene29IndexingEngine(); //UNDONE:!!! Create instance by configuration
        }

        public DefaultIndexingEngineFactory(IIndexingEngine indexingEngine)
        {
            _defaultEngine = indexingEngine;
        }
    }
}
