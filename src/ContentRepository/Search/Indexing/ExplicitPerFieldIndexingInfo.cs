using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Contains detailed per field indexing information
    /// </summary>
    public sealed class ExplicitPerFieldIndexingInfo
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        public string ContentTypeName { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string ContentTypePath { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string FieldName { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string FieldTitle { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string FieldDescription { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string FieldType { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexFieldAnalyzer Analyzer { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string IndexHandler { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string IndexingMode { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string IndexStoringMode { get; internal set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public string TermVectorStoringMode { get; internal set; }
    }
}
