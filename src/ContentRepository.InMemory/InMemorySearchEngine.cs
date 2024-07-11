using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemorySearchEngine : ISearchEngine
    {
        private IDictionary<string, IndexFieldAnalyzer> _analyzers = new Dictionary<string, IndexFieldAnalyzer>();
        private List<string> _numberFields = new List<string>();

        public IIndexingEngine IndexingEngine { get; protected set; }

        public IQueryEngine QueryEngine { get; }

        public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
        {
            return _analyzers;
        }

        public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            var analyzerTypes = new Dictionary<string, IndexFieldAnalyzer>();
            var numberFields = new List<string> {"NodeTimestamp", "VersionTimestamp"};

            foreach (var item in indexingInfo)
            {
                var fieldName = item.Key;
                var fieldInfo = item.Value;
                if (fieldInfo.Analyzer != IndexFieldAnalyzer.Default)
                    analyzerTypes.Add(fieldName, fieldInfo.Analyzer);
                if (fieldInfo.FieldDataType == typeof(int) ||
                    fieldInfo.FieldDataType == typeof(long) ||
                    fieldInfo.FieldDataType == typeof(IEnumerable<Node>))
                    numberFields.Add(fieldName);
            }

            _analyzers = analyzerTypes;
            Index.Analyzers = _analyzers;
            ((InMemoryIndexingEngine)IndexingEngine).NumberFields = numberFields;
        }

        public object GetConfigurationForHealthDashboard()
        {
            return "This provider has no configuration.";
        }

        public Task<object> GetHealthAsync(CancellationToken cancel)
        {
            IDictionary<string, string> data = null;
            string error = null;
            TimeSpan? elapsed = null;

            try
            {
                var timer = Stopwatch.StartNew();
                data = this.IndexingEngine.GetIndexDocumentByVersionId(1);
                timer.Stop();
                elapsed = timer.Elapsed;
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            object result;
            if (error != null)
            {
                result = new
                {
                    Color = "Red", // Error
                    Reason = $"ERROR: {error}",
                    Method = "SearchEngine (InProc) ties to get index document by VersionId 1."
                };
            }
            else if (data == null)
            {
                result = new
                {
                    Color = "Yellow", // Problem
                    Reason = "No result",
                    Method = "SearchEngine (InProc) ties to get index document by VersionId 1."
                };
            }
            else
            {
                result = new
                {
                    Color = "Green", // Working well
                    ResponseTime = elapsed,
                    Method = "Measure the time of getting the index document by VersionId 1 in secs from SearchEngine (InProc)."
                };
            }

            return System.Threading.Tasks.Task.FromResult(result);
        }

        public InMemoryIndex Index { get; set; }
        public InMemorySearchEngine(InMemoryIndex index)
        {
            Index = index;
            IndexingEngine = new InMemoryIndexingEngine(this);
            QueryEngine = new InMemoryQueryEngine(this);
        }
    }
}
