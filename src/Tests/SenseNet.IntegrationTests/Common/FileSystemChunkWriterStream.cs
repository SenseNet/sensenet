using System;
using System.IO;

namespace SenseNet.IntegrationTests.Common
{
    internal class FileSystemChunkWriterStream : Stream
    {
        private LocalDiskChunkBlobProvider _blobProvider;

        private Guid _id;
        private readonly int _chunkSize;

        private int _currentChunkIndex;
        private int _currentChunkPosition;
        private byte[] _buffer;
        private bool _flushIsNecessary;
        
        public FileSystemChunkWriterStream(LocalDiskChunkBlobProvider blobProvider, LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData providerData, long fullSize)
        {
            _blobProvider = blobProvider;
            Length = fullSize;
            _chunkSize = providerData.ChunkSize;
            _id = providerData.Id;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length { get; }

        private long _position;
        public override long Position
        {
            get => _position;
            // set Position value only through the private SetPosition method
            set => throw new NotSupportedException();
        }
        private void SetPosition(long position)
        {
            _currentChunkIndex = (position / _chunkSize).ToInt();
            _currentChunkPosition = (position % _chunkSize).ToInt();
            _position = position;
        }

        public override void Flush()
        {
            // nothing to write to the db
            if (!_flushIsNecessary)
                return;

            var bytesToWrite = _currentChunkPosition;
            var chunkIndex = _currentChunkIndex;

            // If the current chunk position is 0, that means we are at the beginning of the
            // next chunk, so we have to write all bytes (chunk size) from the buffer using
            // the previous chunk index.
            if (_currentChunkPosition == 0)
            {
                bytesToWrite = _chunkSize;
                chunkIndex = _currentChunkIndex - 1;
            }

            byte[] bytes;
            if (bytesToWrite == _buffer.Length)
            {
                bytes = _buffer;
            }
            else
            {
                bytes = new byte[bytesToWrite];
                Array.ConstrainedCopy(_buffer, 0, bytes, 0, bytesToWrite);
            }

            _blobProvider.WriteChunk(_id,chunkIndex, bytes);

            _flushIsNecessary = false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || buffer.Length < offset + count)
                throw new ArgumentException(string.Format("Invalid write parameters: buffer length {0}, offset {1}, count {2}.",
                    buffer.Length, offset, count));

            // nothing to write
            if (count == 0)
                return;

            if (Position >= Length)
                throw new InvalidOperationException("Stream length exceeded.");

            // Initialize buffer here and not in the constructor 
            // to allocate memory only when it is needed.
            if (_buffer == null)
                _buffer = new byte[_chunkSize];

            var bytesToWrite = count;
            while (bytesToWrite > 0)
            {
                // if the inner buffer is already full, write it to the db
                if (_currentChunkPosition >= _chunkSize || _currentChunkPosition == 0 && _flushIsNecessary)
                {
                    Flush();

                    if (_currentChunkPosition >= _chunkSize)
                    {
                        // reset inner buffer position and move to the next chunk index
                        _currentChunkPosition = 0;
                        _currentChunkIndex++;
                    }
                }

                // we can only write so much bytes in one round as many slots are left in the inner buffer
                var maxBytesToWrite = Math.Min(bytesToWrite, _chunkSize - _currentChunkPosition);

                Array.ConstrainedCopy(buffer, offset, _buffer, _currentChunkPosition, maxBytesToWrite);

                bytesToWrite -= maxBytesToWrite;
                offset += maxBytesToWrite;
                _currentChunkPosition += maxBytesToWrite;
                _flushIsNecessary = true;
            }

            SetPosition(Position + count);
        }
        //UNDONE:DB:BLOB: Override WriteAsync instead of Write

        protected override void Dispose(bool disposing)
        {
            try
            {
                Flush();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
