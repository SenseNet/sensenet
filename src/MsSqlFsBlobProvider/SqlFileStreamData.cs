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
