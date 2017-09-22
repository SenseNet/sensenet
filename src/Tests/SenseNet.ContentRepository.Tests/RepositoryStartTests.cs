using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.Diagnostics;
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

            // switch this ON here for testing purposes (to check that repo start does not override it)
            SnTrace.Custom.Enabled = true;

            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dbProvider)
                .UseSecurityDataProvider(securityDbProvider)
                .UseSearchEngine(searchEngine)
                .UseAccessProvider(accessProvider)
                .UseElevatedModificationVisibilityRuleProvider(emvrProvider)
                .StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .UseTraceCategories(new[] {"Test", "Web", "System"});

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

                // Check a few trace categories that were switched ON above.
                Assert.IsTrue(SnTrace.Custom.Enabled);
                Assert.IsTrue(SnTrace.Test.Enabled);
                Assert.IsTrue(SnTrace.Web.Enabled);
                Assert.IsTrue(SnTrace.System.Enabled);
                Assert.IsFalse(SnTrace.TaskManagement.Enabled);
                Assert.IsFalse(SnTrace.Workflow.Enabled);
            }
        }
    }
}
