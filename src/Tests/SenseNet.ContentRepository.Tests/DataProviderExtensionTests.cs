﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderExtensionTests : TestBase
    {
        [TestMethod]
        public void DataProviderExtension_CallingInterfaceMethod()
        {
            var dp = new InMemoryTestingDataProvider();
            var builder = CreateRepositoryBuilderForTest(TestContext);
            builder.UseTestingDataProvider(dp);
            var table = dp.DB.LogEntries;
            table.Insert(new LogEntryDoc {Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-2.1d), LogId = table.GetNextId()});
            table.Insert(new LogEntryDoc {Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-2.1d), LogId = table.GetNextId()});
            table.Insert(new LogEntryDoc {Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-1.1d), LogId = table.GetNextId()});
            table.Insert(new LogEntryDoc {Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-1.1d), LogId = table.GetNextId()});
            table.Insert(new LogEntryDoc {Title = "ContentUpdated",    LogDate = DateTime.UtcNow.AddDays(-0.1d), LogId = table.GetNextId()});
            table.Insert(new LogEntryDoc {Title = "PermissionChanged", LogDate = DateTime.UtcNow.AddDays(-0.1d), LogId = table.GetNextId() });

            var testingDataProvider = Providers.Instance.GetProvider<ITestingDataProvider>();
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
            var builder = CreateRepositoryBuilderForTest(TestContext);
            builder.UseTestingDataProvider(new InMemoryTestingDataProvider());

            // ACTION: Call a not interface method
            var actual = ((InMemoryTestingDataProvider) Providers.Instance.GetProvider<ITestingDataProvider>())
                .TestMethodThatIsNotInterfaceMember("asdf");

            // ASSERT
            Assert.AreEqual("asdfasdf", actual);
        }
    }
}