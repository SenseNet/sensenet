using System;

namespace SenseNet.Search.Indexing
{
    //UNDONE:!!!! XMLDOC ContentRepository
    public class PerFieldIndexingInfo : IPerFieldIndexingInfo
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        public static readonly IndexingMode DefaultIndexingMode = IndexingMode.Analyzed;
        //UNDONE:!!!! XMLDOC ContentRepository
        public static readonly IndexStoringMode DefaultIndexStoringMode = IndexStoringMode.No;
        //UNDONE:!!!! XMLDOC ContentRepository
        public static readonly IndexTermVector DefaultTermVectorStoringMode = IndexTermVector.No;

        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexFieldAnalyzer Analyzer { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IFieldIndexHandler IndexFieldHandler { get; set; }

        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexingMode IndexingMode { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexStoringMode IndexStoringMode { get; set; }
        //UNDONE:!!!! XMLDOC ContentRepository
        public IndexTermVector TermVectorStoringMode { get; set; }

        //UNDONE:!!!! XMLDOC ContentRepository
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

        //UNDONE:!!!! XMLDOC ContentRepository
        public Type FieldDataType { get; set; }
    }
}
