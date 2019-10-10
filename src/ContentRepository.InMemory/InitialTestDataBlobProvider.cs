using System;
using System.IO;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    internal class InitialTestDataBlobProvider : IBlobProvider
    {
        private readonly IRepositoryDataFile _dataFile = InitialTestData.Instance;

        public STT.Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public STT.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        public STT.Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
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
