using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Wopi
{
    internal abstract class FilesRequest : WopiRequest
    {
        public string FileId { get; }

        protected FilesRequest(WopiRequestType requestType, string fileId) : base(requestType)
        {
            FileId = fileId;
        }
    }

    internal abstract class UploadRequest : FilesRequest
    {
        public Stream RequestStream { get; }

        protected UploadRequest(WopiRequestType requestType, string fileId, Stream requestStream) : base(requestType, fileId)
        {
            RequestStream = requestStream;
        }
    }


    internal class CheckFileInfoRequest : FilesRequest
    {
        public string SessionContext { get; }

        public CheckFileInfoRequest(string fileId, string sessionContext) : base(WopiRequestType.CheckFileInfo, fileId)
        {
            SessionContext = sessionContext;
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

    internal class UnlockRequest : FilesRequest
    {
        public string Lock { get; }

        internal UnlockRequest(string fileId, string @lock) : base(WopiRequestType.Unlock, fileId)
        {
            Lock = @lock;
        }
    }

    internal class UnlockAndRelockRequest : FilesRequest
    {
        public string Lock { get; }
        public string OldLock { get; }

        internal UnlockAndRelockRequest(string fileId, string @lock, string oldLock) : base(
            WopiRequestType.UnlockAndRelock, fileId)
        {
            Lock = @lock;
            OldLock = oldLock;
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

    internal class PutFileRequest : UploadRequest
    {
        public string Lock { get; }

        public PutFileRequest(string fileId, string @lock, Stream requestStream) : base(WopiRequestType.PutFile, fileId, requestStream)
        {
            Lock = @lock;
        }
    }

    internal class PutRelativeFileRequest : UploadRequest
    {
        public string SuggestedTarget { get; }
        public string RelativeTarget { get; }
        public bool OverwriteRelativeTarget { get; }
        public int Size { get; }
        public string FileConversion { get; }

        public PutRelativeFileRequest(string fileId, string suggestedTarget, string relativeTarget,
            bool overwriteRelativeTarget, int size, string fileConversion, Stream requestStream)
            : base(WopiRequestType.PutRelativeFile, fileId, requestStream)
        {
            SuggestedTarget = suggestedTarget;
            RelativeTarget = relativeTarget;
            OverwriteRelativeTarget = overwriteRelativeTarget;
            Size = size;
            FileConversion = fileConversion;
        }
    }

    internal class DeleteFileRequest : FilesRequest
    {
        public DeleteFileRequest(string fileId) : base(WopiRequestType.DeleteFile, fileId)
        {
        }
    }

    internal class RenameFileRequest : FilesRequest
    {
        public string Lock { get; }
        public string RequestedName { get; }

        internal RenameFileRequest(string fileId, string @lock, string requestedName)
            : base(WopiRequestType.RenameFile, fileId)
        {
            Lock = @lock;
            RequestedName = requestedName;
        }
    }

    internal class BadRequest : WopiRequest
    {
        public HttpStatusCode StatusCode { get; }
        public Exception Exception { get; }
        internal BadRequest(Exception e) : base(WopiRequestType.NotDefined)
        {
            Exception = e;
            switch (e)
            {
                case InvalidWopiRequestException we:
                    StatusCode = we.StatusCode;
                    break;
                case SnNotSupportedException _:
                case NotSupportedException _:
                    StatusCode = HttpStatusCode.NotImplemented;
                    break;
                default:
                    StatusCode = HttpStatusCode.InternalServerError;
                    break;
            }
        }
    }
}
