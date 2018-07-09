namespace SenseNet.MsSqlFsBlobProvider
{
    /// <summary>
    /// Holds context information for SqlFileStream operations.
    /// </summary>
    internal class SqlFileStreamData
    {
        public string Path { get; set; }
        public byte[] TransactionContext { get; set; }
    }
}

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// This class is used in legacy databases.
    /// </summary>
    internal class FileStreamData : MsSqlFsBlobProvider.SqlFileStreamData
    {
    }
}