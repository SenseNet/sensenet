﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryBlobProviderSelector : IBlobProviderSelector
    {
        public IBlobProvider GetProvider(long fullSize)
        {
            //TODO: [DIREF] get provider store service through the constructor
            return Providers.Instance.BlobProviders[typeof(InMemoryBlobProvider).FullName];
        }
    }

    public class InMemoryBlobProviderData
    {
        private Guid _blobId;
        public Guid BlobId
        {
            get => _blobId;
            set => _blobId = value;
        }
    }

    public class InMemoryStreamForWrite : Stream
    {
        private readonly byte[] _buffer;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get; }

        public override long Position { get; set; }

        public InMemoryStreamForWrite(byte[] buffer)
        {
            _buffer = buffer;
            Length = _buffer.Length;
        }

        public override void Flush()
        {
            // do nothing
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Array.Copy(buffer, 0, _buffer, offset, count);
        }
    }

    public class InMemoryStreamForRead : Stream
    {
        private byte[] _buffer;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length { get; }

        public override long Position { get; set; }

        public InMemoryStreamForRead( byte[] buffer)
        {
            _buffer = buffer;
            Length = _buffer.Length;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position -= offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var realCount = Math.Min(_buffer.LongLength - Position - offset, count);
            if (realCount > 0)
                Array.Copy(_buffer, Position, buffer, 0, realCount);
            Position += realCount;
            return Convert.ToInt32(realCount);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    public class InMemoryBlobProvider : IBlobProvider
    {
        private readonly Dictionary<Guid, byte[]> _blobStorage = new Dictionary<Guid, byte[]>();

        public STT.Task AllocateAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            _blobStorage.Add(id, new byte[context.Length]);

            context.BlobProviderData = new InMemoryBlobProviderData { BlobId = id };
            return STT.Task.CompletedTask;
        }

        public STT.Task WriteAsync(BlobStorageContext context, long offset, byte[] buffer, CancellationToken cancellationToken)
        {
            var data = (InMemoryBlobProviderData)context.BlobProviderData;
            var target = _blobStorage[data.BlobId];
            Array.Copy(buffer, 0, target, offset, buffer.Length);
            return STT.Task.CompletedTask;
        }

        public STT.Task DeleteAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var data = (InMemoryBlobProviderData)context.BlobProviderData;
            _blobStorage.Remove(data.BlobId);
            return STT.Task.CompletedTask;
        }

        public STT.Task ClearAsync(BlobStorageContext context, CancellationToken cancellationToken)
        {
            var data = (InMemoryBlobProviderData)context.BlobProviderData;
            _blobStorage[data.BlobId] = new byte[0];
            return STT.Task.CompletedTask;
        }

        public Stream GetStreamForRead(BlobStorageContext context)
        {
            var data = (InMemoryBlobProviderData) context.BlobProviderData;
            var buffer = _blobStorage[data.BlobId];
            return new InMemoryStreamForRead(buffer);
        }

        public Stream GetStreamForWrite(BlobStorageContext context)
        {
            var providerData = (InMemoryBlobProviderData)context.BlobProviderData;
            var buffer = new byte[context.Length];
            _blobStorage[providerData.BlobId] = buffer;
            return new InMemoryStreamForWrite(buffer);
        }

        public Stream CloneStream(BlobStorageContext context, Stream stream)
        {
            throw new NotImplementedException();
        }

        public object ParseData(string providerData)
        {
            return BlobStorageContext.DeserializeBlobProviderData<InMemoryBlobProviderData>(providerData);
        }
    }
}
