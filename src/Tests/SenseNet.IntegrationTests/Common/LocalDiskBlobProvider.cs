using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.IntegrationTests.Common
{
    internal class LocalDiskBlobProvider : IBlobProvider
    {
        internal class LocalDiskBlobProviderData
        {
            public Guid Id { get; set; }
        }

        private readonly string _rootDirectory;

        public LocalDiskBlobProvider()
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
            return BlobStorageContext.DeserializeBlobProviderData<LocalDiskBlobProviderData>(providerData);
        }

        public async Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            await CreateFileAsync(id, null, cancellationToken);
            context.BlobProviderData = new LocalDiskBlobProviderData { Id = id };
        }

        public Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = GetData(context).Id;
            DeleteFile(id);
            return Task.CompletedTask;
        }

        public async Task ClearAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var id = GetData(context).Id;
            DeleteFile(id);
            await CreateFileAsync(id, null, cancellationToken);
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            return GetStream(context, FileMode.Open);
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            return GetStream(context, FileMode.OpenOrCreate);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            if (!(stream is FileStream))
                throw new InvalidOperationException("Stream must be a FileStream in the local disk provider.");

            return GetStream(context, FileMode.Open);
        }

        public async Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = GetAndExtendStream(context, offset, buffer.Length))
            {
                await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            }
        }

        private Stream GetAndExtendStream(BlobStorageContext context, long offset, int bufferLength)
        {
            var stream = GetStream(context, FileMode.Open);
            var missingGapSize = offset + bufferLength - stream.Length;
            if (missingGapSize > 0)
            {
                stream.Seek(0, SeekOrigin.End);
                for (var i = 0; i < missingGapSize; i++)
                    stream.WriteByte(0x0);
            }
            stream.Seek(offset, SeekOrigin.Begin);

            return stream;
        }


        /*========================================================================== tools */

        private string GetPath(Guid id)
        {
            return Path.Combine(_rootDirectory, id.ToString());
        }
        private LocalDiskBlobProviderData GetData(BlobStorageContext context)
        {
            var data = context.BlobProviderData;
            if (data == null)
                throw new InvalidOperationException("BlobProviderData cannot be null.");
            if (!(data is LocalDiskBlobProviderData specData))
                throw new InvalidOperationException("Unknown BlobProviderData type: " + data.GetType().FullName);
            return specData;
        }

        private async Task CreateFileAsync(Guid id, Stream stream, CancellationToken cancellationToken)
        {
            using (var fileStream = new FileStream(GetPath(id), FileMode.CreateNew))
            {
                if (stream != null)
                {
                    var buffer = new byte[stream.Length];
                    await stream.ReadAsync(buffer, 0, Convert.ToInt32(stream.Length), cancellationToken);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        private void DeleteFile(Guid id)
        {
            var filePath = GetPath(id);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private Stream GetStream(BlobStorageContext context, FileMode mode)
        {
            return new FileStream(GetPath(GetData(context).Id), mode);
        }

    }
}
