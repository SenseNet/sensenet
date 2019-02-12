namespace SenseNet.ContentRepository.Security
{
    /// <summary>
    /// Defines members for an OAuth provider. Developers should not implement this
    /// interface directly: use the OAuthProvider base class instead.
    /// </summary>
    public interface IOAuthProvider
    {
        string ProviderName { get; }
        string IdentifierFieldName { get; }
    }
}
