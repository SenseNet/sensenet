using SenseNet.Configuration;
using SenseNet.Services.Core;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Switches the ResponseLengthLimiter feature on.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="maxLength">Limit value in bytes (optional).</param>
        /// <returns>The IRepositoryBuilder instance.</returns>
        public static IRepositoryBuilder UseResponseLengthLimiter(this IRepositoryBuilder builder, int maxLength = 0)
        {
            Providers.Instance.SetProvider(typeof(IResponseLengthLimiter), new SnResponseLengthLimiter(maxLength));

            return builder;
        }
    }
}
