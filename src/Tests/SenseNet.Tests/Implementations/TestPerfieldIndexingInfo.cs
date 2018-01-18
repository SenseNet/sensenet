using System;
using SenseNet.Search.Indexing;

namespace SenseNet.Tests.Implementations
{
    public class TestPerfieldIndexingInfoString : IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerString();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.No;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(string);
    }
    public class TestPerfieldIndexingInfoInt: IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerInt();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.Yes;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(int);
    }
    public class TestPerfieldIndexingInfoLong : IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerLong();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.Yes;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(int);
    }
    public class TestPerfieldIndexingInfoSingle : IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerSingle();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.Yes;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(int);
    }
    public class TestPerfieldIndexingInfoDouble : IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerDouble();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.Yes;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(int);
    }
    public class TestPerfieldIndexingInfoBool : IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerBool();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.Yes;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(int);
    }
    public class TestPerfieldIndexingInfoDateTime : IPerFieldIndexingInfo
    {
        public IndexFieldAnalyzer Analyzer { get; set; }
        public IFieldIndexHandler IndexFieldHandler { get; set; } = new TestIndexFieldHandlerDateTime();
        public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
        public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.Yes;
        public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
        public bool IsInIndex { get; } = true;
        public Type FieldDataType { get; set; } = typeof(int);
    }
}
