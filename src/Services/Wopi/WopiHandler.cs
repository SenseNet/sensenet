using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;
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
            public static readonly string Lock = "X-WOPI-Lock";
            public static readonly string LockFailureReason = "X-WOPI-LockFailureReason";
        }

        /// <inheritdoc select="summary" />
        /// <remarks>Returns with false in this implementation.</remarks>
        public bool IsReusable => true;

        /// <inheritdoc />
        /// <remarks>Processes the WOPI web request.</remarks>
        public void ProcessRequest(HttpContext context)
        {
            ProcessRequest(context, false);
        }
        internal void ProcessRequest(HttpContext context, bool calledFromTest)
        {
            // Get actors.
            var webResponse = context.Response;
            var portalContext = (PortalContext)context.Items[PortalContext.CONTEXT_ITEM_KEY];
            var wopiResponse = GetResponse(portalContext);

            // Set content type if it is known.
            if (!string.IsNullOrEmpty(wopiResponse.ContentType))
                webResponse.ContentType = wopiResponse.ContentType;

            // Set response headers if any (works well only in IIS evironment).
            if(!calledFromTest)
                foreach (var item in wopiResponse.Headers)
                    webResponse.Headers.Add(item.Key, item.Value);

            // Set HTTP Status code.
            webResponse.StatusCode = (int)wopiResponse.StatusCode;

            // Write binary content
            if (wopiResponse is IWopiBinaryResponse wopiBinaryResponse)
            {
                var stream = wopiBinaryResponse.GetResponseStream();
                if (!calledFromTest)
                {
                    HttpHeaderTools.SetContentDispositionHeader(wopiBinaryResponse.FileName);
                    context.Response.AppendHeader("Content-Length", stream.Length.ToString());
                }
                stream.CopyTo(context.Response.OutputStream);
                return;
            }

            // Write JSON body
            if (wopiResponse is IWopiObjectResponse)
            {
                var settings = new JsonSerializerSettings {Formatting = Formatting.Indented};
                var serializer = JsonSerializer.Create(settings);

var xx = new StringWriter(); //UNDONE: Delete this
serializer.Serialize(xx, wopiResponse);
xx.Flush();
var xxx = xx.GetStringBuilder().ToString();


                serializer.Serialize(webResponse.Output, wopiResponse);
                webResponse.Flush();
            }
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
                    return ProcessPutFileRequest((PutFileRequest)wopiReq, portalContext);
                case WopiRequestType.PutRelativeFile:
                    return ProcessPutRelativeFileRequest((PutRelativeFileRequest)wopiReq, portalContext);
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
                    return new WopiResponse {StatusCode = HttpStatusCode.NotImplemented};
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private WopiResponse ProcessCheckFileInfoRequest(CheckFileInfoRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var user = User.Current;
            var userCanWrite = file.Security.HasPermission(PermissionType.Save);

            return new CheckFileInfoResponse
            {
                StatusCode = HttpStatusCode.OK,
                ContentType = "application/json",
                
                // Base properties
                BaseFileName = file.Name,
                Size = file.Binary.Size,
                UserId = user.Name,
                Version = null, //UNDONE:!! set real property value

                // User metadata properties
                IsAnonymousUser = !user.IsAuthenticated,
                IsEduUser = false, //UNDONE:!! set real property value
                LicenseCheckForEditIsEnabled = false, //UNDONE:!! set real property value
                UserFriendlyName = user.FullName,
                UserInfo = null, //UNDONE:!! set real property value

                // User permissions properties
                ReadOnly = !userCanWrite,
                RestrictedWebViewOnly = !file.Security.HasPermission(PermissionType.OpenMinor),
                UserCanAttend = false, //UNDONE:!! set real property value
                UserCanNotWriteRelative = !file.Parent?.Security.HasPermission(PermissionType.AddNew) ?? false,
                UserCanPresent = false, //UNDONE:!! set real property value
                UserCanRename = false, //UNDONE:!! set real property value
                UserCanWrite = userCanWrite,

                // File URL properties
                CloseUrl = null, //UNDONE:!! set real property value
                DownloadUrl = null, //UNDONE:!! set real property value
                FileSharingUrl = null, //UNDONE:!! set real property value
                FileUrl = null, //UNDONE:!! set real property value
                FileVersionUrl = null, //UNDONE:!! set real property value
                HostEditUrl = null, //UNDONE:!! set real property value
                HostEmbeddedViewUrl = null, //UNDONE:!! set real property value
                HostViewUrl = null, //UNDONE:!! set real property value
                SignoutUrl = null, //UNDONE:!! set real property value

                // Breadcrumb properties
                BreadcrumbBrandName = null, //UNDONE:!! set real property value
                BreadcrumbBrandUrl = null, //UNDONE:!! set real property value
                BreadcrumbDocName = null, //UNDONE:!! set real property value
                BreadcrumbFolderName = null, //UNDONE:!! set real property value
                BreadcrumbFolderUrl = null, //UNDONE:!! set real property value

                // Other miscellaneous properties
                FileExtension = Path.GetExtension(file.Name),
                LastModifiedTime = file.ModificationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                SHA256 = null, //UNDONE:!! set real property value
                UniqueContentId = null, //UNDONE:!! set real property value
            };
        }
        private WopiResponse ProcessPutRelativeFileRequest(PutRelativeFileRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var allowIncrementalNaming = wopiReq.SuggestedTarget != null;
            var allowOverwrite = wopiReq.OverwriteRelativeTarget;
            var targetName = wopiReq.SuggestedTarget ?? wopiReq.RelativeTarget;
            if (targetName.StartsWith("."))
                targetName = Path.GetFileNameWithoutExtension(file.Name) + targetName;

            File targetFile = null;
            if (!allowIncrementalNaming)
            {
                var targetPath = $"{file.ParentPath}/{targetName}";
                var node = Node.LoadNode(targetPath);
                if (node != null)
                {
                    if (!allowOverwrite || !(node is File loadedFile))
                        return new PutRelativeFileResponse {StatusCode = HttpStatusCode.NotImplemented};
                    targetFile = loadedFile;
                }
            }

            if (targetFile == null)
            {
                targetFile = new File(file.Parent) {Name = targetName};
                targetFile.AllowIncrementalNaming = allowIncrementalNaming;
            }
            else
            {
                throw new NotImplementedException(); //UNDONE:! Check lock
            }

            targetFile.Binary.FileName = targetName;
            targetFile.Binary.SetStream(wopiReq.RequestStream);
            targetFile.Save();

            var url = "__notimplemented__"; //UNDONE:! Generate correct URL
            return new PutRelativeFileResponse
            {
                StatusCode = HttpStatusCode.OK,
                Name = targetFile.Name,
                Url = url,
            };
        }
        private WopiResponse ProcessGetLockRequest(GetLockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};

            var existingLock = SharedLock.GetLock(file.Id) ?? string.Empty;

            return new WopiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    {WopiHeader.Lock, existingLock},
                }
            };
        }
        private WopiResponse ProcessLockRequest(LockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                SharedLock.Lock(file.Id, wopiReq.Lock);
                return new WopiResponse { StatusCode = HttpStatusCode.OK };
            }
            if (existingLock != wopiReq.Lock)
            {
                return new WopiResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }
            SharedLock.RefreshLock(contentId, existingLock);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private WopiResponse ProcessUnlockRequest(UnlockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                return new WopiResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
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
                    StatusCode = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }
            SharedLock.Unlock(contentId, existingLock);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private WopiResponse ProcessRefreshLockRequest(RefreshLockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                return new WopiResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
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
                    StatusCode = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }
            SharedLock.RefreshLock(contentId, existingLock);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private WopiResponse ProcessUnlockAndRelockRequest(UnlockAndRelockRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                return new WopiResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
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
                    StatusCode = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }
            SharedLock.ModifyLock(contentId, existingLock, wopiReq.Lock);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }

        private WopiResponse ProcessGetFileRequest(GetFileRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            if(!IsPreconditionOk(wopiReq, file))
                return new WopiResponse { StatusCode = HttpStatusCode.PreconditionFailed };
            return new GetFileResponse
            {
                StatusCode = HttpStatusCode.OK,
                File = file,
                ContentType = file.Binary.ContentType
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

        private WopiResponse ProcessPutFileRequest(PutFileRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id);
            if (existingLock == null)
            {
                if (file.Binary.Size != 0)
                    return new WopiResponse { StatusCode = HttpStatusCode.Conflict };
            }
            if (existingLock != wopiReq.Lock)
            {
                return new WopiResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, existingLock},
                        {WopiHeader.LockFailureReason, "LockedByAnother"}
                    }
                };
            }

            file.Binary.SetStream(wopiReq.RequestStream);
            file.Save();
            //UNDONE:! Set X-WOPI-ItemVersion header if needed.
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
    }
}
