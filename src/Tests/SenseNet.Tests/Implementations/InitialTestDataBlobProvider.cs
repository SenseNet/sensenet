using System;
using System.IO;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Tests.Implementations
{
    internal class InitialTestDataBlobProvider : IBlobProvider
    {
        private readonly IRepositoryDataFile _dataFile = new InitialTestData();

        public Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        public Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var path = (string)context.BlobProviderData;
            string fileContent = null;
            if (path.StartsWith(Repository.ContentTypesFolderPath))
            {
                var ctdName = RepositoryPath.GetFileName(path);
                fileContent = _dataFile.ContentTypeDefinitions[ctdName];
            }
            else
            {
                var key = $"{ActiveSchema.PropertyTypes.GetItemById(context.PropertyTypeId).Name}:{path}";
                _dataFile.Blobs.TryGetValue(key, out fileContent);
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
