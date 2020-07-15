using System;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;

namespace SenseNet.Services.Core
{
    public static class StreamExtensions
    {
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
            if (source == null)
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

        //UNDONE: Upgrade SDK NETStandard.Library (2.1) or NETCore (2.1) or higher
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

        public static IAsyncResult BeginWriteLimited(this Stream stream, byte[] buffer, int offset, int count,
            AsyncCallback callback, object state)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(stream.Length + count);

            return stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public static async Task WriteLimitedAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertResponseLimit(stream.Length + count);

            await stream.WriteAsync(buffer, offset, count);
        }

        public static async Task WriteLimitedAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(stream.Length + count);

            await stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        //UNDONE: Upgrade SDK NETStandard.Library (2.1) or NETCore (2.1) or higher
        //public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        //{

        //}

        public static void WriteLimited(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(stream.Length + count);

            stream.Write(buffer, offset, count);
        }

        //UNDONE: Upgrade SDK NETStandard.Library (2.1) or NETCore (2.1) or higher
        //public static void WriteLimited(this Stream stream, ReadOnlySpan<byte> buffer)
        //{
        //    if (stream == null)
        //        throw new ArgumentNullException(nameof(stream));

        //    Providers.Instance.GetProvider<IResponseLimiter>()?
        //        .AssertFileLength(stream.Length + buffer.Length);

        //    stream.Write(buffer);
        //}

        public static void WriteByteLimited(this Stream stream, byte value)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Providers.Instance.GetProvider<IResponseLimiter>()?
                .AssertFileLength(stream.Length + 1);

            stream.WriteByte(value);
        }

    }
}
