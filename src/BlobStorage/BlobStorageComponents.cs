using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.ContentRepository.Storage
{
    internal class BlobStorageComponents
    {
        /// <summary>
        /// Blob storage metadata provider instance used by the blob storage component.
        /// Currently this property is hardcoded as an MsSqlBlobMetaDataProvider. 
        /// Later it will be possible to change it using property injection.
        /// </summary>
        internal static IBlobStorageMetaDataProvider DataProvider { get; set; } = new MsSqlBlobMetaDataProvider();
    }
}
