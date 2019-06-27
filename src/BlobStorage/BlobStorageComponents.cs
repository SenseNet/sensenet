using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Contains provider instances for the blob storage.
    /// </summary>
    public class BlobStorageComponents
    {
        private static IBlobStorageMetaDataProvider _dataProvider2;

        /// <summary>
        /// For test purposes only
        /// </summary>
        //UNDONE:DB -------Remove BlobStorageComponents.SetMetadataProvider
        public static void SetMetadataProvider2(IBlobStorageMetaDataProvider dataProvider2)
        {
            _dataProvider2 = dataProvider2;
        }

        ///// <summary>
        ///// Gets or sets the blob storage metadata provider instance used by the blob storage component.
        ///// </summary>
        //public static IBlobStorageMetaDataProvider DataProvider { get; set; } = new MsSqlBlobMetaDataProvider();

        private static IBlobStorageMetaDataProvider _dataProvider = new MsSqlBlobMetaDataProvider();

        public static IBlobStorageMetaDataProvider DataProvider
        {
            get => _dataProvider2;
            set => _dataProvider2 = value;
        }

        /// <summary>
        /// Gets or sets the globally used IBlobProviderSelector instance.
        /// </summary>
        public static IBlobProviderSelector ProviderSelector { get; set; } = new BuiltInBlobProviderSelector();
    }
}
