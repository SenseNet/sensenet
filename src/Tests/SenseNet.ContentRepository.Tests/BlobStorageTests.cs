using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
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

            // check if the service is accessible
            Assert.IsNotNull(bs);
        }
        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.Configure<DataOptions>(options => {});
            services.Configure<BlobStorageOptions>(options => {});
            services.AddSenseNetBlobStorage();

            // make sure the services can be registered and there is no circular reference
            var provider = services.BuildServiceProvider();

            return provider;
        }
    }
}
