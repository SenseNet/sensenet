using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Contains detailed per field indexing information defined in a the Content Type Definition.
    /// </summary>
    public sealed class ExplicitPerFieldIndexingInfo
    {
        /// <summary>
        /// Gets the name of the <see cref="ContentType"/>.
        /// </summary>
        public string ContentTypeName { get; internal set; }
        /// <summary>
        /// Gets the path of the <see cref="ContentType"/>.
        /// </summary>
        public string ContentTypePath { get; internal set; }
        /// <summary>
        /// Gets the name of the <see cref="Field"/>.
        /// </summary>
        public string FieldName { get; internal set; }
        /// <summary>
        /// Gets the display name of the <see cref="Field"/>.
        /// </summary>
        public string FieldTitle { get; internal set; }
        /// <summary>
        /// Gets the description name of the <see cref="Field"/>.
        /// </summary>
        public string FieldDescription { get; internal set; }
        /// <summary>
        /// Gets the short type name of the <see cref="Field"/>.
        /// </summary>
        public string FieldType { get; internal set; }
        /// <summary>
        /// Gets the <see cref="IndexFieldAnalyzer"/> choice of the <see cref="Field"/>.
        /// </summary>
        public IndexFieldAnalyzer Analyzer { get; internal set; }
        /// <summary>
        /// Gets the type name of the FieldIndexHandler of the <see cref="Field"/>.
        /// </summary>
        public string IndexHandler { get; internal set; }
        /// <summary>
        /// Gets the indexing mode of the <see cref="Field"/>.
        /// </summary>
        public string IndexingMode { get; internal set; }
        /// <summary>
        /// Gets the index storing mode of the <see cref="Field"/>.
        /// </summary>
        public string IndexStoringMode { get; internal set; }
        /// <summary>
        /// Gets the term vector storing mode of the <see cref="Field"/>.
        /// </summary>
        public string TermVectorStoringMode { get; internal set; }
    }
}
