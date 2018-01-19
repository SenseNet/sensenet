using System;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Implements the metadata for indexing a field.
    /// </summary>
    public class PerFieldIndexingInfo : IPerFieldIndexingInfo
    {
        /// <summary>
        /// Default value of the <see cref="IndexingMode"/>.
        /// </summary>
        public static readonly IndexingMode DefaultIndexingMode = IndexingMode.Analyzed;
        /// <summary>
        /// Default value of the <see cref="IndexStoringMode"/>.
        /// </summary>
        public static readonly IndexStoringMode DefaultIndexStoringMode = IndexStoringMode.No;
        /// <summary>
        /// Default value of the <see cref="IndexTermVector"/>.
        /// </summary>
        public static readonly IndexTermVector DefaultTermVectorStoringMode = IndexTermVector.No;

        /// <inheritdoc />
        public IndexFieldAnalyzer Analyzer { get; set; }
        /// <inheritdoc />
        public IFieldIndexHandler IndexFieldHandler { get; set; }

        /// <inheritdoc />
        public IndexingMode IndexingMode { get; set; }
        /// <inheritdoc />
        public IndexStoringMode IndexStoringMode { get; set; }
        /// <inheritdoc />
        public IndexTermVector TermVectorStoringMode { get; set; }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Type FieldDataType { get; set; }
    }
}
