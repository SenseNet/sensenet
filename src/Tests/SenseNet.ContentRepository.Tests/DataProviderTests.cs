using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderTests : TestBase
    {
        [TestMethod]
        public void DP_Create()
        {
            DPTest(() =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };

                // ACTION
                using (DataStore.CheckerBlock())
                    folder.Save();

                // ASSERT managed in the CheckerBlock.
            });
        }
        [TestMethod]
        public void DP_Update()
        {
            DPTest(() =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                folder.Save();
                folder = Node.Load<SystemFolder>(folder.Id);
                folder.Index++;

                // ACTION
                using (DataStore.CheckerBlock())
                    folder.Save();

                // ASSERT managed in the CheckerBlock.
            });
        }

        private void DPTest(Action callback)
        {
            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            Providers.Instance.DataProvider2 = new InMemoryDataProvider2();
            using (Repository.Start(builder))
            {
                using (new SystemAccount())
                    callback();
            }
        }

    }
}
