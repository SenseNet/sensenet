using SenseNet.Search;
// ReSharper disable InconsistentNaming

namespace SenseNet.Tests.SelfTest
{
    internal class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "Name:@0"</summary>
        public static string Name => "Name:@0";
        /// <summary>Returns with the following query: "@0:@0"</summary>
        public static string OneTerm => "@0:@1";
        /// <summary>Returns with the following query: "@0:&gt;@0"</summary>

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

        public static string Recursive => "Id:{{@0:@1 .SELECT:OwnerId}}";
    }
}
