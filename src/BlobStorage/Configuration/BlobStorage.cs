// ReSharper disable once CheckNamespace

using System;

namespace SenseNet.Configuration
{
    /// <summary>
    /// Determines the blob deletion algorithm.
    /// </summary>
    public enum BlobDeletionPolicy
    {
        /// <summary>
        /// The SnMaintenance service checks orphaned blobs periodically and after a delay deletes them.
        /// </summary>
        /// <remarks>
        /// This policy ensures a fast response time but the database may contain a lot of unnecessary data.
        /// This is an enterprise feature.
        /// </remarks>
        BackgroundDelayed,
        /// <summary>
        /// Deleting blobs is requested immediately, and a dedicated background task deletes them one by one.
        /// </summary>
        /// <remarks>
        /// This policy ensures a fast response time but the database load is higher than
        /// in the BackgroundDelayed case.
        /// The database does not reach the desired size immediately, but the delay is as short as possible.
        /// </remarks>
        BackgroundImmediately,
        /// <summary>
        /// Deleting a blob happens immediately and synchronously.
        /// </summary>
        /// <remarks>
        /// This policy results in a slower response, the database load is highest but
        /// the database reaches the desired size immediately.
        /// </remarks>
        Immediately,
    }

    [Obsolete("Use BlobStorageOptions with DI instead.")]
    /// <summary>
    /// Provides configuration values needed by the blob storage. Looks for values 
    /// in the sensenet/blobstorage section. All properties have default values,
    /// no configuration is mandatory.
    /// </summary>
    public class BlobStorage : SnConfig
    {
        private const string SectionName = "sensenet/blobstorage";

        /// <summary>
        /// Size of chunks (in bytes) that are sent to the server by the upload control. It is also taken into
        /// account when computing the size of inner buffers and caches.
        /// </summary>
        public static int BinaryChunkSize { get; internal set; } = GetInt(SectionName, "BinaryChunkSize", 1048576, 524288, 104857600);

        /// <summary>
        /// Size (in bytes) of the binary buffer used by internal streams when serving files from 
        /// the database or the file system. The purpose of this cache is to serve requests faster 
        /// and to reduce the number of SQL connections. 
        /// </summary>
        public static int BinaryBufferSize { get; internal set; } = GetInt(SectionName, "BinaryBufferSize", 1048576, 524288, 104857600);

        /// <summary>
        /// Maximum file size (in bytes) that should be cached after loading a binary value. Smaller files 
        /// will by placed into the cache, larger files will always be served from the blob storage directly.
        /// </summary>
        public static int BinaryCacheSize { get; internal set; } = GetInt(SectionName, "BinaryCacheSize", 1048576, 102400, 104857600);

        /// <summary>
        /// Minimum size limit (in bytes) for binary data to be stored in the external blob storage. 
        /// Files under this size will be stored in the database. If you set this to 0, all files
        /// will go to the external storage. In case of a huge value everything will remain in the db.
        /// </summary>
        public static int MinimumSizeForBlobProviderInBytes { get; internal set; } = GetInt(SectionName, "MinimumSizeForBlobProviderKB", 500) * 1024;

        /// <summary>
        /// Class name of an optional external blob storage provider.
        /// </summary>
        public static string BlobProviderClassName { get; internal set; } = GetString(SectionName, "BlobProvider");
        /// <summary>
        /// Class name of an optional external metadata provider for the blob storage.
        /// </summary>
        public static string MetadataProviderClassName { get; internal set; } = GetString(SectionName, "MetadataProvider");

        /// <summary>
        /// Gets or sets the blob deletion algorithm.
        /// </summary>
        public static BlobDeletionPolicy BlobDeletionPolicy { get; set; } =
            GetValue<BlobDeletionPolicy>(SectionName, "BlobDeletionPolicy");
    }

    public class BlobStorageOptions
    {
        [Obsolete("Get configuration through dependency injection instead.")]
        public static BlobStorageOptions GetLegacyConfiguration()
        {
            return new BlobStorageOptions
            {
                BinaryBufferSize = BlobStorage.BinaryBufferSize,
                BinaryCacheSize = BlobStorage.BinaryCacheSize,
                BinaryChunkSize = BlobStorage.BinaryChunkSize,
                MinimumSizeForBlobProviderKb = BlobStorage.MinimumSizeForBlobProviderInBytes / 1024,
                BlobDeletionPolicy = BlobStorage.BlobDeletionPolicy
            };
        }

        public int BinaryChunkSize { get; set; } = 1048576;
        public int BinaryBufferSize { get; set; } = 1048576;
        public int BinaryCacheSize { get; set; } = 1048576;

        private int? _minimumSizeForBlobProviderInBytes;
        public int MinimumSizeForBlobProviderInBytes =>
            _minimumSizeForBlobProviderInBytes ??
            (_minimumSizeForBlobProviderInBytes = MinimumSizeForBlobProviderKb * 1024).Value;

        public int MinimumSizeForBlobProviderKb { get; set; } = 500;

        public BlobDeletionPolicy BlobDeletionPolicy { get; set; }
    }
}
