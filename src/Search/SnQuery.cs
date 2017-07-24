using System;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        public string Querytext { get; internal set; }
        public string Projection { get; internal set; }
    }
}