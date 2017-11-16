namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Defines the Contnt Query Language related constants.
    /// </summary>
    public static class Cql
    {
        /// <summary>
        /// A System.String that contains all string-end characters.
        /// If a string value contains any characters from these, 
        /// the value should enveloped by the quotation marks or apostrophes.
        /// </summary>
        public const string StringTerminatorChars = "\":+-&|!(){}[]^~";

        /// <summary>
        /// Defines constants for keywords of the Contnt Query Language
        /// </summary>
        public static class Keyword
        {
            /// <summary>Value: "OR"</summary>
            public const string Or = "OR";
            /// <summary>Value: "AND"</summary>
            public const string And = "AND";
            /// <summary>Value: "NOT"</summary>
            public const string Not = "NOT";
            /// <summary>Value: "TO"</summary>
            public const string To = "TO";

            /// <summary>Value: ".SELECT"</summary>
            public const string Select = ".SELECT";
            /// <summary>Value: ".SKIP"</summary>
            public const string Skip = ".SKIP";
            /// <summary>Value: ".TOP"</summary>
            public const string Top = ".TOP";
            /// <summary>Value: ".SORT"</summary>
            public const string Sort = ".SORT";
            /// <summary>Value: ".REVERSESORT"</summary>
            public const string ReverseSort = ".REVERSESORT";
            /// <summary>Value: ".AUTOFILTERS"</summary>
            public const string Autofilters = ".AUTOFILTERS";
            /// <summary>Value: ".LIFESPAN"</summary>
            public const string Lifespan = ".LIFESPAN";
            /// <summary>Value: ".COUNTONLY"</summary>
            public const string CountOnly = ".COUNTONLY";
            /// <summary>Value: ".QUICK"</summary>
            public const string Quick = ".QUICK";
            /// <summary>Value: ".ALLVERSIONS"</summary>
            public const string AllVersions = ".ALLVERSIONS";

            /// <summary>Value: "ON"</summary>
            public const string On = "ON";
            /// <summary>Value: "OFF"</summary>
            public const string Off = "OFF";
        }
    }
}
