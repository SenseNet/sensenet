using System;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.BlobStorage.IntegrationTests.Implementations
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

        public void Allocate(BlobStorageContext context)
        {
            var id = Guid.NewGuid();
            CreateFile(id, null);
            context.BlobProviderData = new LocalDiskBlobProviderData { Id = id };
        }

        public void Delete(BlobStorageContext context)
        {
            var id = GetData(context).Id;
            DeleteFile(id);
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

        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var stream = GetAndExtendStream(context, offset, buffer.Length))
            {
                stream.Write(buffer, 0, buffer.Length);
            }
        }
        public async System.Threading.Tasks.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var stream = GetAndExtendStream(context, offset, buffer.Length))
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
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

        private void CreateFile(Guid id, Stream stream)
        {
            using (var fileStream = new FileStream(GetPath(id), FileMode.CreateNew))
            {
                if (stream != null)
                {
                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
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
