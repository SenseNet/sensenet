namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines an interface for blob provider selector implementations.
    /// </summary>
    public interface IBlobProviderSelector
    {
        /// <summary>
        /// Gets a provider based on the binary size and the available blob providers in the system.
        /// </summary>
        /// <param name="fullSize">Full binary length.</param>
        /// <param name="builtIn">The special built-in provider.</param>
        /// <returns>Returns the appropriate provider based on the environment.</returns>
        IBlobProvider GetProvider(long fullSize, IBlobProvider builtIn);
    }
}
