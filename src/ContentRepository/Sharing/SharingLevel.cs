namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Specifies the level of access the user will have for the shared content.
    /// </summary>
    public enum SharingLevel
    {
        /// <summary>
        /// Users will be able to open the content, access and download major versions
        /// of documents.
        /// </summary>
        Open,
        /// <summary>
        /// Users will be able to modify the content, even documents.
        /// </summary>
        Edit
    }
}
