using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using SenseNet.Tests.Implementations2;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderExtensionTests : TestBase
    {
        [TestMethod]
        public void DataProviderExtension_CallingInterfaceMethod()
        {
            var dp = new InMemoryTestingDataProvider();
            var builder = CreateRepositoryBuilderForTest();
            builder.UseTestingDataProviderExtension(dp);
            dp.DB.LogEntries.Insert(new LogEntryDoc {Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-2.1d)});
            dp.DB.LogEntries.Insert(new LogEntryDoc {Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-2.1d)});
            dp.DB.LogEntries.Insert(new LogEntryDoc {Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-1.1d)});
            dp.DB.LogEntries.Insert(new LogEntryDoc {Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-1.1d)});
            dp.DB.LogEntries.Insert(new LogEntryDoc {Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-0.1d)});
            dp.DB.LogEntries.Insert(new LogEntryDoc {Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-0.1d)});

            var testingDataProvider = DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();
            testingDataProvider.InitializeForTests();

            // ACTION
            // Call an interface method
            var actual = testingDataProvider.GetPermissionLogEntriesCountAfterMoment(DateTime.UtcNow.AddDays(-2));

            // ASSERT
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void DataProviderExtension_CallingNotInterfaceMethod()
        {
            var builder = CreateRepositoryBuilderForTest();
            builder.UseTestingDataProviderExtension(new InMemoryTestingDataProvider());

            // ACTION: Call a not interface method
            var actual = ((InMemoryTestingDataProvider) DataStore
                    .GetDataProviderExtension<ITestingDataProviderExtension>())
                .TestMethodThatIsNotInterfaceMember("asdf");

            // ASSERT
            Assert.AreEqual("asdfasdf", actual);
        }
    }
}