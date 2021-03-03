using System;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.ContentRepository.Storage
{
    [Obsolete("Register services using the new dependency injection methods.", true)]
    /// <summary>
    /// Contains provider instances for the blob storage.
    /// </summary>
    public class BlobStorageComponents
    {
        //UNDONE: [DIBLOB] register blob provider property values as services

        /// <summary>
        /// Gets or sets the blob storage metadata provider instance used by the blob storage component.
        /// </summary>
        public static IBlobStorageMetaDataProvider DataProvider { get; set; } // = new MsSqlBlobMetaDataProvider();

        /// <summary>
        /// Gets or sets the globally used IBlobProviderSelector instance.
        /// </summary>
        public static IBlobProviderSelector ProviderSelector { get; set; } // = new BuiltInBlobProviderSelector();
    }
}
