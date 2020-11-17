using SenseNet.ContentRepository;

namespace SenseNet.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        public abstract RepositoryBuilder GetRepositoryBuilder();
    }
}
