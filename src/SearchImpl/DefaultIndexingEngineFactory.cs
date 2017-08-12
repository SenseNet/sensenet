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
        private Lucene29IndexingEngine _defaultEngine = new Lucene29IndexingEngine(); //UNDONE:!!!! Create instance by configuration

        public IIndexingEngine CreateIndexingEngine()
        {
            return _defaultEngine;
        }
    }
}
