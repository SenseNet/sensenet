using System;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    /// <inheritdoc/>
    public class SnResponseLimiter : IResponseLimiter
    {
        public long MaxFileLengthInBytes { get; }
        public long MaxResponseLengthInBytes { get; }

        public SnResponseLimiter(long maxResponseLengthInBytes = 0, long maxFileLengthInBytes = 0)
        {
            MaxResponseLengthInBytes = maxResponseLengthInBytes > 0 ? maxResponseLengthInBytes : Limits.MaxResponseLengthInBytes;
            MaxFileLengthInBytes = maxFileLengthInBytes > 0 ? maxFileLengthInBytes : Limits.MaxFileLengthInBytes;
        }

        public void AssertFileLength(long fileLength)
        {
            if (fileLength > MaxFileLengthInBytes)
                throw new ApplicationException($"File length limit exceeded.");
        }

        public void AssertResponseLimit(long responseLength)
        {
            if (responseLength > MaxResponseLengthInBytes)
                throw new ApplicationException($"Response length limit exceeded.");
        }
    }
}
