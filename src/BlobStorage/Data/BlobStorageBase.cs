using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Encapsulates all binary-related storage operations in the Content Repository.
    /// </summary>
    public abstract class BlobStorageBase
    {
        /// <summary>
        /// Inserts a new binary record into the metadata database containing a new or an already existing file id,
        /// removing the previous record if the content is not new.
        /// </summary>
        /// <param name="value">Binary data to insert.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="isNewNode">Whether this value belongs to a new or an existing node.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static Task InsertBinaryPropertyAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, SnDataContext dataContext)
        {
            var blobProvider = GetProvider(value.Size);
            if (value.FileId > 0 && value.Stream == null)
                return BlobStorageComponents.DataProvider.InsertBinaryPropertyWithFileIdAsync(value, versionId, propertyTypeId, isNewNode, dataContext);
            else
                return BlobStorageComponents.DataProvider.InsertBinaryPropertyAsync(blobProvider, value, versionId, propertyTypeId, isNewNode, dataContext);
        }

        /// <summary>
        /// Updates an existing binary property value in the database and the blob storage.
        /// </summary>
        /// <param name="value">Binary data to update.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static async Task UpdateBinaryPropertyAsync(BinaryDataValue value, SnDataContext dataContext)
        {
            var blobProvider = GetProvider(value.Size);
            await BlobStorageComponents.DataProvider.UpdateBinaryPropertyAsync(blobProvider, value, dataContext);
            dataContext.NeedToCleanupFiles = true;
        }

        /// <summary>
        /// Deletes a binary property value from the metadata database, making the corresponding blob storage entry orphaned.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static async Task DeleteBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            await BlobStorageComponents.DataProvider.DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext);
            dataContext.NeedToCleanupFiles = true;
        }

        /// <summary>
        /// Deletes all binary properties of the requested versions.
        /// </summary>
        /// <param name="versionIds">VersionId set.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static async Task DeleteBinaryPropertiesAsync(IEnumerable<int> versionIds, SnDataContext dataContext)
        {
            await BlobStorageComponents.DataProvider.DeleteBinaryPropertiesAsync(versionIds, dataContext);
            dataContext.NeedToCleanupFiles = true;
        }

        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        protected internal static Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId,
            CancellationToken cancellationToken)
        {
            return GetBlobStorageContextAsync(fileId, false, cancellationToken);
        }
        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        protected internal static Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream,
            CancellationToken cancellationToken)
        {
            return GetBlobStorageContextAsync(fileId, clearStream, 0, cancellationToken);
        }
        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        protected internal static Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId,
            CancellationToken cancellationToken)
        {
            return GetBlobStorageContextAsync(fileId, clearStream, versionId, 0, cancellationToken);
        }
        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        protected internal static Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream, int versionId, int propertyTypeId,
            CancellationToken cancellationToken)
        {
            return BlobStorageComponents.DataProvider.GetBlobStorageContextAsync(fileId, clearStream, versionId, propertyTypeId, cancellationToken);
        }

        /// <summary>
        /// Loads binary property object without the stream by the given parameters.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation 
        /// containing the loaded <see cref="BinaryDataValue"/> instance or null.</returns>
        protected static Task<BinaryDataValue> LoadBinaryPropertyAsync(int versionId, int propertyTypeId, SnDataContext dataContext)
        {
            return BlobStorageComponents.DataProvider.LoadBinaryPropertyAsync(versionId, propertyTypeId, dataContext);
        }

        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation 
        /// containig the loaded <see cref="BinaryCacheEntity"/> instance or null.</returns>
        protected internal static Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken)
        {
            return BlobStorageComponents.DataProvider.LoadBinaryCacheEntityAsync(versionId, propertyTypeId, cancellationToken);
        }
        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="dataContext">Database accessor object.</param>
        /// <returns>A Task that represents the asynchronous operation 
        /// containig the loaded <see cref="BinaryCacheEntity"/> instance or null.</returns>
        protected internal static Task<BinaryCacheEntity> LoadBinaryCacheEntityAsync(int versionId, int propertyTypeId,
            SnDataContext dataContext)
        {
            return BlobStorageComponents.DataProvider.LoadBinaryCacheEntityAsync(versionId, propertyTypeId, dataContext);
        }

        /// <summary>
        /// Loads a segment from the binary data beginning at the specified position.
        /// </summary>
        /// <param name="fileId">File record identifier.</param>
        /// <param name="position">Starting position of the segment.</param>
        /// <param name="count">Number of bytes to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing
        /// a byte array containing the requested number of bytes (or less if there is not enough in the binary data).</returns>
        protected internal static async Task<byte[]> LoadBinaryFragmentAsync(int fileId, long position, int count,
            CancellationToken cancellationToken)
        {
            var ctx = await GetBlobStorageContextAsync(fileId, cancellationToken).ConfigureAwait(false);
            var provider = ctx.Provider;

            if (provider == BuiltInProvider)
                return BuiltInBlobProvider.ReadRandom(ctx, position, count);

            var realCount = Convert.ToInt32(Math.Min(ctx.Length - position, count));
            var bytes = new byte[realCount];
            using (var stream = provider.GetStreamForRead(ctx))
            {
                stream.Seek(position, SeekOrigin.Begin);
                stream.Read(bytes, 0, realCount);
            }
            return bytes;
        }

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
        protected internal static Task<string> StartChunkAsync(int versionId, int propertyTypeId, long fullSize,
            CancellationToken cancellationToken)
        {
            var blobProvider = GetProvider(fullSize);
            return BlobStorageComponents.DataProvider.StartChunkAsync(blobProvider, versionId, propertyTypeId, fullSize,
                cancellationToken);
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
        protected internal static async Task WriteChunkAsync(int versionId, string token, byte[] buffer, long offset, long fullSize,
            CancellationToken cancellationToken)
        {
            var tokenData = ChunkToken.Parse(token, versionId);
            try
            {
                var ctx = await GetBlobStorageContextAsync(tokenData.FileId, cancellationToken).ConfigureAwait(false);

                // must update properties because the Length contains the actual saved size but the feature needs the full size
                UpdateContextProperties(ctx, versionId, tokenData.PropertyTypeId, fullSize);
                    
                await ctx.Provider.WriteAsync(ctx, offset, buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new DataException("Error during saving binary chunk to stream.", ex);
            }
        }
        private static void UpdateContextProperties(BlobStorageContext context, int versionId, int propertyTypeId, long fullSize)
        {
            context.Length = fullSize;
            context.VersionId = versionId;
            context.PropertyTypeId = propertyTypeId;
        }

        /// <summary>
        /// Finalizes a chunked save operation.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="fullSize">Full size (stream length) of the binary value.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static Task CommitChunkAsync(int versionId, int propertyTypeId, string token, long fullSize,
            CancellationToken cancellationToken)
        {
            return CommitChunkAsync(versionId, propertyTypeId, token, fullSize, null, cancellationToken);
        }
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
        protected internal static async Task CommitChunkAsync(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source,
            CancellationToken cancellationToken)
        {
            var tokenData = ChunkToken.Parse(token, versionId);
            await BlobStorageComponents.DataProvider.CommitChunkAsync(versionId, propertyTypeId, tokenData.FileId,
                fullSize, source, cancellationToken);
            await DeleteOrphanedFilesAsync(cancellationToken);
        }

        /// <summary>
        /// Writes an input stream to an entry in the blob storage specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="input">The whole stream to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        protected internal static async Task CopyFromStreamAsync(int versionId, string token, Stream input,
            CancellationToken cancellationToken)
        {
            var tokenData = ChunkToken.Parse(token, versionId);
            try
            {
                var context = await GetBlobStorageContextAsync(tokenData.FileId, true, versionId, tokenData.PropertyTypeId, cancellationToken).ConfigureAwait(false);
                if (context.Provider == BuiltInProvider)
                {
                    // Our built-in provider does not have a special stream for the case when
                    // the binary should be saved into a regular SQL varbinary column.
                    await CopyFromStreamByChunksAsync(context, input, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // This is the recommended way to write a stream to the binary storage.
                    using (var targetStream = context.Provider.GetStreamForWrite(context))
                        await input.CopyToAsync(targetStream).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                throw new DataException("Error during saving binary chunk to stream.", e);
            }
        }
        private static async Task CopyFromStreamByChunksAsync(BlobStorageContext context, Stream input,
            CancellationToken cancellationToken)
        {
            // This method should be used only when the client has a stream and
            // the target will be a regular SQL varbinary column, because we do
            // not have a special write stream for that case. In every other case
            // the blobprovider API should be used that exposes a writable stream.

            var buffer = new byte[BlobStorage.BinaryChunkSize];
            int read;
            long offset = 0;

            while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await context.Provider.WriteAsync(context, offset, GetLocalBufferAfterRead(read, buffer), cancellationToken).ConfigureAwait(false);

                offset += read;
            }
        }

        private static byte[] GetLocalBufferAfterRead(int read, byte[] buffer)
        {
            // In case of the last chunk it is possible that the buffer is
            // bigger than the number of bytes that were read from the db. 
            // In that case we have to create a new byte array that is 
            // of the appropriate length to avoid saving a stream that is 
            // larger than the file.
            return read < buffer.Length ? buffer.Take(read).ToArray() : buffer;
        }

        /*================================================================== Maintenance*/

        protected static async Task DeleteOrphanedFilesAsync(CancellationToken cancellationToken)
        {
            switch (BlobStorage.BlobDeletionPolicy)
            {
                case BlobDeletionPolicy.BackgroundDelayed:
                    // Do nothing, the blob deletion is a maintenance task.
                    break;
                case BlobDeletionPolicy.Immediately:
                    await CleanupFilesSetFlagImmediatelyAsync(cancellationToken);
                    await CleanupAllFilesAsync(cancellationToken);
                    break;
                case BlobDeletionPolicy.BackgroundImmediately:
                    await CleanupFilesSetFlagImmediatelyAsync(cancellationToken);
#pragma warning disable 4014
                    // This call is not awaited because of shorter response time.
                    CleanupAllFilesAsync(cancellationToken);
#pragma warning restore 4014
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// Marks only files that were created more than 30 minutes ago.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static Task CleanupFilesSetFlagAsync(CancellationToken cancellationToken)
        {
            return BlobStorageComponents.DataProvider.CleanupFilesSetDeleteFlagAsync(cancellationToken);
        }
        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static Task CleanupFilesSetFlagImmediatelyAsync(CancellationToken cancellationToken)
        {
            return BlobStorageComponents.DataProvider.CleanupFilesSetDeleteFlagImmediatelyAsync(cancellationToken);
        }

        /// <summary>
        /// Deletes one records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation containing a boolean value 
        /// that is true if there was at least one row that was deleted.</returns>
        protected internal static Task<bool> CleanupFilesAsync(CancellationToken cancellationToken)
        {
            return BlobStorageComponents.DataProvider.CleanupFilesAsync(cancellationToken);
        }
        /// <summary>
        /// Deletes all records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        protected internal static Task CleanupAllFilesAsync(CancellationToken cancellationToken)
        {
            return BlobStorageComponents.DataProvider.CleanupAllFilesAsync(cancellationToken);
        }

        /*==================================================================== Provider */

        /// <summary>
        /// Gets an instance of the built-in provider.
        /// </summary>
        public static IBlobProvider BuiltInProvider { get; }
        /// <summary>
        /// Gets a list of available blob storage providers in the system.
        /// </summary>
        protected internal static Dictionary<string, IBlobProvider> Providers { get; set; }

        private class BlobProviderComparer : IEqualityComparer<IBlobProvider>
        {
            public bool Equals(IBlobProvider x, IBlobProvider y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;
                var tx = x.GetType();
                var ty = y.GetType();
                if (tx.FullName != ty.FullName)
                    return false;
                var ax = tx.Assembly;
                var ay = ty.Assembly;
                if (ax.FullName != ay.FullName)
                    return false;
                return ax.GetName().Version == ay.GetName().Version;
            }

            public int GetHashCode(IBlobProvider obj)
            {
                var n = obj.GetType().Assembly.GetName();
                return n.FullName.GetHashCode() ^ n.Version.GetHashCode();
            }
        }
        static BlobStorageBase()
        {
            Providers = TypeResolver.GetTypesByInterface(typeof(IBlobProvider))
                .Select(t =>
                {
                    try
                    {
                        var instance = (IBlobProvider)Activator.CreateInstance(t);
                        SnTrace.System.Write("BlobProvider found: {0} ({1}) from: {2}", t.FullName, t.Assembly.GetName().Version, t.Assembly.CodeBase);

                        return instance;
                    }
                    catch (MissingMethodException)
                    {
                        // no default constructor: provider must be instantiated manually
                        SnTrace.System.Write("BlobProvider found, but must be configured manually: {0}", t.FullName);
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, $"Error during instantiating {t.FullName}.");
                    }

                    return null;
                })
                .Where(instance => instance != null)
                .Distinct(new BlobProviderComparer())
                .ToDictionary(x => x.GetType().FullName, x => x);

            BuiltInProvider = new BuiltInBlobProvider();
        }

        /// <summary>
        /// Gets a provider based on the binary size and the available blob providers in the system.
        /// </summary>
        /// <param name="fullSize">Full binary length.</param>
        public static IBlobProvider GetProvider(long fullSize)
        {
            return BlobStorageComponents.ProviderSelector.GetProvider(fullSize, Providers, BuiltInProvider);
        }
        /// <summary>
        /// Gets the blob provider instance with the specified name. Default is the built-in provider.
        /// </summary>
        public static IBlobProvider GetProvider(string providerName)
        {
            if (providerName == null)
                return BuiltInProvider;
            IBlobProvider provider;
            if (Providers.TryGetValue(providerName, out provider))
                return provider;
            throw new InvalidOperationException("BlobProvider not found: '" + providerName + "'.");
        }
    }
}
