using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Tools.SnInitialDataGenerator
{
    internal class SearchEngineForInitialDataGenerator : InMemorySearchEngine
    {
        private IDictionary<string, IndexFieldAnalyzer> _analyzers = new Dictionary<string, IndexFieldAnalyzer>();

        public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
        {
            return _analyzers;
        }

        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            var analyzerTypes = new Dictionary<string, IndexFieldAnalyzer>();

            foreach (var item in indexingInfo)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;
                if (fieldInfo.Analyzer != IndexFieldAnalyzer.Default)
                    analyzerTypes.Add(fieldName, fieldInfo.Analyzer);
            }

            _analyzers = analyzerTypes;

            ((IndexingEngineForInitialDataGenerator) IndexingEngine).SetIndexingInfo(indexingInfo);
        }

        public SearchEngineForInitialDataGenerator() : base(new InMemoryIndex())
        {
            base.IndexingEngine = new IndexingEngineForInitialDataGenerator(this);
        }

        public void SaveIndexDocuments(string indexDocumentsFileName)
        {
            ((IndexingEngineForInitialDataGenerator)IndexingEngine).SaveIndexDocuments(indexDocumentsFileName);
        }
    }

    internal class IndexingEngineForInitialDataGenerator : InMemoryIndexingEngine
    {
        public bool Running => true;
        public bool IndexIsCentralized => false;

        public IndexingEngineForInitialDataGenerator(InMemorySearchEngine searchEngine) : base(searchEngine) { }


        List<IndexDocument> _documents = new List<IndexDocument>();
        public override async Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions,
            CancellationToken cancellationToken)
        {
            var docs = additions as IndexDocument[] ?? additions.ToArray();
            _documents.AddRange(docs);

            await base.WriteIndexAsync(deletions, updates, docs, cancellationToken);
        }


        private IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;
        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            _indexingInfo = indexingInfo;
        }

        public void SaveIndexDocuments(string fileName)
        {
            using (var writer = new StreamWriter(fileName, false))
            {
                foreach (var doc in _documents)
                    writer.WriteLine(doc.Serialize(true));
            }
        }
    }
}
