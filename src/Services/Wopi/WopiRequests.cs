using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Services.Wopi
{
    internal abstract class FilesRequest : WopiRequest
    {
        public string FileId { get; }
        protected FilesRequest(WopiRequestType requestType, string fileId) : base(requestType)
        {
            FileId = fileId;
        }
    }

    internal class GetLockRequest : FilesRequest
    {
        internal GetLockRequest(string fileId) : base(WopiRequestType.GetLock, fileId)
        {
        }
    }
    internal class LockRequest : FilesRequest
    {
        public string Lock { get; }
        internal LockRequest(string fileId, string @lock) : base(WopiRequestType.Lock, fileId)
        {
            Lock = @lock;
        }
    }
    internal class RefreshLockRequest : FilesRequest
    {
        public string Lock { get; }
        internal RefreshLockRequest(string fileId, string @lock) : base(WopiRequestType.RefreshLock, fileId)
        {
            Lock = @lock;
        }
    }

    internal class GetFileRequest : FilesRequest
    {
        public int? MaxExpectedSize { get; }
        public GetFileRequest(string fileId, int? maxExpectedSize) : base(WopiRequestType.GetFile, fileId)
        {
            MaxExpectedSize = maxExpectedSize;
        }
    }
    internal class PutFileRequest : FilesRequest
    {
        public string Lock { get; }
        public PutFileRequest(string fileId, string @lock) : base(WopiRequestType.PutFile, fileId)
        {
            Lock = @lock;
        }
    }
}
