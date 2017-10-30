using System;
using System.Collections.Generic;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search
{
    public interface ISearchEngine
    {
        /// <summary>
        /// Gets an IIndexingEngine implementation. The instance is not changed during the repository's lifetime. 
        /// </summary>
        IIndexingEngine IndexingEngine { get; }

        /// <summary>
        /// Gets an IQueryEngine implementation. The instance is not changed during the repository's lifetime.
        /// </summary>
        IQueryEngine QueryEngine { get; }

        IDictionary<string, IndexFieldAnalyzer> GetAnalyzers();

        void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo);
    }
}
