using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    public class TestProvider
    {
        public int TestProperty { get; set; }
    }

    public interface IConfiguredTestProvider
    {
        void DoIt();
    }
    public abstract class ConfiguredTestProviderBase: IConfiguredTestProvider
    {
        public abstract void DoIt();
    }
    public class TestProviderA : ConfiguredTestProviderBase
    {
        public override void DoIt()
        {
            throw new System.NotImplementedException();
        }
    }
    public class TestProviderB : ConfiguredTestProviderBase
    {
        public override void DoIt()
        {
            throw new System.NotImplementedException();
        }
    }

    [TestClass]
    public class ProvidersTests : TestBase
    {
        [TestMethod]
        public void Provider_ByType()
        {
            // reset
            Providers.Instance.SetProvider("TestProvider", null);
            Providers.Instance.SetProvider(typeof(TestProvider), null);

            var p1 = Providers.Instance.GetProvider<TestProvider>();
            Assert.IsNull(p1);

            Providers.Instance.SetProvider(typeof(TestProvider), new TestProvider { TestProperty = 123 });

            p1 = Providers.Instance.GetProvider<TestProvider>();
            Assert.AreEqual(123, p1.TestProperty);
        }

        [TestMethod]
        public void Provider_ByName()
        {
            // reset
            Providers.Instance.SetProvider("TestProvider", null);
            Providers.Instance.SetProvider(typeof(TestProvider), null);

            var p1 = Providers.Instance.GetProvider<TestProvider>("TestProvider");
            Assert.IsNull(p1);

            Providers.Instance.SetProvider("TestProvider", new TestProvider { TestProperty = 456 });

            // get by type: still null
            p1 = Providers.Instance.GetProvider<TestProvider>();
            Assert.IsNull(p1);

            p1 = Providers.Instance.GetProvider<TestProvider>("TestProvider");
            Assert.AreEqual(456, p1.TestProperty);
        }

        [TestMethod]
        public void Provider_Configured_ByName()
        {
            var providersInstanceAcc = new ObjectAccessor(Providers.Instance);
            var providersByName = (Dictionary<string, object>) providersInstanceAcc.GetFieldOrProperty("_providersByName");
            providersByName.Clear();

            try
            {
                var p1 = Providers.Instance.GetProvider<IConfiguredTestProvider>("configured-provider-a");
                Assert.IsTrue(p1 is TestProviderA);
                Assert.AreEqual(1, providersByName.Count);

                var p2 = Providers.Instance.GetProvider<ConfiguredTestProviderBase>("configured-provider-a");
                Assert.IsTrue(p2 is TestProviderA);
                Assert.AreEqual(1, providersByName.Count);

                var p3 = Providers.Instance.GetProvider<IConfiguredTestProvider>("configured-provider-b");
                Assert.IsTrue(p3 is TestProviderB);
                Assert.AreEqual(2, providersByName.Count);

                var p4 = Providers.Instance.GetProvider<ConfiguredTestProviderBase>("configured-provider-b");
                Assert.IsTrue(p4 is TestProviderB);
                Assert.AreEqual(2, providersByName.Count);
            }
            finally
            {
                providersByName.Clear();
            }
        }
    }
}
