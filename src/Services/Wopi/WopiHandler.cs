using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Wopi
{
    /// <summary>
    /// An <see cref="IHttpHandler"/> implementation to process the OData requests.
    /// </summary>
    public class WopiHandler : IHttpHandler
    {
        private static class WopiHeader
        {
            public static readonly string Lock = "X-WOPI-Lock";
            public static readonly string LockFailureReason = "X-WOPI-LockFailureReason";
    }

    /// <inheritdoc select="summary" />
    /// <remarks>Returns with false in this implementation.</remarks>
    public bool IsReusable => false;

        /// <inheritdoc />
        /// <remarks>Processes the WOPI web request.</remarks>
        public void ProcessRequest(HttpContext context)
        {
            var portalContext = (PortalContext)context.Items[PortalContext.CONTEXT_ITEM_KEY];
            var response = GetResponse(portalContext);

            throw new NotImplementedException(); //UNDONE: not implemented: ProcessRequest
        }

        internal WopiResponse GetResponse(PortalContext portalContext)
        {
            var wopiReq = portalContext.WopiRequest;
            switch (wopiReq.RequestType)
            {
                case WopiRequestType.CheckFileInfo:
                case WopiRequestType.PutRelativeFile:
                    throw new NotImplementedException(); //UNDONE: not implemented: GetResponse #3
                case WopiRequestType.GetLock:
                    return ProcessGetLockRequest((GetLockRequest)wopiReq, portalContext);
                case WopiRequestType.Lock:
                    return ProcessLockRequest((LockRequest)wopiReq, portalContext);
                case WopiRequestType.Unlock:
                    throw new NotImplementedException(); //UNDONE: not implemented: GetResponse #3
                case WopiRequestType.RefreshLock:
                    return ProcessRefreshLockRequest((RefreshLockRequest)wopiReq, portalContext);
                case WopiRequestType.UnlockAndRelock:
                case WopiRequestType.DeleteFile:
                case WopiRequestType.RenameFile:
                    throw new NotImplementedException(); //UNDONE: not implemented: GetResponse #1
                case WopiRequestType.GetFile:
                    return ProcessGetFileRequest((GetFileRequest)wopiReq, portalContext);
                case WopiRequestType.PutFile:
                case WopiRequestType.CheckContainerInfo:
                case WopiRequestType.CreateChildContainer:
                case WopiRequestType.CreateChildFile:
                case WopiRequestType.DeleteContainer:
                case WopiRequestType.EnumerateAncestors:
                case WopiRequestType.EnumerateChildren:
                case WopiRequestType.RenameContainer:
                case WopiRequestType.CheckEcosystem:
                case WopiRequestType.GetFileWopiSrc:
                case WopiRequestType.GetRootContainer:
                case WopiRequestType.Bootstrap:
                case WopiRequestType.GetNewAccessToken:
                    throw new NotImplementedException(); //UNDONE: not implemented: GetResponse #2
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private WopiResponse ProcessGetLockRequest(GetLockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse {Status = HttpStatusCode.NotFound};
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse {Status = HttpStatusCode.NotFound};

            var existingLock = SharedLock.GetLock(file.Id) ?? string.Empty;

            return new WopiResponse
            {
                Status = HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    {WopiHeader.Lock, existingLock},
                }
            };
        }

        private WopiResponse ProcessLockRequest(LockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { Status = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { Status = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                SharedLock.Lock(file.Id, wopiReq.Lock);
                return new WopiResponse { Status = HttpStatusCode.OK };
            }
            if (existingLock != wopiReq.Lock)
            {
                return new WopiResponse
                {
                    Status = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }
            SharedLock.RefreshLock(contentId, existingLock);
            return new WopiResponse { Status = HttpStatusCode.OK };
        }
        private WopiResponse ProcessRefreshLockRequest(RefreshLockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { Status = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { Status = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                return new WopiResponse
                {
                    Status = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, ""},
                        {WopiHeader.LockFailureReason, "Unlocked"}
                    }
                };
            }
            if (existingLock != wopiReq.Lock)
            {
                return new WopiResponse
                {
                    Status = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }
            SharedLock.RefreshLock(contentId, existingLock);
            return new WopiResponse { Status = HttpStatusCode.OK };
        }

        private WopiResponse ProcessGetFileRequest(GetFileRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse {Status = HttpStatusCode.NotFound};
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse {Status = HttpStatusCode.NotFound};
            if(!IsPreconditionOk(wopiReq, file))
                return new WopiResponse { Status = HttpStatusCode.PreconditionFailed };
            return new WopiResponse { Status = HttpStatusCode.OK, File = file };
        }
        private bool IsPreconditionOk(GetFileRequest wopiReq, File file)
        {
            if (wopiReq.MaxExpectedSize == null)
                return true;

            var bigLength = file.Binary.Size;
            if (bigLength > int.MaxValue)
                return false;

            var length = Convert.ToInt32(bigLength);
            return wopiReq.MaxExpectedSize.Value >= length;
        }
    }
}
