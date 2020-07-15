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
                    .AssertResponseLimit(response.HttpContext, text.Length);
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
                    .AssertResponseLimit(response.HttpContext, text.Length);
            await response.WriteAsync(text, encoding, cancellationToken);
        }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream
        /// is disposed.</exception>
        /// <exception cref="T:System.NotSupportedException">The current stream does not support reading, or the
        /// destination stream does not support writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        public static void CopyToLimited(this Stream source, Stream destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(source.Length);

            source.CopyTo(destination);
        }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream, using a specified buffer size.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero.
        /// The default size is 81920.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="bufferSize" /> is negative or zero.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream
        /// is disposed.</exception>
        /// <exception cref="T:System.NotSupportedException">The current stream does not support reading, or the
        /// destination stream does not support writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        public static void CopyToLimited(this Stream source, Stream destination, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(source.Length);

            source.CopyTo(destination, bufferSize);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream
        /// is disposed.</exception>
        /// <exception cref="T:System.NotSupportedException">The current stream does not support reading, or the
        /// destination stream does not support writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        public static async Task CopyToLimitedAsync(this Stream source, Stream destination)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(source.Length);

            await source.CopyToAsync(destination);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream,
        /// using a specified buffer size.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero.
        /// The default size is 81920.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="bufferSize" /> is negative or zero.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream
        /// is disposed.</exception>
        /// <exception cref="T:System.NotSupportedException">The current stream does not support reading, or the
        /// destination stream does not support writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        public static async Task CopyToLimitedAsync(this Stream source, Stream destination, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(source.Length);

            await source.CopyToAsync(destination, bufferSize);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream,
        /// using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero.
        /// The default size is 81920.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.
        /// The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="bufferSize" /> is negative or zero.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream
        /// is disposed.</exception>
        /// <exception cref="T:System.NotSupportedException">The current stream does not support reading,  or the
        /// destination stream does not support writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        public static async Task CopyToLimitedAsync(this Stream source, Stream destination, int bufferSize,
            CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(source.Length);

            await source.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /* The base method is implemented in the System.Runtime, Version=4.2.2.0
           Assembly location: C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream,
        /// using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.
        /// The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream
        /// is disposed.</exception>
        /// <exception cref="T:System.NotSupportedException">The current stream does not support reading,  or the
        /// destination stream does not support writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        public static async Task CopyToLimitedAsync(this Stream source, Stream destination, 
            CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(source.Length);

            await source.CopyToAsync(destination, cancellationToken);
        }
        */
    }
}
