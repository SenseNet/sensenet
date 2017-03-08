using System;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Provides configuration values needed by the blob storage. Obsolete class.
    /// </summary>
    [Obsolete("After V6.5 PATCH 9: Use the Configuration.BlobStorage class instead.")]
    public static class BlobStorageConfiguration
    {
        /// <summary>
        /// Size of chunks (in bytes) that are sent to the server by the upload control. It is also taken into
        /// account when computing the size of inner buffers and caches.
        /// </summary>
        public static int BinaryChunkSize => BlobStorage.BinaryChunkSize;

        /// <summary>
        /// Size (in bytes) of the binary buffer used by internal streams when serving files from 
        /// the database or the file system. The purpose of this cache is to serve requests faster 
        /// and to reduce the number of SQL connections. 
        /// </summary>
        public static int BinaryBufferSize => BlobStorage.BinaryBufferSize;

        /// <summary>
        /// Maximum file size (in bytes) that should be cached after loading a binary value. Smaller files 
        /// will by placed into the cache, larger files will always be served from the blob storage directly.
        /// </summary>
        public static int BinaryCacheSize => BlobStorage.BinaryCacheSize;
        
        /// <summary>
        /// Minimum size limit (in bytes) for binary data to be stored in a SQL FileStream column. 
        /// Files smaller or equal this size will be stored in the database. Bigger files will go
        /// to a FileStream column if the feature is enabled in the database.
        /// If you set this to 0, all files will go to the filestream column. 
        /// In case of a huge value everything will remain in the db.
        /// </summary>
        public static int MinimumSizeForFileStreamInBytes => BlobStorage.MinimumSizeForFileStreamInBytes;

        /// <summary>
        /// Minimum size limit (in bytes) for binary data to be stored in the external blob storage. 
        /// Files under this size will be stored in the database. If you set this to 0, all files
        /// will go to the external storage. In case of a huge value everything will remain in the db.
        /// </summary>
        public static int MinimumSizeForBlobProviderInBytes => BlobStorage.MinimumSizeForBlobProviderInBytes;

        /// <summary>
        /// Whether the FileStream feature is enabled in the system or not.
        /// Computed property, not possible to configure.
        /// </summary>
        public static bool FileStreamEnabled => BlobStorage.FileStreamEnabled;
    }
}
