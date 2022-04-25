using System;

namespace SenseNet.ContentRepository.Security
{
    /// <summary>
    /// Defines members for an OAuth provider. Developers should not implement this
    /// interface directly: use the OAuthProvider base class instead.
    /// </summary>
    [Obsolete("This feature is obsolete. Use newer authentication methods.")]
    public interface IOAuthProvider
    {
        string ProviderName { get; }
        string IdentifierFieldName { get; }
    }
}
