using System;
using System.IO;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    internal class FileSystemReaderBlobProvider : IBlobProvider
    {
        public void Allocate(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
        public void Write(BlobStorageContext context, long offset, byte[] buffer)
        {
            throw new NotSupportedException();
        }
        public System.Threading.Tasks.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer)
        {
            throw new NotSupportedException();
        }
        public void Delete(BlobStorageContext context)
        {
            throw new NotSupportedException();
        }
        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var path = (string) context.BlobProviderData;

            // Transform CTD paths
            var ctdRoot = Repository.ContentTypesFolderPath + "/";
            if (path.StartsWith(ctdRoot))
                path = ctdRoot + RepositoryPath.GetFileName(path);

            //UNDONE:DB!!!!!!!!!!! DO NOT USE ABSOUTE PATH OF YOUR MACHINE
            var fsPath = Path.Combine(@"D:\dev\github\sensenet\src\nuget\snadmin\install-services\import", path.Substring("/root/".Length).Replace("/", "\\"));
            return new FileStream(fsPath, FileMode.Open, FileAccess.Read);
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
