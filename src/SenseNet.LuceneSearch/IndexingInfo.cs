using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Indexing;

namespace SenseNet.LuceneSearch
{
    /// <summary>
    /// Local helper class for indexinginfo-related constants.
    /// </summary>
    internal class IndexingInfo
    {
        public static readonly IndexingMode DefaultIndexingMode = IndexingMode.Analyzed;
        public static readonly IndexStoringMode DefaultIndexStoringMode = IndexStoringMode.No;
        public static readonly IndexTermVector DefaultTermVectorStoringMode = IndexTermVector.No;

        //UNDONE: make sure GetPerFieldIndexingInfo is not needed
        public static IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            //UNDONE: implement a local indexing info store that is refreshed when a CTD changes
            throw new NotImplementedException();
        }
    }
}
