namespace SenseNet.ContentRepository.Security
{
    /// <summary>
    /// Defines a minimal set of fields that should be filled by an OAuth provider implementation.
    /// </summary>
    public interface IOAuthIdentity
    {
        /// <summary>
        /// Full name of the user, may contain any unicode character.
        /// </summary>
        string FullName { get; }
        /// <summary>
        /// User name of the synchronized identity. Will become the name of the User content.
        /// </summary>
        string Username { get; }
        /// <summary>
        /// Unique identifier of the user provided by the external OAuth service.
        /// </summary>
        string Identifier { get; }
        /// <summary>
        /// Email address of the user. Optional.
        /// </summary>
        string Email { get; }
        /// <summary>
        /// Profile image url. Optional.
        /// </summary>
        string AvatarUrl { get; }
    }

    /// <summary>
    /// Built-in implementation of the IOAuthIdentity interface. Derived classes may
    /// extend it with additional, provider-specific fields.
    /// </summary>
    public class OAuthIdentity : IOAuthIdentity
    {
        /// <inheritdoc />
        public string FullName { get; set; }
        /// <inheritdoc />
        public string Username { get; set; }
        /// <inheritdoc />
        public string Identifier { get; set; }
        /// <inheritdoc />
        public string Email { get; set; }
        /// <inheritdoc />
        public string AvatarUrl { get; set; }
    }
}
