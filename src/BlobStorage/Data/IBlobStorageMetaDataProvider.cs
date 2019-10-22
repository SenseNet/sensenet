using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines the API for handling blob-related operations in the main metadata database.
    /// You will have to implement this when you create a new data provider for the whole
    /// Content Repository. If you only want to store binaries in an external database, 
    /// please implement the IBlobProvider interface.
    /// </summary>
    public interface IBlobStorageMetaDataProvider
    {
        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a new binary property value into the metadata database and the blob storage, 
        /// removing the previous one if the content is not new.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="value">Binary data to insert.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="isNewNode">Whether this value belongs to a new or an existing node.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task InsertBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, SnDataContext dataContext);

        /// <summary>
        /// Inserts a new binary record into the metadata database containing an already existing file id,
        /// removing the previous record if the content is not new.
        /// </summary>
        /// <param name="value">Binary data to insert.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="isNewNode">Whether this value belongs to a new or an existing node.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task InsertBinaryPropertyWithFileIdAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, SnDataContext dataContext);

        /// <summary>
        /// Updates an existing binary property value in the database and the blob storage.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="value">Binary data to update.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task UpdateBinaryPropertyAsync(IBlobProvider blobProvider, BinaryDataValue value, SnDataContext dataContext);

        /// <summary>
        /// Deletes a binary property value from the metadata database, making the corresponding blob storage entry orphaned.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext);

        /// <summary>
        /// Deletes all binary properties of the requested versions.
        /// </summary>
        /// <param name="versionIds">VersionId set.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteBinaryPropertiesAsync(IEnumerable<int> versionIds, SnDataContext dataContext);

        /// <summary>
        /// Loads binary property object without the stream by the given parameters.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation 
        /// containing the loaded <see cref="BinaryDataValue"/> instance or null.</returns>
        Task<BinaryDataValue> LoadBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext);

        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation 
        /// containing the loaded <see cref="BinaryCacheEntity"/> instance or null.</returns>
        Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken);
        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation 
        /// containing the loaded <see cref="BinaryCacheEntity"/> instance or null.</returns>
        Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, SnDataContext dataContext);

        /// <summary>
        /// Starts a chunked save operation on an existing content. It does not write any binary data 
        /// to the storage, it only makes prerequisite operations - e.g. allocates a new slot in the storage.
        /// </summary>
        /// <param name="blobProvider">Blob storage provider.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing a token with
        ///  all the information (db record ids) that identify a single entry in the blob storage.</returns>
        Task<string> StartChunkAsync(IBlobProvider blobProvider, int versionId, int propertyTypeId, long fullSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Finalizes a chunked save operation.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="fileId">File identifier.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="source">Binary data containing metadata (e.g. content type).</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CommitChunkAsync(int versionId, int propertyTypeId, int fileId, long fullSize, BinaryDataValue source,
            CancellationToken cancellationToken);

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CleanupFilesSetDeleteFlagAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes file records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing a boolean value 
        /// that is true if there was at least one row that was deleted.</returns>
        Task<bool> CleanupFilesAsync(CancellationToken cancellationToken);
    }
}
