namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Holds context information for SqlFileStream operations.
    /// </summary>
    internal class FileStreamData
    {
        public string Path { get; set; }
        public byte[] TransactionContext { get; set; }
    }
}
