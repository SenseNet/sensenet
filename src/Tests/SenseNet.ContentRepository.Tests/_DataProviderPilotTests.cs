using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class _DataProviderPilotTests : TestBase
    {
        [TestMethod]
        public void Xxx_1()
        {
            var builder = base.CreateRepositoryBuilderForTest();
            builder.UseDataProviderForFeature1(new Feature1_DataProvider1());

            var actual = DataProvider.Instance<Feature1_DataProvider1>().Feature1_Method1();
            Assert.AreEqual($"{typeof(Feature1_DataProvider1).Name}.Feature1_Method1", actual);
        }
        [TestMethod]
        public void Xxx_2()
        {
            var builder = base.CreateRepositoryBuilderForTest();
            builder.UseDataProviderForFeature1(new Feature1_DataProvider2());

            var actual = DataProvider.Instance<Feature1_DataProvider1>().Feature1_Method1();
            Assert.AreEqual($"{typeof(Feature1_DataProvider2).Name}.Feature1_Method1", actual);
        }
        [TestMethod]
        public void Xxxxx_3()
        {
            var builder = base.CreateRepositoryBuilderForTest();
            builder.UseDataProviderForFeature1(new Feature1_DataProvider2());

            var actual = DataProvider.Instance<Feature1_DataProvider1>().Feature1_Method1();
            Assert.AreEqual($"{typeof(Feature1_DataProvider2).Name}.Feature1_Method1", actual);
        }
    }


    public static class RepositoryBuilderExtensions
    {
        public static IRepositoryBuilder UseDataProviderForFeature1(this IRepositoryBuilder repositoryBuilder, IFeature1_DataProvider provider)
        {
            DataProvider.Instance().SetProvider(provider.GetType(), provider);
            return repositoryBuilder;
        }
    }

    public interface IFeature1_DataProvider
    {
        string Feature1_Method1();
    }

    public class Feature1_DataProvider1 : IFeature1_DataProvider
    {
        public string Feature1_Method1()
        {
            var method = MethodBase.GetCurrentMethod();
            return $"{method.DeclaringType.Name}.{method.Name}";
        }
    }
    public class Feature1_DataProvider2 : IFeature1_DataProvider
    {
        public string Feature1_Method1()
        {
            var method = MethodBase.GetCurrentMethod();
            return $"{method.DeclaringType.Name}.{method.Name}";
        }
    }

}