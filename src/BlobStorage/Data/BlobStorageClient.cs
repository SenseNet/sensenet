using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Entry point for accessing the blob storage directly. Most of the methods here require a preliminary 
    /// request to the portal to gain access to a token that identifies the blob you want to work with.
    /// </summary>
    public class BlobStorageClient : BlobStorageBase
    {
        /// <summary>
        /// Writes a byte array to the blob entry specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="buffer">Byte array to write.</param>
        /// <param name="offset">Starting position.</param>
        /// <param name="fullSize">Full size of the whole stream.</param>
        public new static void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            BlobStorageBase.WriteChunk(versionId, token, buffer, offset, fullSize);
        }
        /// <summary>
        /// Writes a byte array to the blob entry specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="buffer">Byte array to write.</param>
        /// <param name="offset">Starting position.</param>
        /// <param name="fullSize">Full size of the whole stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public new static Task WriteChunkAsync(int versionId, string token, byte[] buffer, long offset, long fullSize,
            CancellationToken cancellationToken)
        {
            return BlobStorageBase.WriteChunkAsync(versionId, token, buffer, offset, fullSize, cancellationToken);
        }
        /// <summary>
        /// Writes an input stream to an entry in the blob storage specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="input">The whole stream to write.</param>
        public new static void CopyFromStream(int versionId, string token, Stream input)
        {
            BlobStorageBase.CopyFromStream(versionId, token, input);
        }
        /// <summary>
        /// Writes an input stream to an entry in the blob storage specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="input">The whole stream to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public new static Task CopyFromStreamAsync(int versionId, string token, Stream input,
            CancellationToken cancellationToken)
        {
            return BlobStorageBase.CopyFromStreamAsync(versionId, token, input, cancellationToken);
        }
        /// <summary>
        /// Gets a readonly stream that contains a blob entry in the blob storage.
        /// </summary>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <returns>A readonly stream that comes from the blob storage directly.</returns>
        public static Stream GetStreamForRead(string token)
        {
            var tokenData = ChunkToken.Parse(token);
            var context = GetBlobStorageContext(tokenData.FileId, false, tokenData.VersionId, tokenData.PropertyTypeId);

            return context.Provider.GetStreamForRead(context);
        }
    }
}
