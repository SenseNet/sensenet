using System;
using System.IO;

namespace SenseNet.BlobStorage.IntegrationTests.Implementations
{
    internal class FileSystemChunkReaderStream : Stream
    {
        private readonly string _directoryPath;

        private readonly int _chunkSize;
        private int _currentChunkIndex;

        private int _loadedChunkIndex = -1;
        private byte[] _loadedBytes;

        public FileSystemChunkReaderStream(LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData providerData, long fullSize, string directoryPath)
        {
            _directoryPath = directoryPath;
            Length = fullSize;
            _chunkSize = providerData.ChunkSize;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length { get; }

        // ReSharper disable once InconsistentNaming
        private long __position;
        public override long Position
        {
            get => __position;
            set
            {
                _currentChunkIndex = (value / _chunkSize).ToInt();
                (value % _chunkSize).ToInt();
                __position = value;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min((Length - Position), count).ToInt();

            var totalCount = 0;
            while (count > 0)
            {
                var chunkOffset = (long)_chunkSize * _currentChunkIndex;

                if (_currentChunkIndex != _loadedChunkIndex)
                {
                    _loadedBytes = LoadChunk(_currentChunkIndex);
                    if (_loadedBytes == null)
                        throw new ApplicationException(
                            $"Chunk not found. ChunkIndex:{_currentChunkIndex}");

                    _loadedChunkIndex = _currentChunkIndex;
                }
                var copiedCount = CopyBytes(_loadedBytes, (Position - chunkOffset).ToInt(), buffer, offset, count);

                Position += copiedCount;
                offset += copiedCount;
                count -= copiedCount;
                totalCount += copiedCount;
                if (Position >= Length)
                    break;
            }
            return totalCount;
        }

        private byte[] LoadChunk(int chunkIndex)
        {
            var path = Path.Combine(_directoryPath, chunkIndex.ToString());
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var streamLength = stream.Length.ToInt();
                var bytes = new byte[streamLength];
                stream.Read(bytes, 0, streamLength);
                return bytes;
            }
        }

        private int CopyBytes(byte[] source, int sourceOffset, byte[] target, int targetOffset, int expectedCount)
        {
            var availableSourceCount = source.Length - sourceOffset;
            var availableTargetCount = target.Length - targetOffset;
            var availableCount = Math.Min(Math.Min(availableSourceCount, availableTargetCount), expectedCount);

            Array.ConstrainedCopy(source, sourceOffset, target, targetOffset, availableCount);

            return availableCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var position = Position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = Length - offset;
                    break;
            }
            if (position < 0 || position > Length - 1)
                throw new ApplicationException($"Invalid offset. Expected max:{Length}, requested:{offset}");
            Position = position;
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        //=======================================================================


    }
}
