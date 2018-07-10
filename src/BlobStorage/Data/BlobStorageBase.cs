using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Encapsulates all binary-related storage operations in the Content Repository.
    /// </summary>
    public abstract class BlobStorageBase
    {
        /// <summary>
        /// Inserts a new binary record into the metadata database containing a new or an already exising file id,
        /// removing the previous record if the content is not new.
        /// </summary>
        /// <param name="value">Binary data to insert.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        /// <param name="isNewNode">Whether this value belongs to a new or an existing node.</param>
        protected internal static void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            var blobProvider = GetProvider(value.Size);
            if (value.FileId > 0 && value.Stream == null)
                BlobStorageComponents.DataProvider.InsertBinaryPropertyWithFileId(value, versionId, propertyTypeId, isNewNode);
            else
                BlobStorageComponents.DataProvider.InsertBinaryProperty(blobProvider, value, versionId, propertyTypeId, isNewNode);
        }
        /// <summary>
        /// Updates an existing binary property value in the database and the blob storage.
        /// </summary>
        /// <param name="value">Binary data to update.</param>
        protected internal static void UpdateBinaryProperty(BinaryDataValue value)
        {
            var blobProvider = GetProvider(value.Size);
            BlobStorageComponents.DataProvider.UpdateBinaryProperty(blobProvider, value);
        }
        /// <summary>
        /// Deletes a binary property value from the metadata database, making the corresponding blob storage entry orphaned.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        protected internal static void DeleteBinaryProperty(int versionId, int propertyTypeId)
        {
            BlobStorageComponents.DataProvider.DeleteBinaryProperty(versionId, propertyTypeId);
        }

        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public static BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            return BlobStorageComponents.DataProvider.GetBlobStorageContext(fileId, clearStream, versionId, propertyTypeId);
        }
        /// <summary>
        /// Returns a context object that holds provider-specific data for blob storage operations.
        /// </summary>
        /// <param name="fileId">File identifier.</param>
        /// <param name="clearStream">Whether the blob provider should clear the stream during assembling the context.</param>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        protected internal static Task<BlobStorageContext> GetBlobStorageContextAsync(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            return BlobStorageComponents.DataProvider.GetBlobStorageContextAsync(fileId, clearStream, versionId, propertyTypeId);
        }

        /// <summary>
        /// Loads a cache item into memory that either contains the raw binary (if its size fits into the limit) or
        /// just the blob metadata pointing to the blob storage.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        protected internal static BinaryCacheEntity LoadBinaryCacheEntity(int versionId, int propertyTypeId)
        {
            return BlobStorageComponents.DataProvider.LoadBinaryCacheEntity(versionId, propertyTypeId);
        }
        /// <summary>
        /// Loads a segment from the binary data beginning at the specified position.
        /// </summary>
        /// <param name="fileId">File record identifier.</param>
        /// <param name="position">Starting position of the segment.</param>
        /// <param name="count">Number of bytes to load.</param>
        /// <returns>Byte array containing the requested number of bytes (or less if there is not enough in the binary data).</returns>
        protected internal static byte[] LoadBinaryFragment(int fileId, long position, int count)
        {
            var ctx = GetBlobStorageContext(fileId);
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
        /// <returns>A token containing all the information (db record ids) that identify a single entry in the blob storage.</returns>
        protected internal static string StartChunk(int versionId, int propertyTypeId, long fullSize)
        {
            var blobProvider = GetProvider(fullSize);
            return BlobStorageComponents.DataProvider.StartChunk(blobProvider, versionId, propertyTypeId, fullSize);
        }

        /// <summary>
        /// Writes a byte array to the blob entry specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="buffer">Byte array to write.</param>
        /// <param name="offset">Starting position.</param>
        /// <param name="fullSize">Full size of the whole stream.</param>
        protected internal static void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            var tokenData = ChunkToken.Parse(token, versionId);

            using (var tran = SnTransaction.Begin())
            {
                try
                {
                    var ctx = GetBlobStorageContext(tokenData.FileId);
                    
                    // must update properties because the Length contains the actual saved size but the featue needs the full size
                    UpdateContextProperties(ctx, versionId, tokenData.PropertyTypeId, fullSize);

                    ctx.Provider.Write(ctx, offset, buffer);

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    throw new DataException("Error during saving binary chunk to stream.", ex);
                }
            }
        }
        /// <summary>
        /// Writes a byte array to the blob entry specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="buffer">Byte array to write.</param>
        /// <param name="offset">Starting position.</param>
        /// <param name="fullSize">Full size of the whole stream.</param>
        protected internal static async Task WriteChunkAsync(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            var tokenData = ChunkToken.Parse(token, versionId);

            using (var tran = SnTransaction.Begin())
            {
                try
                {
                    var ctx = await GetBlobStorageContextAsync(tokenData.FileId);

                    // must update properties because the Length contains the actual saved size but the featue needs the full size
                    UpdateContextProperties(ctx, versionId, tokenData.PropertyTypeId, fullSize);
                    
                    await ctx.Provider.WriteAsync(ctx, offset, buffer);

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    throw new DataException("Error during saving binary chunk to stream.", ex);
                }
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
        /// <param name="source">Binary data containing metadata (e.g. content type).</param>
        protected internal static void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null)
        {
            var tokenData = ChunkToken.Parse(token, versionId);
            BlobStorageComponents.DataProvider.CommitChunk(versionId, propertyTypeId, tokenData.FileId, fullSize, source);
        }

        /// <summary>
        /// Writes an input stream to an entry in the blob storage specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="input">The whole stream to write.</param>
        protected internal static void CopyFromStream(int versionId, string token, Stream input)
        {
            var tokenData = ChunkToken.Parse(token, versionId);

            try
            {
                using (var tran = SnTransaction.Begin())
                {
                    var context = GetBlobStorageContext(tokenData.FileId, true, versionId, tokenData.PropertyTypeId);

                    if (context.Provider == BuiltInProvider)
                    {
                        // Our built-in provider does not have a special stream for the case when
                        // the binary should be saved into a regular SQL varbinary column.
                        CopyFromStreamByChunks(context, input);
                    }
                    else
                    {
                        // This is the recommended way to write a stream to the binary storage.
                        using (var targetStream = context.Provider.GetStreamForWrite(context))
                            input.CopyTo(targetStream);
                    }

                    tran.Commit();
                }
            }
            catch (Exception e)
            {
                throw new DataException("Error during saving binary chunk to stream.", e);
            }
        }
        /// <summary>
        /// Writes an input stream to an entry in the blob storage specified by the provided token.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="token">Blob token provided by a preliminary request.</param>
        /// <param name="input">The whole stream to write.</param>
        protected internal static async Task CopyFromStreamAsync(int versionId, string token, Stream input)
        {
            var tokenData = ChunkToken.Parse(token, versionId);

            try
            {
                using (var tran = SnTransaction.Begin())
                {
                    var context = await GetBlobStorageContextAsync(tokenData.FileId, true, versionId, tokenData.PropertyTypeId);

                    if (context.Provider == BuiltInProvider)
                    {
                        // Our built-in provider does not have a special stream for the case when
                        // the binary should be saved into a regular SQL varbinary column.
                        await CopyFromStreamByChunksAsync(context, input);
                    }
                    else
                    {
                        // This is the recommended way to write a stream to the binary storage.
                        using (var targetStream = context.Provider.GetStreamForWrite(context))
                            await input.CopyToAsync(targetStream);
                    }

                    tran.Commit();
                }
            }
            catch (Exception e)
            {
                throw new DataException("Error during saving binary chunk to stream.", e);
            }
        }
        private static void CopyFromStreamByChunks(BlobStorageContext context, Stream input)
        {
            // This method should be used only when the client has a stream and
            // the target will be a regular SQL varbinary column, because we do
            // not have a special write stream for that case. In every other case
            // the blobprovider API should be used that exposes a writable stream.

            var buffer = new byte[BlobStorage.BinaryChunkSize];
            int read;
            long offset = 0;

            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                context.Provider.Write(context, offset, GetLocalBufferAfterRead(read, buffer));

                offset += read;
            }
        }
        private static async Task CopyFromStreamByChunksAsync(BlobStorageContext context, Stream input)
        {
            // This method should be used only when the client has a stream and
            // the target will be a regular SQL varbinary column, because we do
            // not have a special write stream for that case. In every other case
            // the blobprovider API should be used that exposes a writable stream.

            var buffer = new byte[BlobStorage.BinaryChunkSize];
            int read;
            long offset = 0;

            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await context.Provider.WriteAsync(context, offset, GetLocalBufferAfterRead(read, buffer));

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

        /// <summary>
        /// Marks orphaned file records (the ones that do not have a referencing binary record anymore) as Deleted.
        /// </summary>
        protected internal static void CleanupFilesSetFlag()
        {
            BlobStorageComponents.DataProvider.CleanupFilesSetDeleteFlag();
        }
        /// <summary>
        /// Deletes file records that are marked as deleted from the metadata database and also from the blob storage.
        /// </summary>
        /// <returns>Whether there was at least one row that was deleted.</returns>
        protected internal static bool CleanupFiles()
        {
            return BlobStorageComponents.DataProvider.CleanupFiles();
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

        static BlobStorageBase()
        {
            Providers = TypeResolver.GetTypesByInterface(typeof(IBlobProvider))
                .Select(t =>
                {
                    SnTrace.System.Write("BlobProvider found: {0}", t.FullName);
                    return (IBlobProvider) Activator.CreateInstance(t);
                })
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
