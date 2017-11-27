using System;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Defines metadata for indexing a field.
    /// </summary>
    public interface IPerFieldIndexingInfo
    {
        /// <summary>
        /// Gets or sets the used analyzer category of the field.
        /// </summary>
        IndexFieldAnalyzer Analyzer { get; set; }
        /// <summary>
        /// Gets or sets the converter class instance matching the field's data type.
        /// </summary>
        IFieldIndexHandler IndexFieldHandler { get; set; }

        /// <summary>
        /// Gets or sets the field's indexing mode.
        /// </summary>
        IndexingMode IndexingMode { get; set; }
        /// <summary>
        /// Gets or sets the field's storing mode.
        /// </summary>
        IndexStoringMode IndexStoringMode { get; set; }
        /// <summary>
        /// Gets or sets the term vector usage of the field.
        /// </summary>
        IndexTermVector TermVectorStoringMode { get; set; }

        /// <summary>
        /// Gets a value that is true if the field is indexed or sored in the index.
        /// This is a shorcut of aggregated value of the IndexingMode and IndexStoringMode.
        /// </summary>
        bool IsInIndex { get; }

        /// <summary>
        /// Gets or sets the System.Type of the field's native value.
        /// </summary>
        Type FieldDataType { get; set; }
    }
}
