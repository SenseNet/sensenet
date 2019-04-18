using System;
using System.IO;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Tests.Implementations
{
    internal class ContentTypeStringBlobProvider : IBlobProvider
    {
        public void Allocate(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            throw new NotSupportedException();
        }
        public Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            throw new NotSupportedException();
        }
        public void Delete(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var ctdPath = (string)context.BlobProviderData;
            var ctdName = RepositoryPath.GetFileName(ctdPath);
            var ctd = InitialTestData.ContentTypeDefinitions[ctdName];
            var stream = RepositoryTools.GetStreamFromString(ctd);
            return stream;
        }
        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            throw new NotSupportedException();
        }
        public object ParseData(string providerData)
        {
            return providerData;
        }
    }
}
