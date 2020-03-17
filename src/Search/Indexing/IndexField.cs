using System;
using System.Diagnostics;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Describes the field's indexing mode.
    /// </summary>
    public enum IndexingMode
    {
        /// <summary>
        /// Means "Analyzed"
        /// </summary>
        Default,
        /// <summary>
        /// The value is transformed by the associated text analyzer.
        /// </summary>
        Analyzed,
        /// <summary>
        /// Not used. Inspired by similar option of the Lucene
        /// (see: https://lucene.apache.org/core/3_5_0/api/core/org/apache/lucene/document/Field.Index.html#ANALYZED_NO_NORMS)
        /// </summary>
        AnalyzedNoNorms,
        /// <summary>
        /// Field is not indexed.
        /// </summary>
        No,
        /// <summary>
        /// Field is indexed by it's raw value.
        /// </summary>
        NotAnalyzed,
        /// <summary>
        /// Not used. Inspired by similar option of the Lucene
        /// (see: https://lucene.apache.org/core/3_5_0/api/core/org/apache/lucene/document/Field.Index.html#NOT_ANALYZED_NO_NORMS
        /// </summary>
        NotAnalyzedNoNorms
    }

    /// <summary>
    /// Describes the field's storing mode in the index.
    /// </summary>
    public enum IndexStoringMode
    {
        /// <summary>
        /// Means "No"
        /// </summary>
        Default,
        /// <summary>
        /// The field's raw value is not stored in the index.
        /// </summary>
        No,
        /// <summary>
        /// The field's raw value is stored in the index.
        /// </summary>
        Yes
    }

    /// <summary>
    /// Describes the term vector handling.
    /// Used in Lucene based indexes.
    /// See: https://lucene.apache.org/core/3_5_0/api/core/org/apache/lucene/document/Field.TermVector.html
    /// </summary>
    public enum IndexTermVector
    {
        /// <summary>
        /// Means "No"
        /// </summary>
        Default,
        /// <summary>
        /// Term vector is not stored.
        /// </summary>
        No,
        /// <summary>
        /// Term vector is stored with offset information.
        /// </summary>
        WithOffsets,
        /// <summary>
        /// Term vector is stored with position information.
        /// </summary>
        WithPositions,
        /// <summary>
        /// Term vector is stored with position and offset information.
        /// </summary>
        WithPositionsOffsets,
        /// <summary>
        /// Term vector is stored.
        /// </summary>
        Yes
    }

    /// <summary>
    /// Represents a field in the index.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}, Mode:{Mode}, Store:{Store}, TermVector:{TermVector}")]
    public class IndexField : SnTerm
    {
        /// <summary>
        /// Gets the IndexingMode of the field.
        /// </summary>
        public IndexingMode Mode { get; }
        /// <summary>
        /// Gets the IndexStoringMode of the field that describes whether the field's raw value is stored in the index or not.
        /// </summary>
        public IndexStoringMode Store { get; }
        /// <summary>
        /// Gets the IndexTermVector handling of the field.
        /// </summary>
        public IndexTermVector TermVector { get; }

        /// <summary>
        /// Initializes an instance of the IndexField with a named System.String value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.String value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, string value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named array of System.String and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">Array of System.String</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, string[] value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named System.Boolean value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Boolean value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, bool value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named System.Int32 value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int32 value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, int value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named System.Int64 value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int64 value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, long value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named System.Single value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Single value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, float value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named System.Double value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Double value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, double value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }
        /// <summary>
        /// Initializes an instance of the IndexField with a named System.DateTime value and indexing metadata.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.DateTime value</param>
        /// <param name="mode">Indexing mode.</param>
        /// <param name="store">Index storing mode.</param>
        /// <param name="termVector">Term vector handling.</param>
        public IndexField(string name, DateTime value, IndexingMode mode, IndexStoringMode store, IndexTermVector termVector) : base(name, value) { Mode = mode; Store = store; TermVector = termVector; }

        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool stored)
        {
            var im = Mode > IndexingMode.Analyzed ? "IM" + (int)Mode + "," : string.Empty;
            var sm = Store > IndexStoringMode.No ? "SM" + (int)Store + "," : string.Empty;
            var tv = TermVector > IndexTermVector.No ? "TV" + (int)TermVector + "," : string.Empty;

            return stored
                ? $"{im}{tv}{base.ToString()}"
                : $"{im}{sm}{tv}{base.ToString()}";
        }

        public static IndexField Parse(string src, bool stored)
        {
            var mode = IndexingMode.Default;
            var store = stored ? IndexStoringMode.Yes : IndexStoringMode.Default;
            var termVector = IndexTermVector.Default;

            var p1 = src.IndexOf(':');
            var p0 = src.IndexOf(',');
            while (p0 > 0 && p0 < p1)
            {
                var flag = src.Substring(0, 2);
                int.TryParse(src[2].ToString(), out var flagValue);
                src = src.Substring(4);
                switch (flag)
                {
                    case "IM": mode = (IndexingMode) flagValue; break;
                    case "SM": store = (IndexStoringMode) flagValue; break;
                    case "TV": termVector = (IndexTermVector) flagValue; break;
                }

                p0 = src.IndexOf(',');
            }

            // Split name, value, type
            p0 = src.IndexOf(':');
            p1 = src.LastIndexOf(':');
            var name = src.Substring(0, p0);
            var value = src.Substring(p0 + 1, p1 - p0 - 1);
            var typeFlag = src.Substring(p1 + 1);
            switch (typeFlag)
            {
                case "S":
                    return new IndexField(name, value, mode, store, termVector);
                case "A":
                    throw new NotSupportedException();
                case "B":
                    return new IndexField(name, value == IndexValue.Yes, mode, store, termVector);
                case "I":
                    return new IndexField(name, int.Parse(value), mode, store, termVector);
                case "L":
                    return new IndexField(name, long.Parse(value), mode, store, termVector);
                case "F":
                    return new IndexField(name, float.Parse(value), mode, store, termVector);
                case "D":
                    return new IndexField(name, double.Parse(value), mode, store, termVector);
                case "T":
                    return new IndexField(name, DateTime.Parse(value), mode, store, termVector);
                default:
                    throw new ArgumentException("Unknown type flag: " + typeFlag);
            }
        }
    }
}
