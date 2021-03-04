using System;
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
    public class BlobStorageClient : BlobStorageBase
    {
        public BlobStorageClient() : base(null, null)
        {
            //UNDONE: [DIBLOB] how to get providers?
            // How do we use this class? How will we get the meta provider and the selector here?
            // Should we simply get them through the constructor?
            // Can we use the built-in classes as defaults?
            throw new NotImplementedException("BlobStorageClient is not fully modified to support the new API.");
        }

        /// <summary>
        /// Gets a readonly stream that contains a blob entry in the blob storage.
        /// </summary>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containig
        /// a readonly stream that comes from the blob storage directly.</returns>
        public async Task<Stream> GetStreamForReadAsync(string token, CancellationToken cancellationToken)
        {
            var tokenData = ChunkToken.Parse(token);
            var context = await GetBlobStorageContextAsync(tokenData.FileId, false, tokenData.VersionId, tokenData.PropertyTypeId, cancellationToken).ConfigureAwait(false);

            return context.Provider.GetStreamForRead(context);
        }
    }
}
