using System;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search.Indexing
{
    public class PerFieldIndexingInfo : IPerFieldIndexingInfo
    {
        public static readonly IndexingMode DefaultIndexingMode = IndexingMode.Analyzed;
        public static readonly IndexStoringMode DefaultIndexStoringMode = IndexStoringMode.No;
        public static readonly IndexTermVector DefaultTermVectorStoringMode = IndexTermVector.No;

        public string Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; }

        public IndexingMode IndexingMode { get; set; }
        public IndexStoringMode IndexStoringMode { get; set; }
        public IndexTermVector TermVectorStoringMode { get; set; }

        public bool IsInIndex
        {
            get
            {
                if (IndexingMode == IndexingMode.No &&
                    (IndexStoringMode == IndexStoringMode.Default || IndexStoringMode == IndexStoringMode.No))
                    return false;
                return true;
            }
        }

        public Type FieldDataType { get; set; }
    }
}
