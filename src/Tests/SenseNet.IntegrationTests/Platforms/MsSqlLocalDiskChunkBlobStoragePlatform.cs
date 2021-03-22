using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Common;

namespace SenseNet.IntegrationTests.Platforms
{
    public class MsSqlLocalDiskChunkBlobStoragePlatform : MsSqlBuiltInBlobStoragePlatform
    {
        public override Type ExpectedExternalBlobProviderType => typeof(LocalDiskChunkBlobProvider);
        public override Type ExpectedBlobProviderDataType => typeof(LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData);
        public override bool UseChunk => true;

        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new TestBlobProviderSelector(typeof(LocalDiskChunkBlobProvider), true);
        }

        protected override async Task<byte[][]> GetRawDataAsync(int fileId)
        {
            throw new NotImplementedException();

            using (var ctx = new MsSqlDataContext(Configuration.ConnectionStrings.ConnectionString,
                new DataOptions(), CancellationToken.None))
            {
                var script = "SELECT [Stream] FROM Files WHERE FileId = @FileId";
                var bytes = await ctx.ExecuteScalarAsync(script, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FileId", SqlDbType.Int, fileId)
                    });
                }).ConfigureAwait(false);

                return new byte[][] { (byte[])bytes };
            }
        }
    }
}
