using System.Collections.Generic;
using System.Linq;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary>
    /// Internal interface for aiding the built-in blob provider selector class.
    /// </summary>
    public interface IExternalBlobProviderFactory
    {
        //UNDONE: [DIBLOB] [CIRCLE] remove IBlobProvider reference --> GetBlobProviderType
        IBlobProvider GetBlobProvider();
    }

    /// <summary>
    /// Default external provider factory that returns null.
    /// </summary>
    public class NullExternalBlobProviderFactory : IExternalBlobProviderFactory
    {
        public IBlobProvider GetBlobProvider() { return null; }
    }

    /// <summary>
    /// Functional external provider factory that returns the provider instance
    /// that matches the provided type.
    /// </summary>
    /// <typeparam name="T">The main blob provider type.</typeparam>
    public class ExternalBlobProviderFactory<T> : IExternalBlobProviderFactory where T : IBlobProvider
    {
        private readonly IBlobProvider _provider;
        public ExternalBlobProviderFactory(IEnumerable<IBlobProvider> providers)
        {
            _provider = providers?.FirstOrDefault(p => p.GetType().FullName == typeof(T).FullName);
        }
        public IBlobProvider GetBlobProvider() { return _provider; }
    }
}
