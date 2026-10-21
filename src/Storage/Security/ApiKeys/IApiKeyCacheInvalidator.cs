using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Security.ApiKeys
{
    /// <summary>
    /// Optional capability of an <see cref="IApiKeyManager"/> that caches API keys.
    /// </summary>
    public interface IApiKeyCacheInvalidator
    {
        /// <summary>
        /// Invalidates cached API keys locally and on other repository instances.
        /// </summary>
        Task InvalidateCacheAsync(CancellationToken cancellationToken);
    }
}
