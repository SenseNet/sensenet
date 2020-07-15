using System;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    public class ResponseLimiter
    {
        public static void AssertResponseLength(long length)
        {
            Providers.Instance.GetProvider<IResponseLimiter>()?.AssertResponseLimit(length);
        }
        public static void AssertFileLength(long length)
        {
            Providers.Instance.GetProvider<IResponseLimiter>()?.AssertFileLength(length);
        }
    }

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
