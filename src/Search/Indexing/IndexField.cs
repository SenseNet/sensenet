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
    }
}
