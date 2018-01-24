namespace SenseNet.Search
{
    internal class SafeQueriesInternal : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "+Id:@0"</summary>
        public static string ContentById => "+Id:@0";
    }
}
