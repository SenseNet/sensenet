using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.Services.WebDav
{
    /// <summary>
    /// Wraps a Stream object and mocks it as the stream starts at a constant offset
    /// Useful when trying to save a stream to the repository but you need to skip the first n bytes
    /// </summary>
    internal class OffsetStream : Stream, IDisposable
    {
        private Stream _originalStream;
        private int _offset;

        public OffsetStream(Stream stream, int offset)
        {
            _originalStream = stream;
            _offset = offset;
            _originalStream.Position = _offset;
        }


        // =========================== methods changed
        public override long Seek(long offset, SeekOrigin origin)
        {
            offset += _offset;
            return _originalStream.Seek(offset, origin);
        }
        public override long Length
        {
            get { return _originalStream.Length - _offset; }
        }
        public override long Position
        {
            get
            {
                return _originalStream.Position - _offset;
            }
            set
            {
                _originalStream.Position = value + _offset;
            }
        }
        public override void SetLength(long value)
        {
            _originalStream.SetLength(value + _offset);
        }

        // =========================== methods that did not change
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _originalStream.BeginRead(buffer, offset, count, callback, state);
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _originalStream.BeginWrite(buffer, offset, count, callback, state);
        }
        public override bool CanRead
        {
            get { return _originalStream.CanRead; }
        }
        public override bool CanSeek
        {
            get { return _originalStream.CanSeek; }
        }
        public override bool CanTimeout
        {
            get { return _originalStream.CanTimeout; }
        }
        public override bool CanWrite
        {
            get { return _originalStream.CanWrite; }
        }
        public override void Close()
        {
            _originalStream.Close();
        }
        public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
        {
            return _originalStream.CreateObjRef(requestedType);
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            return _originalStream.EndRead(asyncResult);
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            _originalStream.EndWrite(asyncResult);
        }
        public override bool Equals(object obj)
        {
            return _originalStream.Equals(obj);
        }
        public override void Flush()
        {
            _originalStream.Flush();
        }
        public override int GetHashCode()
        {
            return _originalStream.GetHashCode();
        }
        public override object InitializeLifetimeService()
        {
            return _originalStream.InitializeLifetimeService();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _originalStream.Read(buffer, offset, count);
        }
        public override int ReadByte()
        {
            return _originalStream.ReadByte();
        }
        public override int ReadTimeout
        {
            get
            {
                return _originalStream.ReadTimeout;
            }
            set
            {
                _originalStream.ReadTimeout = value;
            }
        }
        public override string ToString()
        {
            return _originalStream.ToString();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _originalStream.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            _originalStream.WriteByte(value);
        }
        public override int WriteTimeout
        {
            get
            {
                return _originalStream.WriteTimeout;
            }
            set
            {
                _originalStream.WriteTimeout = value;
            }
        }


        // =========================== IDisposable interface
        void IDisposable.Dispose()
        {
            _originalStream.Dispose();
            base.Dispose();
        }
    }
}
