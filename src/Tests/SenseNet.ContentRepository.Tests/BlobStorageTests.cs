using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class BlobStorageTests
    {
        [TestMethod]
        public void BlobStorage_Services_Basic()
        {
            var provider = BuildServiceProvider();
            var bs = provider.GetService<IBlobStorage>();

            Assert.IsNotNull(bs);
        }
        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSenseNetBlobStorage();

            var provider = services.BuildServiceProvider();

            return provider;
        }
    }
}
