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
        /// If the passed value exceeds the configured MaxResponseLengthInBytes limit,
        /// ApplicationException will be thrown.
        /// </summary>
        /// <param name="totalLength">The length of response to write.</param>
        void AssertResponseLength(long totalLength);

        /// <summary>
        /// If the length of <paramref name="response"/> and <paramref name="partialLength"/> exceeds the
        /// configured MaxResponseLengthInBytes limit, ApplicationException will be thrown.
        /// If this method is called more times in one HTTP request, the <paramref name="partialLength"/> values
        /// will be summarized.
        /// </summary>
        /// <param name="response">The response that will be written.</param>
        /// <param name="partialLength">The current length of data to write.</param>
        void AssertResponseLength(HttpResponse response, long partialLength);
    }
}
