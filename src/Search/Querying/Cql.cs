namespace SenseNet.Search.Querying
{
    public static class Cql
    {
        public const string StringTerminatorChars = "\":+-&|!(){}[]^~";

        public static class Keyword
        {
            public const string Or = "OR";
            public const string And = "AND";
            public const string Not = "NOT";
            public const string To = "TO";

            public const string Select = ".SELECT";
            public const string Skip = ".SKIP";
            public const string Top = ".TOP";
            public const string Sort = ".SORT";
            public const string ReverseSort = ".REVERSESORT";
            public const string Autofilters = ".AUTOFILTERS";
            public const string Lifespan = ".LIFESPAN";
            public const string CountOnly = ".COUNTONLY";
            public const string Quick = ".QUICK";
            public const string AllVersions = ".ALLVERSIONS";

            public const string On = "ON";
            public const string Off = "OFF";
        }
    }
}
