using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.BlobStorage.IntegrationTests.Implementations
{
    internal class FileSystemChunkReaderStream : Stream
    {
        private string _directoryPath;

        private long _length;
        private int _chunkSize;
        private int _currentChunkIndex;
        private int _currentChunkPosition;

        private int _loadedChunkIndex = -1;
        private byte[] _loadedBytes;

        public FileSystemChunkReaderStream(LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData providerData, long fullSize, string directoryPath)
        {
            _directoryPath = directoryPath;
            _length = fullSize;
            _chunkSize = providerData.ChunkSize;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override long Length
        {
            get
            {
                return _length;
            }
        }

        private long __position;
        public override long Position
        {
            get { return __position; }
            set
            {
                _currentChunkIndex = (value / _chunkSize).ToInt();
                _currentChunkPosition = (value % _chunkSize).ToInt();
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
                            string.Format("Chunk not found. ChunkIndex:{0}", _currentChunkIndex));

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

            byte[] bytes = null;

            using (var stream = new FileStream(path, FileMode.Open))
            {
                var streamLength = stream.Length.ToInt();
                bytes = new byte[streamLength];
                stream.Read(bytes, 0, streamLength);
            }
            return bytes;
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
                default:
                    break;
            }
            if (position < 0 || position > Length - 1)
                throw new ApplicationException(String.Format("Invalid offset. Expected max:{0}, requested:{1}", Length, offset));
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
