namespace SenseNet.MsSqlFsBlobProvider
{
    /// <summary>
    /// Blob provider data for the built-in blob provider.
    /// </summary>
    internal class SqlFileStreamBlobProviderData
    {
        /// <summary>
        /// Custom data for the Filestream column.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("FileStreamData")]
        public SqlFileStreamData FileStreamData { get; set; }
    }
}
