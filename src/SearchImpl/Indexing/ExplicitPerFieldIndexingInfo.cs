using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Contains detailed per field indexing information
    /// </summary>
    public sealed class ExplicitPerFieldIndexingInfo
    {
        public string ContentTypeName { get; internal set; }
        public string ContentTypePath { get; internal set; }
        public string FieldName { get; internal set; }
        public string FieldTitle { get; internal set; }
        public string FieldDescription { get; internal set; }
        public string FieldType { get; internal set; }
        public IndexFieldAnalyzer Analyzer { get; internal set; }
        public string IndexHandler { get; internal set; }
        public string IndexingMode { get; internal set; }
        public string IndexStoringMode { get; internal set; }
        public string TermVectorStoringMode { get; internal set; }
    }
}
