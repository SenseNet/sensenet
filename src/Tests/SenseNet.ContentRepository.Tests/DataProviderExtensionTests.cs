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
        public void DataProviderExtension_FeatureExtension_Type1()
        {
            var builder = CreateRepositoryBuilderForTest();
            builder.UseDataProviderForFeature1(new Platform1Feature1DataProvider());

            var actual = DataProvider.Instance<IFeature1DataProvider>().Feature1Method1();

            Assert.AreEqual($"{typeof(Platform1Feature1DataProvider).Name}.Feature1Method1", actual);
        }
        [TestMethod]
        public void DataProviderExtension_FeatureExtension_Type2()
        {
            var builder = CreateRepositoryBuilderForTest();
            builder.UseDataProviderForFeature1(new Platform2Feature1DataProvider());

            var actual = DataProvider.Instance<IFeature1DataProvider>().Feature1Method1();

            Assert.AreEqual($"{typeof(Platform2Feature1DataProvider).Name}.Feature1Method1", actual);
        }
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


    public static class RepositoryBuilderExtensions
    {
        public static IRepositoryBuilder UseDataProviderForFeature1(this IRepositoryBuilder repositoryBuilder, IFeature1DataProvider provider)
        {
            DataProvider.Instance().SetProvider(typeof(IFeature1DataProvider), provider);
            return repositoryBuilder;
        }

        //UNDONE: Move this method to the more general place (SenseNet.Tests?)
        public static IRepositoryBuilder UseTestingDataProvider(this IRepositoryBuilder repositoryBuilder, ITestingDataProvider provider)
        {
            DataProvider.Instance().SetProvider(typeof(ITestingDataProvider), provider);
            return repositoryBuilder;
        }
    }

    public interface IFeature1DataProvider : IDataProvider
    {
        string Feature1Method1();
    }

    public class Platform1Feature1DataProvider : IFeature1DataProvider
    {
        public DataProvider MetadataProvider { get; set; }

        public string Feature1Method1()
        {
            return "Platform1Feature1DataProvider.Feature1Method1";
        }
    }
    public class Platform2Feature1DataProvider : IFeature1DataProvider
    {
        public DataProvider MetadataProvider { get; set; }

        public string Feature1Method1()
        {
            return "Platform2Feature1DataProvider.Feature1Method1";
        }
    }

}