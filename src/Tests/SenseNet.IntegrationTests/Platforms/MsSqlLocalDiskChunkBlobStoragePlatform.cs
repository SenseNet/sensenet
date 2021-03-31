using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.Platforms
{
    public class MsSqlLocalDiskChunkBlobStoragePlatform : MsSqlBuiltInBlobStoragePlatform
    {
        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;

        public override Type ExpectedExternalBlobProviderType => typeof(LocalDiskChunkBlobProvider);
        public override Type ExpectedBlobProviderDataType => typeof(LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData);
        public override bool UseChunk => true;

        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new TestBlobProviderSelector(typeof(LocalDiskChunkBlobProvider), true);
        }
        public override IEnumerable<IBlobProvider> GetBlobProviders()
        {
            return new[] { new LocalDiskChunkBlobProvider() };
        }

        protected override async Task<byte[][]> GetRawDataAsync(int fileId)
        {
            string blobProvider = null;
            string blobProviderData = null;
            byte[] buffer = null;

            using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString,
                new DataOptions(), CancellationToken.None))
            {
                var script = "SELECT BlobProvider, BlobProviderData, [Stream] FROM Files WHERE FileId = @FileId";
                var _ = await ctx.ExecuteReaderAsync(script, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FileId", SqlDbType.Int, fileId)
                    });
                }, async (reader, cancel) =>
                {
                    if (await reader.ReadAsync(cancel))
                    {
                        blobProvider = reader.GetSafeString("BlobProvider");
                        blobProviderData = reader.GetSafeString("BlobProviderData");
                        buffer = reader.GetSafeByteArray("Stream");
                    }
                    return true;
                }).ConfigureAwait(false);
            }

            if (blobProvider == null)
            {
                if (buffer.Length == 0)
                    return new byte[0][];
                return new[] { buffer };
            }

            return GetRawDataAsync(blobProvider, blobProviderData);
        }
        private byte[][] GetRawDataAsync(string blobProvider, string blobProviderData)
        {
            var provider = (LocalDiskChunkBlobProvider)BlobStorage.GetProvider(blobProvider);
            var providerData = (LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData)provider.ParseData(blobProviderData);

            var providerAcc = new ObjectAccessor(provider);
            var rootPath = (string)providerAcc.GetField("_rootDirectory");
            var path = Path.Combine(rootPath, providerData.Id.ToString());

            var files = Directory.GetFiles(path).OrderBy(f => f).ToArray();
            var buffers = new byte[files.Length][];
            for (int i = 0; i < buffers.Length; i++)
            {
                using (var stream = new FileStream(files[i], FileMode.Open))
                {
                    buffers[i] = new byte[stream.Length];
                    stream.Read(buffers[i], 0, buffers[i].Length);
                }
            }

            return buffers;
        }
    }
}
