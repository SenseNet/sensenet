using SenseNet.ContentRepository.InMemory;
using SenseNet.Search;

namespace SenseNet.IntegrationTests.MsSql.Platforms
{
    public class MsSqlDataProviderPlatform : MsSqlPlatform
    {
        public override ISearchEngine GetSearchEngine()
        {
            return new InMemorySearchEngine(new InMemoryIndex());
        }
    }
}
