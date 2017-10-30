using System;
using System.Diagnostics;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    public enum IndexingMode { Default, Analyzed, AnalyzedNoNorms, No, NotAnalyzed, NotAnalyzedNoNorms }
    public enum IndexStoringMode { Default, No, Yes }
    public enum IndexTermVector { Default, No, WithOffsets, WithPositions, WithPositionsOffsets, Yes }

    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}, Mode:{Mode}, Store:{Store}, TermVector:{TermVector}")]
    public class IndexField : SnTerm
    {
        public IndexingMode Mode { get; }
        public IndexStoringMode Store { get; }
        public IndexTermVector TermVector { get; }

        public IndexField(string name, string value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, string[] value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, bool value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, int value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, long value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, float value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, double value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        public IndexField(string name, DateTime value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
    }
}
