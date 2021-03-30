using System.Linq;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Internal interface for aiding the built-in blob provider selector class.
    /// </summary>
    public interface IExternalBlobProviderFactory
    {
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
        private IBlobProvider _provider;
        private readonly IBlobProviderStore _providers;
        
        public ExternalBlobProviderFactory(IBlobProviderStore providers)
        {
            _providers = providers;
        }

        public IBlobProvider GetBlobProvider()
        {
            //TODO: [DIBLOB] compare provider types or type full names?
            return _provider ?? (_provider =
                       _providers?.Values.FirstOrDefault(p => p.GetType().FullName == typeof(T).FullName));
        }
    }
}
