using System;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    /// <inheritdoc/>
    public class SnResponseLengthLimiter : IResponseLengthLimiter
    {
        private const string ResponseLength = "ResponseLength";
        public int Limit { get; }

        public SnResponseLengthLimiter(int limit = 0)
        {
            Limit = limit > 0 ? limit : Limits.MaxResponseLength;
        }

        public void AssertLimit(HttpContext httpContext, string text)
        {
            var length = (long?)httpContext.Items[ResponseLength] ?? 0;
            var newLength = length + text.Length;
            httpContext.Items[ResponseLength] = newLength;

            if (newLength > Limit)
                throw new ApplicationException($"Response limit exceeded.");
        }
    }
}
