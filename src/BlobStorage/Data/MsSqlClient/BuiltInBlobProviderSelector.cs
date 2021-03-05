using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary>
    /// The selector class is responsible for choosing the blob
    /// provider that will store the binary.
    /// Currently the provider selector cannot be changed, only the
    /// external provider can be configured (BlobProvider key). 
    /// The built-in selector chooses the appropriate blob provider 
    /// based on the size of the file that is being saved.
    /// </summary>
    public class BuiltInBlobProviderSelector : IBlobProviderSelector
    {
        /// <summary>
        /// A custom blob provider instance that will be used when the file size exceeds a certain configured value.
        /// </summary>
        internal IBlobProvider ExternalBlobProvider { get; }

        /// <summary>
        /// Initializes an instance of the BuiltInBlobProviderSelector
        /// </summary>
        public BuiltInBlobProviderSelector(IExternalBlobProviderFactory externalBlobProviderFactory)
        {
            ExternalBlobProvider = externalBlobProviderFactory?.GetBlobProvider();
        }

        /// <summary>
        /// Gets a provider based on the binary size and the available blob providers in the system.
        /// </summary>
        /// <param name="fullSize">Full binary length.</param>
        /// <param name="providers">Available blob providers (currently not used).</param>
        /// <param name="builtIn">The built-in provider to be used as a fallback.</param>
        public IBlobProvider GetProvider(long fullSize, Dictionary<string, IBlobProvider> providers, IBlobProvider builtIn)
        {
            //UNDONE: [DIBLOB] [CIRCLE] pin external provider here lazily to remove service reference

            // The default algorithm chooses the blob provider based on binary size: below a limit, we 
            // save files to the db, above we use the configured external provider (if there is any).
            if (fullSize < BlobStorage.MinimumSizeForBlobProviderInBytes)
                return builtIn;

            return ExternalBlobProvider ?? builtIn;
        }
    }
}
