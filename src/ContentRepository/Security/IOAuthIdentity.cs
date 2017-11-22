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
    }

    /// <summary>
    /// Built-in implementation of the IOAuthIdentity interface. Derived classes may
    /// extend it with additional, provider-specific fields.
    /// </summary>
    public class OAuthIdentity : IOAuthIdentity
    {
        /// <summary>
        /// Full name of the user, may contain any unicode character.
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// User name of the synchronized identity. Will become the name of the User content.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Unique identifier of the user provided by the external OAuth service.
        /// </summary>
        public string Identifier { get; set; }
        /// <summary>
        /// Email address of the user. Optional.
        /// </summary>
        public string Email { get; set; }
    }
}
