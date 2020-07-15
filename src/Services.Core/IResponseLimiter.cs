using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Core
{
    public interface IResponseLimiter
    {
        /// <summary>
        /// Gets the maximum file length in bytes.
        /// </summary>
        long MaxFileLengthInBytes { get; }
        /// <summary>
        /// Gets the maximum response length in bytes.
        /// </summary>
        long MaxResponseLengthInBytes { get; }

        /// <summary>
        /// If the passed value exceeds the configured MaxFileLengthInBytes limit,
        /// ApplicationException will be thrown.
        /// </summary>
        /// <param name="fileLength">The length of file to write.</param>
        void AssertFileLength(long fileLength);

        /// <summary>
        /// Aggregates the "textLength" using the httpContext as a background storage.
        /// If the aggregated value exceeds the configured MaxResponseLengthInBytes limit,
        /// ApplicationException will be thrown.
        /// </summary>
        /// <param name="httpContext">The current HttpContext instance.</param>
        /// <param name="textLength">The length of the text to write.</param>
        void AssertResponseLimit(HttpContext httpContext, long textLength);

        /// <summary>
        /// Returns all written bytes. Designed only test purposes.
        /// </summary>
        /// <param name="httpContext">The current HttpContext instance.</param>
        /// <returns>Byte count.</returns>
        long GetCurrentResponseLength(HttpContext httpContext);
    }
}
