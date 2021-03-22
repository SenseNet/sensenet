using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    //UNDONE: [DIBLOB] merge BlobStorage and BlobStorageBase classes
    public class BlobStorage : BlobStorageBase
    {
        public BlobStorage(IBlobProviderStore providers, IBlobProviderSelector selector,
            IBlobStorageMetaDataProvider metaProvider) :
            base(providers, selector, metaProvider)
        {
        }

        public Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, CancellationToken cancellationToken, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            return base.GetBlobStorageContextAsync(fileId, clearStream, versionId, propertyTypeId, cancellationToken);
        }
    }
}
