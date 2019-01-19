using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderExtensionTests : TestBase
    {
        [TestMethod]
        public void DataProviderExtension_CallingInterfaceMethod()
        {
            // ARRANGE
            var dp = new InMemoryTestingDataProvider();
            var builder = CreateRepositoryBuilderForTest();
            builder.UseTestingDataProvider(dp);
            dp.DB.LogEntries.AddRange(new[]
            {
                new InMemoryDataProvider.LogEntriesRow{Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-2.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-2.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-1.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-1.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-0.1d)},
                new InMemoryDataProvider.LogEntriesRow{Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-0.1d)},
            });

            var testingDataProvider = DataProvider.Instance<ITestingDataProvider>();
            testingDataProvider.InitializeForTests();

            // ACTION
            // Call an interface method
            var actual = testingDataProvider.GetPermissionLogEntriesCountAfterMoment(DateTime.UtcNow.AddDays(-2));

            // ASSERT
            Assert.AreEqual(2, actual);
        }
        [TestMethod]
        public void DataProviderExtension__CallingNotInterfaceMethod()
        {
            var builder = CreateRepositoryBuilderForTest();
            builder.UseTestingDataProvider(new InMemoryTestingDataProvider());

            // ACTION
            // Call a not interface method
            var actual = ((InMemoryTestingDataProvider)DataProvider.Instance<ITestingDataProvider>())
                .TestMethodThatIsNotInterfaceMember("asdf");

            // ASSERT
            Assert.AreEqual("asdfasdf", actual);
        }

    }
}