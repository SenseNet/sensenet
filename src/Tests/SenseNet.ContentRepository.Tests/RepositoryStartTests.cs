using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.Security.Data;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class RepositoryStartTests
    {
        [TestMethod]
        public void NamedProviders()
        {
            var dbProvider = new InMemoryDataProvider();
            var securityDbProvider = new MemoryDataProvider(DatabaseStorage.CreateEmpty());
            var searchEngine = new InMemorySearchEngine();
            var accessProvider = new DesktopAccessProvider();
            var emvrProvider = new ElevatedModificationVisibilityRule();

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartLuceneManager(false)
                .StartWorkflowEngine(false);

            using (var repo = Repository.Start(repoBuilder))
            {
                Assert.AreEqual(dbProvider, DataProvider.Current);
                Assert.AreEqual(searchEngine, StorageContext.Search.SearchEngine);
                Assert.AreEqual(accessProvider, AccessProvider.Current);
                Assert.AreEqual(emvrProvider, Providers.Instance.ElevatedModificationVisibilityRuleProvider);

                // Currently this does not work, because the property below re-creates the security 
                // db provider from the prototype, so it cannot be ref equal with the original.
                // Assert.AreEqual(securityDbProvider, SecurityHandler.SecurityContext.DataProvider);
                Assert.AreEqual(securityDbProvider, Providers.Instance.SecurityDataProvider);
            }
        }
    }
}
