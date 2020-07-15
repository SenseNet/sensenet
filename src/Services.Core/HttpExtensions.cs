using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    public static class HttpExtensions
    {
        /// <summary>
        /// Writes the given text to the response body. UTF-8 encoding will be used.
        /// </summary>
        /// <param name="response">The <see cref="T:Microsoft.AspNetCore.Http.HttpResponse" />.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static async Task WriteLimitedAsync(this HttpResponse response, string text,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (text != null)
                Providers.Instance.GetProvider<IResponseLimiter>()?
                    .AssertResponseLimit(response.Body.Length + text.Length);
            await response.WriteAsync(text, cancellationToken);
        }

        /// <summary>
        /// Writes the given text to the response body using the given encoding.
        /// </summary>
        /// <param name="response">The <see cref="T:Microsoft.AspNetCore.Http.HttpResponse" />.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static async Task WriteLimitedAsync(this HttpResponse response, string text, Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if(text != null)
                Providers.Instance.GetProvider<IResponseLimiter>()?
                    .AssertResponseLimit(response.Body.Length + text.Length);
            await response.WriteAsync(text, encoding, cancellationToken);
        }
    }
}
