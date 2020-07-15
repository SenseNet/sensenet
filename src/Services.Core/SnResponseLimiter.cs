using System;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    /// <inheritdoc/>
    public class SnResponseLimiter : IResponseLimiter
    {
        private const string ResponseLength = "ResponseLength";

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

        public void AssertResponseLimit(HttpContext httpContext, long textLength)
        {
            var length = (long?)httpContext.Items[ResponseLength] ?? 0;
            var newLength = length + textLength;
            httpContext.Items[ResponseLength] = newLength;

            if (newLength > MaxResponseLengthInBytes)
                throw new ApplicationException($"Response length limit exceeded.");
        }

        public long GetCurrentResponseLength(HttpContext httpContext)
        {
            return (long?)httpContext.Items[ResponseLength] ?? 0L;
        }
    }
}
