namespace SenseNet.Search.Tests.Implementations
{
    internal class TestQueryEngineSelector : IQueryEngineSelector
    {
        public TestQueryEngine QueryEngine { get; set; }
        public IQueryEngine Select(SnQuery query, QuerySettings settings)
        {
            return QueryEngine;
        }
    }
}
