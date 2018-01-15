using System.Collections.Generic;
using Lucene.Net.Analysis;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Lucene29
{
    /// <inheritdoc />
    /// <summary>
    /// Defines Lucene29-specific methods for indexing engines.
    /// </summary>
    public interface ILuceneIndexingEngine : IIndexingEngine
    {
        LuceneSearchManager LuceneSearchManager { get; }

        void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo);
        Analyzer GetAnalyzer();
    }
}
