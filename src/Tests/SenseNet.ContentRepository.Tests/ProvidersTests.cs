using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Tests
{
    public class TestProvider
    {
        public int TestProperty { get; set; }
    }

    [TestClass]
    public class ProvidersTests
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
    }
}
