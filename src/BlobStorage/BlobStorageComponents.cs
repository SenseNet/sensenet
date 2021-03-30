using System;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Contains provider instances for the blob storage.
    /// </summary>
    [Obsolete("Register services using the new dependency injection methods.", true)]
    public class BlobStorageComponents
    {
        /// <summary>
        /// Gets or sets the blob storage metadata provider instance used by the blob storage component.
        /// </summary>
        public static IBlobStorageMetaDataProvider DataProvider { get; set; }

        /// <summary>
        /// Gets or sets the globally used IBlobProviderSelector instance.
        /// </summary>
        public static IBlobProviderSelector ProviderSelector { get; set; }
    }
}
