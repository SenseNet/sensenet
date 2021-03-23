using System.IO;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Entry point for accessing the blob storage directly. Most of the methods here require a preliminary 
    /// request to the portal to gain access to a token that identifies the blob you want to work with.
    /// </summary>
    public class BlobStorageClient : BlobStorage
    {
        public BlobStorageClient(IBlobProviderStore providers,
            IBlobProviderSelector selector,
            IBlobStorageMetaDataProvider metaProvider) : base(providers, selector, metaProvider) {}

        /// <summary>
        /// Gets a readonly stream that contains a blob entry in the blob storage.
        /// </summary>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing
        /// a readonly stream that comes from the blob storage directly.</returns>
        public async Task<Stream> GetStreamForReadAsync(string token, CancellationToken cancellationToken)
        {
            var tokenData = ChunkToken.Parse(token);
            var context = await GetBlobStorageContextAsync(tokenData.FileId, false, tokenData.VersionId, tokenData.PropertyTypeId, cancellationToken).ConfigureAwait(false);

            return context.Provider.GetStreamForRead(context);
        }
    }
}
