using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class BlobStorage : BlobStorageBase
    {
        public BlobStorage(IBlobStorageMetaDataProvider metaProvider) :
            base(metaProvider)
        {
        }

        public Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, CancellationToken cancellationToken, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            return base.GetBlobStorageContextAsync(fileId, clearStream, versionId, propertyTypeId, cancellationToken);
        }
    }
}
