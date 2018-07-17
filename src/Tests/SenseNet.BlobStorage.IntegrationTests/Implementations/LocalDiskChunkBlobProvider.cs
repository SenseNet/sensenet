using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.BlobStorage.IntegrationTests.Implementations
{
    internal class LocalDiskChunkBlobProvider : IBlobProvider
    {
        public static int ChunkByteSize { get;set; } = 10;

        internal class LocalDiskChunkBlobProviderData
        {
            public Guid Id { get; set; }
            public string Trace { get; set; }
            public int ChunkSize { get; set; }

        }

        private static string _rootDirectory;

        public LocalDiskChunkBlobProvider()
        {
            _rootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\" + GetType().Name);
            if (Directory.Exists(_rootDirectory))
            {
                foreach (var path in Directory.GetFiles(_rootDirectory))
                    File.Delete(path);
            }
            else
            {
                Directory.CreateDirectory(_rootDirectory);
            }
        }

        /*========================================================================== provider implementation */

        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<LocalDiskChunkBlobProviderData>(providerData);
        }

        public void Allocate(BlobStorageContext context)
        {
            var id = Guid.NewGuid();
            CreateFolder(id);
            context.BlobProviderData = new LocalDiskChunkBlobProviderData { Id = id, ChunkSize = ChunkByteSize };
        }

        public void Delete(BlobStorageContext context)
        {
            var id = GetData(context).Id;
            DeleteFolder(id);
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var providerData = (LocalDiskChunkBlobProviderData)context.BlobProviderData;
            return new FileSystemChunkReaderStream(providerData, context.Length, GetDirectoryPath(providerData.Id));
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            var providerData = (LocalDiskChunkBlobProviderData)context.BlobProviderData;
            return new FileSystemChunkWriterStream(providerData, context.Length);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            if (!(stream is FileSystemChunkReaderStream))
                throw new InvalidOperationException("Stream must be a FileSystemChunkReaderStream in the local disk provider.");

            return GetStreamForRead(context);
        }

        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            var providerData = (LocalDiskChunkBlobProviderData)context.BlobProviderData;
            var originalChunkSize = providerData.ChunkSize;

            AssertValidChunks(context.Length, originalChunkSize, offset, buffer.Length);

            var length = buffer.Length;
            var sourceOffset = 0;

            while (GetNextChunk(originalChunkSize, buffer, ref length, ref offset, ref sourceOffset, out var bytes, out var chunkIndex))
                WriteChunk(((LocalDiskChunkBlobProviderData)context.BlobProviderData).Id, chunkIndex, bytes);
        }
        public async System.Threading.Tasks.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            var providerData = (LocalDiskChunkBlobProviderData)context.BlobProviderData;
            var originalChunkSize = providerData.ChunkSize;

            AssertValidChunks(context.Length, originalChunkSize, offset, buffer.Length);

            var length = buffer.Length;
            var sourceOffset = 0;

            while (GetNextChunk(originalChunkSize, buffer, ref length, ref offset, ref sourceOffset, out var bytes, out var chunkIndex))
                await WriteChunkAsync(((LocalDiskChunkBlobProviderData)context.BlobProviderData).Id, chunkIndex, bytes);
        }

        /// <summary>
        /// Returns a fragment of the provided byte array and updates the index and offset parameters for the next iteration.
        /// </summary>
        private static bool GetNextChunk(int originalChunkSize, byte[] buffer, ref int remainingLength, ref long offset, ref int sourceOffset, out byte[] bytes, out int chunkIndex)
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

        /*====================================================================================================*/

        private static string GetDirectoryPath(Guid id)
        {
            return Path.Combine(_rootDirectory, id.ToString());
        }

        private static string GetFilePath(Guid id, int chunkIndex)
        {
            return Path.Combine(GetDirectoryPath(id), chunkIndex.ToString());
        }

        public static void WriteChunk(Guid id, int chunkIndex, byte[] bytes)
        {
            using (var stream = new FileStream(GetFilePath(id, chunkIndex), FileMode.OpenOrCreate))
                stream.Write(bytes, 0, bytes.Length);
        }
        public static async System.Threading.Tasks.Task WriteChunkAsync(Guid id, int chunkIndex, byte[] bytes)
        {
            using (var stream = new FileStream(GetFilePath(id, chunkIndex), FileMode.OpenOrCreate))
                await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static void CreateFolder(Guid id)
        {
            Directory.CreateDirectory(GetDirectoryPath(id));
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static void AssertValidChunks(long currentBlobSize, int chunkSize, long offset, int size)
        {
            if (offset % chunkSize > 0)
                throw new Exception("Invalid offset");
        }

        private void DeleteFolder(Guid id)
        {
            var myPath = GetDirectoryPath(id);
            if (!Directory.Exists(myPath))
                return;

            var dirinfo = new DirectoryInfo(myPath);
            foreach (FileInfo file in dirinfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in dirinfo.GetDirectories())
                dir.Delete(true);
        }

        /* ---------------------------------------------------------------------------------------------------- */

        // LEGACY !!

        private LocalDiskChunkBlobProviderData GetData(BlobStorageContext context)
        {
            var data = context.BlobProviderData;
            if (data == null)
                throw new InvalidOperationException("BlobProviderData cannot be null.");
            if (!(data is LocalDiskChunkBlobProviderData specData))
                throw new InvalidOperationException("Unknown BlobProviderData type: " + data.GetType().FullName);
            return specData;
        }

        internal static string GetChunkId(int fileId, int chunkIndex)
        {
            return $"{fileId}_{chunkIndex}";
        }

    }
}
