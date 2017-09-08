using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.Tools;

namespace SenseNet.Search.Lucene29
{
    public class Lucene29SearchEngine : ISearchEngine
    {
        public static readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;

        readonly Lucene29QueryEngine _queryEngineInstance = new Lucene29QueryEngine();

        public IIndexingEngine IndexingEngine { get; } = new Lucene29IndexingEngine();

        public IQueryEngine QueryEngine { get; } = new Lucene29QueryEngine();

        static Lucene29SearchEngine()
        {
            Lucene.Net.Search.BooleanQuery.SetMaxClauseCount(100000);
        }

        public IIndexPopulator GetPopulator()
        {
            return new DocumentPopulator();
        }
        public IEnumerable<int> Execute(string lucQuery)
        {
            var query = LucQuery.Parse(lucQuery);
            var lucObjects = query.Execute();
            return from lucObject in lucObjects select lucObject.NodeId;
        }

        private IDictionary<string, Type> _analyzers = new Dictionary<string, Type>();
        public IDictionary<string, Type> GetAnalyzers()
        {
            return _analyzers;
        }

        internal static IEnumerable<LucObject> GetAllDocumentVersionsByNodeId(int nodeId)
        {
            var queryText = String.Concat(IndexFieldName.NodeId, ":", nodeId, " .AUTOFILTERS:OFF");
            var query = LucQuery.Parse(queryText);
            var result = query.Execute(true);
            return result;
        }

        public void SetIndexingInfo(object indexingInfo)
        {
            var allInfo = (Dictionary<string, PerFieldIndexingInfo>)indexingInfo;
            var analyzerTypes = new Dictionary<string, Type>();

            foreach (var item in allInfo)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;
                if (fieldInfo.Analyzer != null)
                {
                    var analyzerType = TypeResolver.GetType(fieldInfo.Analyzer);
                    if (analyzerType == null)
                        throw new InvalidOperationException(String.Concat("Unknown analyzer: ", fieldInfo.Analyzer, ". Field: ", fieldName));
                    analyzerTypes.Add(fieldName, analyzerType);
                }
                _analyzers = analyzerTypes;
            }
        }
    }
}
