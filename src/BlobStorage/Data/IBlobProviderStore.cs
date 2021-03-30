using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines methods for managing a dictionary of blob providers.
    /// </summary>
    public interface IBlobProviderStore : IDictionary<string, IBlobProvider>
    {
        /// <summary>
        /// The built-in blob provider instance that can be found in the fill provider list.
        /// </summary>
        IBuiltInBlobProvider BuiltInBlobProvider { get; }
        /// <summary>
        /// Adds a blob provider to the in-memory storage.
        /// </summary>
        void AddProvider(IBlobProvider provider);
        /// <summary>
        /// Gets a blob provider by its full class name.
        /// </summary>
        IBlobProvider GetProvider(string providerName);
    }
}
