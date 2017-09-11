using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "Name:@0"</summary>
        public static string Name => "Name:@0";
        /// <summary>Returns with the following query: "@0:@0"</summary>
        public static string OneTerm => "@0:@1";
        /// <summary>Returns with the following query: "@0:&gt;@0"</summary>
        public static string GT => "@0:>@1";
        /// <summary>Returns with the following query: "@0:&lt;@0"</summary>
        public static string LT => "@0:<@1";
        /// <summary>Returns with the following query: "@0:&gt;=@0"</summary>
        public static string GTE => "@0:>=@1";
        /// <summary>Returns with the following query: "@0:&lt;=@0"</summary>
        public static string LTE => "@0:<=@1";
        /// <summary>Returns with the following query: "@0:[@1 TO @2]"</summary>
        public static string BracketBracketRange => "@0:[@1 TO @2]";
        /// <summary>Returns with the following query: "@0:[@1 TO @2}"</summary>
        public static string BracketBraceRange => "@0:[@1 TO @2}";
        /// <summary>Returns with the following query: "@0:{@1 TO @2]"</summary>
        public static string BraceBracketRange => "@0:{@1 TO @2]";
        /// <summary>Returns with the following query: "@0:{@1 TO @2}"</summary>
        public static string BraceBraceRange => "@0:{@1 TO @2}";

        /// <summary>Returns with the following query: "@0:@1 @2:@3"</summary>
        public static string TwoTermsShouldShould => "@0:@1 @2:@3";
        /// <summary>Returns with the following query: "+@0:@1 +@2:@3"</summary>
        public static string TwoTermsMustMust => "+@0:@1 +@2:@3";
        /// <summary>Returns with the following query: "+@0:@1 -@2:@3"</summary>
        public static string TwoTermsMustNot => "+@0:@1 -@2:@3";

        /// <summary>Returns with the following query: "(+@0:@1 +@2:@3) (+@4:@5 +@6:@7)"</summary>
        public static string MultiLevelBool1 => "(+@0:@1 +@2:@3) (+@4:@5 +@6:@7)";
        /// <summary>Returns with the following query: "+(@0:@1 @2:@3) +(@4:@5 @6:@7)"</summary>
        public static string MultiLevelBool2 => "+(@0:@1 @2:@3) +(@4:@5 @6:@7)";
    }
}
