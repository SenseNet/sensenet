using SenseNet.Search.Indexing;

namespace SenseNet.Search.Lucene29
{
    /// <summary>
    /// Local helper class for indexinginfo-related constants.
    /// </summary>
    internal class IndexingInfo
    {
        public static readonly IndexingMode DefaultIndexingMode = IndexingMode.Analyzed;
        public static readonly IndexStoringMode DefaultIndexStoringMode = IndexStoringMode.No;
        public static readonly IndexTermVector DefaultTermVectorStoringMode = IndexTermVector.No;
    }
}
