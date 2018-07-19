using System;
using System.IO;
using SenseNet.Configuration;

#pragma warning disable 1591

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// A cache item that either contains the raw binary (if its size fits into the limit) or
    /// just the blob metadata pointing to the blob storage.
    /// </summary>
    public class BinaryCacheEntity
    {
        /// <summary>
        /// Full binary in case of small files.
        /// </summary>
        public byte[] RawData { get; set; }
        /// <summary>
        /// Length of the full stream.
        /// </summary>
        public long Length { get; set; }
        /// <summary>
        /// Binary property id in the metadata database.
        /// </summary>
        public int BinaryPropertyId { get; set; }
        /// <summary>
        /// File id in the meadata database. It points to a record that contains 
        /// the provider-specific blob storage information about the binary.
        /// </summary>
        public int FileId { get; set; }

        /// <summary>
        /// Provider-specific context information for binary operations.
        /// </summary>
        public BlobStorageContext Context { get; set; }
        /// <summary>
        /// Gets a cache key for memorizing binary cache entities.
        /// </summary>
        /// <param name="versionId">Content version id.</param>
        /// <param name="propertyTypeId">Binary property type id.</param>
        public static string GetCacheKey(int versionId, int propertyTypeId)
        {
            return string.Concat("RawBinary.", versionId, ".", propertyTypeId);
        }
    }

    /// <summary>
    /// Stream implementation that serves binaries from the blob storage using an in-memory byte array buffer.
    /// </summary>
    public class RepositoryStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Position { get; set; }

        public override long Length { get; }

        private byte[] _innerBuffer;

        private long _innerBufferFirstPostion;

        public int FileId { get; set; }

        public RepositoryStream(int fileId, long size, byte[] binary = null)
        {
            Length = size;
            FileId = fileId;
            if (binary != null)
                _innerBuffer = binary;
        }

        public override void SetLength(long value)
		{ throw new NotSupportedException("RepositoryStream does not support setting length."); }

        public override void Write(byte[] buffer, int offset, int count)
		{ throw new NotSupportedException("RepositoryStream does not support writing."); }

        public override void Flush()
		{ throw new NotSupportedException("RepositoryStream does not support flushing."); }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset + count > buffer.Length)
                throw new ArgumentException("Offset + count must not be greater than the buffer length.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset must be greater than zero.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count must be greater than zero.");

            // Calculate the maximum count of the bytes that can be read.
            // Return immediately if nothing to read.
            var maximumReadableByteCount = Length - Position;
            if (maximumReadableByteCount < 1)
                return 0;

            var realCount = (int)Math.Min(count, maximumReadableByteCount);

            if (CanInnerBufferHandleReadRequest(realCount))
            {
                Array.Copy(_innerBuffer, (int)Position - _innerBufferFirstPostion, buffer, offset, realCount);
            }
            else
            {
                _innerBuffer = null;

                var bytesRead = 0;
                var bytesStoredInInnerBuffer = 0;

                while (bytesRead < realCount)
                {
                    // bytes to load from the db
                    var bytesToReadInThisIteration = (int)Math.Min(this.Length - Position - bytesRead, BlobStorage.BinaryChunkSize);

                    // bytes that we will copy to the buffer of the caller
                    var bytesToStoreInThisIteration = Math.Min(bytesToReadInThisIteration, realCount - bytesRead);

                    // stores the current chunk
                    var tempBuffer = BlobStorageBase.LoadBinaryFragment(this.FileId, Position + bytesRead, bytesToReadInThisIteration);

                    // first iteration: create inner buffer for caching a part of the stream in memory
                    if (_innerBuffer == null)
                    {
                        _innerBuffer = new byte[GetInnerBufferSize(realCount)];
                        _innerBufferFirstPostion = Position;
                    }

                    // store a fragment of the data in the inner buffer if possible
                    if (bytesStoredInInnerBuffer < _innerBuffer.Length)
                    {
                        var bytesToStoreInInnerBuffer = Math.Min(bytesToReadInThisIteration, _innerBuffer.Length - bytesStoredInInnerBuffer);

                        Array.Copy(tempBuffer, 0, _innerBuffer, bytesStoredInInnerBuffer, bytesToStoreInInnerBuffer);
                        bytesStoredInInnerBuffer += bytesToStoreInInnerBuffer;
                    }

                    // copy the chunk from the temp buffer to the buffer of the caller
                    Array.Copy(tempBuffer, 0, buffer, bytesRead, bytesToStoreInThisIteration);
                    bytesRead += bytesToReadInThisIteration;
                }
            }

            Position += realCount;

            return realCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                default:
                    throw new NotSupportedException(string.Concat("SeekOrigin type ", origin, " is not supported."));
            }
            return Position;
        }

        // ========================================================= Helper methods

        private bool CanInnerBufferHandleReadRequest(int count)
        {
            if (_innerBuffer == null)
                return false;

            if (Position < _innerBufferFirstPostion)
                return false;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (_innerBufferFirstPostion + _innerBuffer.Length < Position + count)
                return false;

            return true;
        }

        private static int GetInnerBufferSize(int realCount)
        {
            // Determine the inner buffer size. It should not be bigger 
            // than all the data that will be loaded in this Read method call.
            return realCount <= BlobStorage.BinaryChunkSize
                ? Math.Min(BlobStorage.BinaryChunkSize, BlobStorage.BinaryBufferSize)
                : Math.Min((realCount / BlobStorage.BinaryChunkSize + 1) * BlobStorage.BinaryChunkSize, BlobStorage.BinaryBufferSize);
        }
    }
}
