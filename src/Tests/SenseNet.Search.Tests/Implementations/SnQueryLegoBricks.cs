using SenseNet.Search.Parser;

namespace SenseNet.Search.Tests.Implementations
{
    internal class SnQueryLegoBricks
    {
        public IPermissionFilterFactory PermissionFilterFactory;
        public IQueryEngineSelector QueryEngineSelector;
        public IQueryParserFactory QueryParserFactory;
    }
}
