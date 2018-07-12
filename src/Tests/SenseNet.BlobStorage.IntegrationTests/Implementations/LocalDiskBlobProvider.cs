using System;
using System.Text;
using System.IO;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.BlobStorage.IntegrationTests.Implementations
{
    internal class LocalDiskBlobProvider : IBlobProvider
    {
        private static StringBuilder _trace = new StringBuilder();
        public static string Trace { get { return _trace.ToString(); } }

        internal class LocalDiskBlobProviderData
        {
            public Guid Id { get; set; }
            public string Trace { get; set; }
        }

        private string _rootDirectory;

        public LocalDiskBlobProvider()
        {
            _rootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\" + GetType().Name);
            if (Directory.Exists(_rootDirectory))
            {
                foreach (var path in Directory.GetFiles(_rootDirectory))
                    System.IO.File.Delete(path);
            }
            else
            {
                Directory.CreateDirectory(_rootDirectory);
            }
        }

        public static void _ClearTrace()
        {
            _trace.Clear();
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
            //SnTrace.Test.Write("LocalDiskBlobProvider.Allocate: " + id);
        }

        public void Delete(BlobStorageContext context)
        {
            var id = GetData(context).Id;
            DeleteFile(id);
            //SnTrace.Test.Write("LocalDiskBlobProvider.Delete: " + id);
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
                _trace.Append($"write from:{offset}, count:{buffer.Length};");
            }
        }
        public async System.Threading.Tasks.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            using (var stream = GetAndExtendStream(context, offset, buffer.Length))
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
                _trace.Append($"write from:{offset}, count:{buffer.Length};");
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
            var specData = data as LocalDiskBlobProviderData;
            if (specData == null)
                throw new InvalidOperationException("Unknown BlobProviderData type: " + data.GetType().FullName);
            return specData;
        }

        private void CreateFile(Guid id, Stream stream)
        {
            using (var fileStream = new System.IO.FileStream(GetPath(id), FileMode.CreateNew))
            {
                if (stream != null)
                {
                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            //SnTrace.Test.Write("LocalDiskBlobProvider.CreateFile: " + id);
        }
        private void DeleteFile(Guid id)
        {
            var filePath = GetPath(id);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            //SnTrace.Test.Write("LocalDiskBlobProvider.DeleteFile: " + id);
        }
        private Stream GetStream(BlobStorageContext context, FileMode mode)
        {
            //SnTrace.Test.Write("LocalDiskBlobProvider.GetStream: {0}, {1}", GetData(context).Id, mode);
            return new FileStream(GetPath(GetData(context).Id), mode);
        }

    }
}
