using System;
using System.IO;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Tests.Implementations
{
    internal class InitialTestDataBlobProvider : IBlobProvider
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
            var path = (string)context.BlobProviderData;
            string fileContent = null;
            if (path.StartsWith(Repository.ContentTypesFolderPath))
            {
                var ctdName = RepositoryPath.GetFileName(path);
                fileContent = InitialTestData.ContentTypeDefinitions[ctdName];
            }
            else
            {
                var key = $"{ActiveSchema.PropertyTypes.GetItemById(context.PropertyTypeId).Name}:{path}";
                InitialTestData.GeneralBlobs.TryGetValue(key, out fileContent);
            }
            var stream = RepositoryTools.GetStreamFromString(fileContent);
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
