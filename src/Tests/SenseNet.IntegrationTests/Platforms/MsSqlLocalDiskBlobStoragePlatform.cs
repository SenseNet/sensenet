using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.Platforms
{
    public class MsSqlLocalDiskBlobStoragePlatform : MsSqlBuiltInBlobStoragePlatform
    {
        public override Type ExpectedExternalBlobProviderType => typeof(LocalDiskBlobProvider);
        public override Type ExpectedBlobProviderDataType => typeof(LocalDiskBlobProvider.LocalDiskBlobProviderData);
        public override bool UseChunk => false;

        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new TestBlobProviderSelector(typeof(LocalDiskBlobProvider), true);
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
                return new [] {buffer};
            }

            return GetRawDataAsync(blobProvider, blobProviderData);
        }

        private byte[][] GetRawDataAsync(string blobProvider, string blobProviderData)
        {
            var provider = (LocalDiskBlobProvider)BlobStorageBase.GetProvider(blobProvider);
            var providerData = (LocalDiskBlobProvider.LocalDiskBlobProviderData)provider.ParseData(blobProviderData);

            var providerAcc = new ObjectAccessor(provider);
            var rootPath = (string)providerAcc.GetField("_rootDirectory");
            var path = Path.Combine(rootPath, providerData.Id.ToString());
            byte[] buffer;
            using (var stream = new FileStream(path, FileMode.Open))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            return new [] {buffer};
        }
    }
}
