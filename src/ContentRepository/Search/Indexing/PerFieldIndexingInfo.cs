using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace SenseNet.Search.Indexing
{
    public class PerFieldIndexingInfo
    {
        public static readonly Field.Index DefaultIndexingMode = Field.Index.ANALYZED;
        public static readonly Field.Store DefaultIndexStoringMode = Field.Store.NO;
        public static readonly Field.TermVector DefaultTermVectorStoringMode = Field.TermVector.NO;

        public string Analyzer { get; set; }
        public FieldIndexHandler IndexFieldHandler { get; set; }

        public Field.Index IndexingMode { get; set; }
        public Field.Store IndexStoringMode { get; set; }
        public Field.TermVector TermVectorStoringMode { get; set; }

        public bool IsInIndex
        {
            get
            {
                if (IndexingMode == Lucene.Net.Documents.Field.Index.NO &&
                    (IndexStoringMode == null || IndexStoringMode == Lucene.Net.Documents.Field.Store.NO))
                    return false;
                return true;
            }
        }

        public Type FieldDataType { get; set; }
    }
}
