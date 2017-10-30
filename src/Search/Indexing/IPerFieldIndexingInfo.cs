using System;

namespace SenseNet.Search.Indexing
{
    public interface IPerFieldIndexingInfo
    {
        IndexFieldAnalyzer Analyzer { get; set; }
        IFieldIndexHandler IndexFieldHandler { get; set; }

        IndexingMode IndexingMode { get; set; }
        IndexStoringMode IndexStoringMode { get; set; }
        IndexTermVector TermVectorStoringMode { get; set; }

        bool IsInIndex { get; }

        Type FieldDataType { get; set; }
    }
}
