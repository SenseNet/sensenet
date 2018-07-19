using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 1591

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// A wrapper stream that provides a unified stream interface on the top of different data 
    /// sources (e.g. an in-memory byte array or a real stream loaded from the blob provider).
    /// </summary>
    public class SnStream : Stream
    {
        private readonly Stream _underlyingStream;

        public override bool CanRead => _underlyingStream.CanRead;
        public override bool CanSeek => _underlyingStream.CanSeek;
        public override bool CanWrite => _underlyingStream.CanWrite;
        public override long Length => _underlyingStream.Length;

        public override long Position
        {
            get { return _underlyingStream.Position; }
            set { _underlyingStream.Position = value; }
        }

        internal BlobStorageContext Context { get; }

        public SnStream(BlobStorageContext context, byte[] rawData = null)
        {
            Context = context;

            _underlyingStream = rawData != null
                ? new RepositoryStream(context.FileId, context.Length, rawData)
                : context.Provider.GetStreamForRead(context);
        }
        private SnStream(BlobStorageContext context, Stream underlyingStream)
        {
            Context = context;
            _underlyingStream = underlyingStream;
        }

        public override void Flush()
        {
            _underlyingStream.Flush();
        }
        public override void SetLength(long value)
        {
            if (!CanWrite)
                throw new NotSupportedException("SnStream does not support setting length.");
            _underlyingStream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("SnStream does not support writing.");
            _underlyingStream.Write(buffer, offset, count);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
                throw new NotSupportedException("SnStream does not support seeking.");
            _underlyingStream.Seek(offset, origin);
            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("SnStream does not support reading.");
            return _underlyingStream.Read(buffer, offset, count);
        }

        public SnStream Clone()
        {
            return new SnStream(Context, Context.Provider.CloneStream(Context, _underlyingStream));
        }

        public override void Close()
        {
            _underlyingStream.Close();
        }
        protected override void Dispose(bool disposing)
        {
            _underlyingStream.Dispose();
        }
        
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (!CanRead)
                throw new NotSupportedException("SnStream does not support reading.");
            return _underlyingStream.BeginRead(buffer, offset, count, callback, state);
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (!CanWrite)
                throw new NotSupportedException("SnStream does not support writing.");
            return _underlyingStream.BeginWrite(buffer, offset, count, callback, state);
        }
        public override bool CanTimeout => _underlyingStream.CanTimeout;

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _underlyingStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            if (!CanRead)
                throw new NotSupportedException("SnStream does not support reading.");
            return _underlyingStream.EndRead(asyncResult);
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (!CanWrite)
                throw new NotSupportedException("SnStream does not support writing.");
            _underlyingStream.EndWrite(asyncResult);
        }
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _underlyingStream.FlushAsync(cancellationToken);
        }
        public override bool Equals(object obj)
        {
            return _underlyingStream.Equals(obj);
        }
        public override int GetHashCode()
        {
            return _underlyingStream.GetHashCode();
        }
        public override object InitializeLifetimeService()
        {
            return _underlyingStream.InitializeLifetimeService();
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanRead)
                throw new NotSupportedException("SnStream does not support reading.");
            return _underlyingStream.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public override int ReadByte()
        {
            if (!CanRead)
                throw new NotSupportedException("SnStream does not support reading.");
            return _underlyingStream.ReadByte();
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite)
                throw new NotSupportedException("SnStream does not support writing.");
            return _underlyingStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
        public override void WriteByte(byte value)
        {
            if (!CanWrite)
                throw new NotSupportedException("SnStream does not support writing.");
            _underlyingStream.WriteByte(value);
        }
        public override int ReadTimeout
        {
            get { return _underlyingStream.ReadTimeout; }
            set { _underlyingStream.ReadTimeout = value; }
        }
        public override int WriteTimeout
        {
            get { return _underlyingStream.WriteTimeout; }
            set { _underlyingStream.WriteTimeout = value; }
        }
    }
}
