namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Specifies the rules how the generated sharing link can be used and by whom.
    /// </summary>
    public enum SharingMode
    {
        /// <summary>
        /// The link can be shared with anybody. The users who click on it 
        /// will be able to access the shared content, regardless of whether
        /// they are visitors or registered users.
        /// Limitation: users will not be able to find the content using a query.
        /// </summary>
        Public,
        /// <summary>
        /// The shared content will be accessible by all registered users in the system.
        /// Users will be able to find the content using a content query.
        /// </summary>
        Authenticated,
        /// <summary>
        /// The shared content will be accessible only by the user whom it was shared.
        /// Even if they share the link, it will not be usable by anybody else.
        /// </summary>
        Private
    }
}
