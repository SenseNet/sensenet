using System;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    public class ResponseLimiter
    {
        public static void AssertFileLength(long fullLength)
        {
            Providers.Instance.GetProvider<IResponseLimiter>()?.AssertFileLength(fullLength);
        }
        public static void AssertResponseLength(long totalLength)
        {
            Providers.Instance.GetProvider<IResponseLimiter>()?.AssertResponseLength(totalLength);
        }
        public static void AssertResponseLength(HttpResponse response, long partialLength)
        {
            Providers.Instance.GetProvider<IResponseLimiter>()?.AssertResponseLength(response, partialLength);
        }
        public static long GetCurrentResponseLength(HttpResponse response)
        {
            return (long?)response.HttpContext.Items[SnResponseLimiter.ResponseLength] ?? 0L;
        }

        public static void ModifyResponseLengthLimit(long maxResponseLengthInBytes)
        {
            var provider = Providers.Instance.GetProvider<IResponseLimiter>();
            if(provider != null)
                provider.MaxResponseLengthInBytes = maxResponseLengthInBytes;
        }

        public static void ModifyFileLengthLimit(long maxFileLengthInBytes)
        {
            var provider = Providers.Instance.GetProvider<IResponseLimiter>();
            if (provider != null)
                provider.MaxFileLengthInBytes = maxFileLengthInBytes;
        }
    }

    /// <inheritdoc/>
    public class SnResponseLimiter : IResponseLimiter
    {
        /// <inheritdoc/>
        public long MaxFileLengthInBytes { get; set; }

        /// <inheritdoc/>
        public long MaxResponseLengthInBytes { get; set; }

        public SnResponseLimiter(long maxResponseLengthInBytes = 0, long maxFileLengthInBytes = 0)
        {
            MaxResponseLengthInBytes = maxResponseLengthInBytes > 0 ? maxResponseLengthInBytes : Limits.MaxResponseLengthInBytes;
            MaxFileLengthInBytes = maxFileLengthInBytes > 0 ? maxFileLengthInBytes : Limits.MaxFileLengthInBytes;
        }

        /// <inheritdoc/>
        public void AssertFileLength(long fileLength)
        {
            if (fileLength > MaxFileLengthInBytes)
                throw new ApplicationException($"File length limit exceeded.");
        }

        /// <inheritdoc/>
        public void AssertResponseLength(long totalLength)
        {
            if (totalLength > MaxResponseLengthInBytes)
                throw new ApplicationException($"Response length limit exceeded.");
        }

        internal const string ResponseLength = "ResponseLength";
        /// <inheritdoc/>
        public void AssertResponseLength(HttpResponse response, long partialLength)
        {
            var length = (long?)response.HttpContext.Items[ResponseLength] ?? 0;
            var totalLength = length + partialLength;

            AssertResponseLength(totalLength);

            response.HttpContext.Items[ResponseLength] = totalLength;
        }
    }
}
