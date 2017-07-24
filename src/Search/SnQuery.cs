using System;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        public string Querytext { get; internal set; }
        public string Projection { get; internal set; }
        public int Top { get; internal set; }
        public int Skip { get; internal set; }
        public SortInfo Order { get; internal set; }

        internal SnQueryNode QueryTree { get; set; }
    }
}