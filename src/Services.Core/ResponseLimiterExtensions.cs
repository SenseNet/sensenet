using SenseNet.Configuration;
using SenseNet.Services.Core;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ResponseLimiterExtensions
    {
        /// <summary>
        /// Switches the ResponseLengthLimiter feature on.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="maxResponseLengthInBytes">Response length limit value in bytes (optional).</param>
        /// <param name="maxFileLengthInBytes">File length limit value in bytes (optional).</param>
        /// <returns>The IRepositoryBuilder instance.</returns>
        public static IRepositoryBuilder UseResponseLimiter(this IRepositoryBuilder builder,
            long maxResponseLengthInBytes = 0, long maxFileLengthInBytes = 0)
        {
            Providers.Instance.SetProvider(typeof(IResponseLimiter),
                new SnResponseLimiter(maxResponseLengthInBytes, maxFileLengthInBytes));

            return builder;
        }
    }
}
