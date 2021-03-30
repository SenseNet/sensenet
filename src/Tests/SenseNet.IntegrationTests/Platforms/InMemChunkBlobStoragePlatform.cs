using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.Platforms
{
    public class InMemChunkBlobStoragePlatform : InMemBlobStoragePlatform
    {
        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;
        public override Type ExpectedExternalBlobProviderType => typeof(InMemoryChunkBlobProvider);
        public override Type ExpectedBlobProviderDataType => typeof(InMemoryChunkBlobProviderData);
        public override bool UseChunk => true;

        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new TestBlobProviderSelector(typeof(InMemoryChunkBlobProvider), false);
        }
        public override IEnumerable<IBlobProvider> GetBlobProviders()
        {
            return new[] { new InMemoryChunkBlobProvider() };
        }

        protected override byte[][] GetRawData(string blobProvider, string blobProviderData)
        {
            var provider = (InMemoryChunkBlobProvider)BlobStorage.GetProvider(blobProvider);
            var providerAcc = new ObjectAccessor(provider);
            var providerData = (InMemoryChunkBlobProviderData)provider.ParseData(blobProviderData);

            var data = (Dictionary<Guid, byte[][]>)providerAcc.GetField("_blobStorage");
            return data[providerData.Id];
        }
    }
}
