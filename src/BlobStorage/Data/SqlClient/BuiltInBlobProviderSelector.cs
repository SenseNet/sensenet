using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Configuration;
using SenseNet.Configuration;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// The selector class is responsible for choosing the blob
    /// provider that will store the binary.
    /// Currently the provider selector cannot be changed, only the
    /// external provider can be configured (BlobProvider key). 
    /// The built-in selector chooses the appropriate blob provider 
    /// based on the size of the file that is being saved.
    /// </summary>
    internal class BuiltInBlobProviderSelector : IBlobProviderSelector
    {
        protected static IBlobProvider ExternalBlobProvider { get; set; }

        static BuiltInBlobProviderSelector()
        {
            // check if there is a configuration for an external blob provider
            if (string.IsNullOrEmpty(BlobStorage.BlobProviderClassName))
                return;

            try
            {
                ExternalBlobProvider = (IBlobProvider)TypeResolver.CreateInstance(BlobStorage.BlobProviderClassName);
                SnLog.WriteInformation("External BlobProvider created by configuration. Type: " + BlobStorage.BlobProviderClassName,
                    EventId.RepositoryLifecycle);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            // We throw an exception in a static constructor (which will prevent this type to work)
            // because if something is wrong with the blob provider configuration, it will affect
            // the whole system and it should be resolved immediately.
            if (ExternalBlobProvider == null)
                throw new ConfigurationErrorsException("Unknown blob provider name in configuration: " + BlobStorage.BlobProviderClassName);
        }

        /// <summary>
        /// Gets a provider based on the binary size and the available blob providers in the system.
        /// </summary>
        /// <param name="fullSize">Full binary length.</param>
        /// <param name="providers">Available blob providers (currently not used).</param>
        /// <param name="builtIn">The built-in provider to be used as a fallback.</param>
        public IBlobProvider GetProvider(long fullSize, Dictionary<string, IBlobProvider> providers, IBlobProvider builtIn)
        {
            // The default algorithm chooses the blob provider based on binary size: below a limit, we 
            // save files to the db, above we use the configured external provider (if there is any).
            if (fullSize < BlobStorage.MinimumSizeForBlobProviderInBytes)
                return builtIn;

            return ExternalBlobProvider ?? builtIn;
        }
    }
}
