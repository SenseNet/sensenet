using System;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Holds provider-specific context information for binary operations.
    /// </summary>
    public class BlobStorageContext
    {
        /// <summary>
        /// Content version id. Warning: this value is valid only when calling the following BlobProvider method: 
        /// Write(BlobStorageContext context, long offset, byte[] buffer)
        /// </summary>
        public int VersionId { get; set; }
        /// <summary>
        /// Binary property type id. Warning: this value is valid only when calling the following BlobProvider method: 
        /// Write(BlobStorageContext context, long offset, byte[] buffer)
        /// </summary>
        public int PropertyTypeId { get; set; }
        /// <summary>
        /// File identifier.
        /// </summary>
        public int FileId { get; set; }
        /// <summary>
        /// Binary data full length.
        /// </summary>
        public long Length { get; set; }
        /// <summary>
        /// Gets the blob provider instance responsible for reading and writing bytes of the blob described by this context object.
        /// </summary>
        public IBlobProvider Provider { get; set; }

        /// <summary>
        /// A blob provider-specific object that contains information for connecting
        /// the record in the Files table with the binary stored in the external storage
        /// (e.g. the name of the folder in the file system where the bytes are stored).
        /// </summary>
        public object BlobProviderData { get; set; }

        /// <summary>
        /// Creates a new instance of the BlobStorageContext class.
        /// </summary>
        /// <param name="provider">Blob provider to work with.</param>
        /// <param name="providerData">Optional existing provider-specific data in text format.</param>
        public BlobStorageContext(IBlobProvider provider, string providerData = null)
        {
            this.Provider = provider;
            if (providerData != null)
                BlobProviderData = provider.ParseData(providerData);
        }

        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        /// <summary>
        /// Serializes a provider-specific data object into a JSON string.
        /// </summary>
        /// <param name="blobProviderData">Provider-specific data (may be null).</param>
        public static string SerializeBlobProviderData(object blobProviderData)
        {
            return blobProviderData == null ? null : JsonConvert.SerializeObject(blobProviderData, SerializerSettings);
        }
        /// <summary>
        /// Deserializes a provider-specific data object from its JSON representation.
        /// </summary>
        /// <typeparam name="T">Provider-specific data type to create.</typeparam>
        /// <param name="blobProviderData">Provider-specific data in a JSON text format.</param>
        public static T DeserializeBlobProviderData<T>(string blobProviderData)
        {
            return blobProviderData == null ? default(T) : JsonConvert.DeserializeObject<T>(blobProviderData);
        }
    }
}
