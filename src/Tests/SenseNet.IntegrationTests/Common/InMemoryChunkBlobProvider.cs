using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.IntegrationTests.Common
{
    public class InMemoryChunkBlobProviderSelector : IBlobProviderSelector
    {
        public IBlobProvider GetProvider(long fullSize, Dictionary<string, IBlobProvider> providers, IBlobProvider builtIn)
        {
            return providers[typeof(InMemoryChunkBlobProvider).FullName];
        }
    }

    public class InMemoryChunkBlobProviderData
    {
        public Guid Id { get; set; }
        public string Trace { get; set; }
        public int ChunkSize { get; set; }
    }

    public class InMemoryChunkBlobProvider : IBlobProvider
    {
        public static int ChunkSizeInBytes { get; set; } = 10;

        private Dictionary<Guid, byte[][]> _blobStorage = new Dictionary<Guid, byte[][]>();

        public Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var chunkCount = context.Length / ChunkSizeInBytes;
            if (context.Length % ChunkSizeInBytes > 0)
                chunkCount++;
            _blobStorage.Add(id, new byte[chunkCount][]);

            context.BlobProviderData = new InMemoryChunkBlobProviderData { Id = id, ChunkSize = ChunkSizeInBytes };
            return Task.CompletedTask;
        }

        public Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken)
        {
            var providerData = (InMemoryChunkBlobProviderData)context.BlobProviderData;
            var originalChunkSize = providerData.ChunkSize;

            AssertValidChunks(context.Length, originalChunkSize, offset, buffer.Length);

            var container = _blobStorage[providerData.Id];
            var length = buffer.Length;
            var sourceOffset = 0;
            while (GetNextChunk(originalChunkSize, buffer, ref length, ref offset, ref sourceOffset, out var bytes, out var chunkIndex))
                container[chunkIndex] = bytes;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var data = (InMemoryChunkBlobProviderData)context.BlobProviderData;
            _blobStorage.Remove(data.Id);
            return Task.CompletedTask;
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var providerData = (InMemoryChunkBlobProviderData)context.BlobProviderData;
            return new InMemoryChunkReaderStream(providerData, context.Length, _blobStorage[providerData.Id]);
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            var providerData = (InMemoryChunkBlobProviderData)context.BlobProviderData;
            return new InMemoryChunkWriterStream(providerData, context.Length, _blobStorage[providerData.Id]);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            throw new NotImplementedException();
        }

        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<InMemoryChunkBlobProviderData>(providerData);
        }

        /*====================================================================================================*/

        //[SuppressMessage("ReSharper", "UnusedParameter.Local")]
        //[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static void AssertValidChunks(long currentBlobSize, int chunkSize, long offset, int size)
        {
            if (offset % chunkSize > 0)
                throw new Exception("Invalid offset");
        }

        private bool GetNextChunk(int originalChunkSize, byte[] buffer, ref int remainingLength, ref long offset, ref int sourceOffset, out byte[] bytes, out int chunkIndex)
        {
            if (remainingLength <= 0)
            {
                chunkIndex = 0;
                bytes = null;
                return false;
            }

            chunkIndex = Convert.ToInt32(offset / originalChunkSize);

            var currentChunkLength = Math.Min(originalChunkSize, remainingLength);
            bytes = new byte[currentChunkLength];

            Array.ConstrainedCopy(buffer, sourceOffset, bytes, 0, currentChunkLength);

            remainingLength -= currentChunkLength;
            offset += originalChunkSize;
            sourceOffset += originalChunkSize;

            return true;
        }
        private void WriteChunkAsync(Guid id, int chunkIndex, byte[] bytes)
        {
            _blobStorage[id][chunkIndex] = bytes;
        }
    }
}
