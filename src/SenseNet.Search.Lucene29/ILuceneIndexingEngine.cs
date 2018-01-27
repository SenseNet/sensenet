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

        /// <summary>
        /// Keeps index descriptors up to date.
        /// </summary>
        void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo);
        /// <summary>
        /// Returns an <see cref="SnPerFieldAnalyzerWrapper"/> containing the data set by the <see cref="SetIndexingInfo"/> method.
        /// </summary>
        Analyzer GetAnalyzer();
    }
}
