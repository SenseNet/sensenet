using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines the central API for performing blob-related operations.
    /// </summary>
    public interface IBlobStorage
    {
        void Initialize();

        Task InsertBinaryPropertyAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode,
            SnDataContext dataContext);
        Task UpdateBinaryPropertyAsync(BinaryDataValue value, SnDataContext dataContext);
        Task DeleteBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext);
        Task DeleteBinaryPropertiesAsync(IEnumerable<int> versionIds, SnDataContext dataContext);

        Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId,
            CancellationToken cancellationToken);
        Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream,
            CancellationToken cancellationToken);
        Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId,
            CancellationToken cancellationToken);
        Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId,
            int propertyTypeId, CancellationToken cancellationToken);

        Task<BinaryDataValue> LoadBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext);
        Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken);
        Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId,
            SnDataContext dataContext);

        Task<byte[]> LoadBinaryFragmentAsync(int fileId, long position, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Starts a chunked save operation on an existing content. It does not write any binary data 
        /// to the storage, it only makes prerequisite operations - e.g. allocates a new slot in the storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing a token with
        /// all the information (db record ids) that identify a single entry in the blob storage.</returns>
        Task<string> StartChunkAsync(int versionId, int propertyTypeId, long fullSize,
            CancellationToken cancellationToken);
        /// <summary>
        /// Writes a byte array to the blob entry specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="buffer">Byte array to write.</param>
        /// <param name="offset">Starting position.</param>
        /// <param name="fullSize">Full size of the whole stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task WriteChunkAsync(int versionId, string token, byte[] buffer, long offset, long fullSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Finalizes a chunked save operation.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CommitChunkAsync(int versionId, int propertyTypeId, string token, long fullSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Finalizes a chunked save operation.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="source">Binary data containing metadata (e.g. content type).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CommitChunkAsync(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source,
            CancellationToken cancellationToken);

        /// <summary>
        /// Writes an input stream to an entry in the blob storage specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="input">The whole stream to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task CopyFromStreamAsync(int versionId, string token, Stream input,
            CancellationToken cancellationToken);

        IBlobProvider GetProvider(long streamSize);
        IBlobProvider GetProvider(string providerName);

        /*================================================================== Maintenance*/

        Task DeleteOrphanedFilesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// Marks only files that were created more than 30 minutes ago.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CleanupFilesSetFlagAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CleanupFilesSetFlagImmediatelyAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes one record that is marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing a boolean value 
        /// that is true if there was at least one row that was deleted.</returns>
        Task<bool> CleanupFilesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes all records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CleanupAllFilesAsync(CancellationToken cancellationToken);
    }
}
