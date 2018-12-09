using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Wopi
{
    /// <summary>
    /// An <see cref="IHttpHandler"/> implementation to process the OData requests.
    /// </summary>
    public class WopiHandler : IHttpHandler
    {
        private static class WopiHeader
        {
            public static readonly string ContentType = "ContentType";

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
                    return ProcessCheckFileInfoRequest((CheckFileInfoRequest)wopiReq, portalContext);
                case WopiRequestType.GetLock:
                    return ProcessGetLockRequest((GetLockRequest)wopiReq, portalContext);
                case WopiRequestType.Lock:
                    return ProcessLockRequest((LockRequest)wopiReq, portalContext);
                case WopiRequestType.Unlock:
                    return ProcessUnlockRequest((UnlockRequest)wopiReq, portalContext);
                case WopiRequestType.RefreshLock:
                    return ProcessRefreshLockRequest((RefreshLockRequest)wopiReq, portalContext);
                case WopiRequestType.UnlockAndRelock:
                    return ProcessUnlockAndRelockRequest((UnlockAndRelockRequest)wopiReq, portalContext);
                case WopiRequestType.GetFile:
                    return ProcessGetFileRequest((GetFileRequest)wopiReq, portalContext);
                case WopiRequestType.PutFile:
                case WopiRequestType.PutRelativeFile:
                case WopiRequestType.DeleteFile:
                case WopiRequestType.RenameFile:
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

        private WopiResponse ProcessCheckFileInfoRequest(CheckFileInfoRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { Status = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { Status = HttpStatusCode.NotFound };

            var user = User.Current;
            var userCanWrite = file.Security.HasPermission(PermissionType.Save);

            return new CheckFileInfoResponse
            {
                Status = HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    {WopiHeader.ContentType, "application/json"},
                },

                // Base properties
                BaseFileName = file.Name,
                Size = file.Binary.Size,
                UserId = user.Name,
                Version = null, //UNDONE: set real property value

                // User metadata properties
                IsAnonymousUser = !user.IsAuthenticated,
                IsEduUser = false, //UNDONE: set real property value
                LicenseCheckForEditIsEnabled = false, //UNDONE: set real property value
                UserFriendlyName = user.FullName,
                UserInfo = null, //UNDONE: set real property value

                // User permissions properties
                ReadOnly = !userCanWrite,
                RestrictedWebViewOnly = !file.Security.HasPermission(PermissionType.OpenMinor),
                UserCanAttend = false, //UNDONE: set real property value
                UserCanNotWriteRelative = !file.Parent?.Security.HasPermission(PermissionType.AddNew) ?? false,
                UserCanPresent = false, //UNDONE: set real property value
                UserCanRename = false, //UNDONE: set real property value
                UserCanWrite = userCanWrite,

                // File URL properties
                CloseUrl = null, //UNDONE: set real property value
                DownloadUrl = null, //UNDONE: set real property value
                FileSharingUrl = null, //UNDONE: set real property value
                FileUrl = null, //UNDONE: set real property value
                FileVersionUrl = null, //UNDONE: set real property value
                HostEditUrl = null, //UNDONE: set real property value
                HostEmbeddedViewUrl = null, //UNDONE: set real property value
                HostViewUrl = null, //UNDONE: set real property value
                SignoutUrl = null, //UNDONE: set real property value

                // Breadcrumb properties
                BreadcrumbBrandName = null, //UNDONE: set real property value
                BreadcrumbBrandUrl = null, //UNDONE: set real property value
                BreadcrumbDocName = null, //UNDONE: set real property value
                BreadcrumbFolderName = null, //UNDONE: set real property value
                BreadcrumbFolderUrl = null, //UNDONE: set real property value

                // Other miscellaneous properties
                FileExtension = Path.GetExtension(file.Name),
                LastModifiedTime = file.ModificationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                SHA256 = null, //UNDONE: set real property value
                UniqueContentId = null, //UNDONE: set real property value
            };
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
        private WopiResponse ProcessUnlockRequest(UnlockRequest wopiReq, PortalContext portalContext)
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
                        {WopiHeader.Lock, string.Empty},
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
            SharedLock.Unlock(contentId, existingLock);
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
                        {WopiHeader.Lock, string.Empty},
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
        private WopiResponse ProcessUnlockAndRelockRequest(UnlockAndRelockRequest wopiReq, PortalContext portalContext)
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
                        {WopiHeader.Lock, string.Empty},
                        {WopiHeader.LockFailureReason, "Unlocked"}
                    }
                };
            }
            if (existingLock != wopiReq.OldLock)
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
            SharedLock.ModifyLock(contentId, existingLock, wopiReq.Lock);
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
            return new GetFileResponse
            {
                Status = HttpStatusCode.OK,
                File = file,
                Headers = new Dictionary<string, string>
                {
                    {WopiHeader.ContentType,  file.Binary.ContentType},
                }
            };
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
