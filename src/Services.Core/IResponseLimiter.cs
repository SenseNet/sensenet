using System.IO;
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
        /// <param name="responseLength">Desired length.</param>
        void AssertResponseLimit(long responseLength);
    }
}
