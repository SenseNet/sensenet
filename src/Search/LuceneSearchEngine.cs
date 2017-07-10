using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tools;

namespace SenseNet.Search
{
    public class LuceneSearchEngine : ISearchEngine
    {
        static LuceneSearchEngine()
        {
            Lucene.Net.Search.BooleanQuery.SetMaxClauseCount(100000);
        }

        //UNDONE:! Call this when settings API are available and outer engine is resolved
        public void SetConfiguration(IDictionary<string, object> configuration)
        {
            SearchEngineSettings.SetConfiguration(configuration);
        }

        public bool IndexingPaused
        {
            get { return LuceneManager.Paused; }}

        public void PauseIndexing()
        {
            LuceneManager.PauseIndexing();
        }
        public void ContinueIndexing()
        {
            LuceneManager.ContinueIndexing();
        }
        public void WaitIfIndexingPaused()
        {
            LuceneManager.WaitIfIndexingPaused();
        }


        public IIndexPopulator GetPopulator()
        {
            return new DocumentPopulator();
        }
        public IEnumerable<int> Execute(NodeQuery nodeQuery)
        {
            var query = __supportClass.LucQuery.Create(nodeQuery);
            var lucObjects = query.Execute();
            return from lucObject in lucObjects select lucObject.NodeId;
        }
        public IEnumerable<int> Execute(string lucQuery)
        {
            var query = __supportClass.LucQuery.Parse(lucQuery);
            var lucObjects = query.Execute();
            return from lucObject in lucObjects select lucObject.NodeId;
        }

        private IDictionary<string, Type> _analyzers = new Dictionary<string, Type>();
        public IDictionary<string, Type> GetAnalyzers()
        {
            return _analyzers;
        }

        internal static IEnumerable<__supportClass.LucObject> GetAllDocumentVersionsByNodeId(int nodeId)
        {
            var queryText = String.Concat(IndexFieldName.NodeId, ":", nodeId, " .AUTOFILTERS:OFF");
            var query = __supportClass.LucQuery.Parse(queryText);
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

        public object DeserializeIndexDocumentInfo(byte[] indexDocumentInfoBytes)
        {
            if (indexDocumentInfoBytes == null)
                return null;
            if (indexDocumentInfoBytes.Length == 0)
                return null;

            var docStream = new System.IO.MemoryStream(indexDocumentInfoBytes);
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var info = (__supportClass.IndexDocumentInfo)formatter.Deserialize(docStream);
            return info;
        }
    }
}
