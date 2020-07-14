using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core
{
    public interface IResponseLengthLimiter
    {
        /// <summary>
        /// Aggregates the "text" sizes using the httpContext as a background storage.
        /// If the aggregated value exceeds the configured limit, ApplicationException will be thrown.
        /// </summary>
        /// <param name="httpContext">The current HttpContext instance.</param>
        /// <param name="text">The text to write.</param>
        void AssertLimit(HttpContext httpContext, string text);

        /// <summary>
        /// Returns all written bytes. Designed only test purposes.
        /// </summary>
        /// <param name="httpContext">The current HttpContext instance.</param>
        /// <returns>Byte count.</returns>
        long GetCurrentLength(HttpContext httpContext);
    }
}
