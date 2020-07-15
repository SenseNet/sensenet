using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Virtualization;
using File = SenseNet.ContentRepository.File;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Wopi
{
    /// <summary>
    /// A middleware for processing WOPI requests.
    /// </summary>
    internal class WopiMiddleware
    {
        internal static bool IsReadOnlyMode { get; } = false;

        private static class WopiHeader
        {
            public static readonly string Lock = "X-WOPI-Lock";
            public static readonly string LockFailureReason = "X-WOPI-LockFailureReason";
        }

        private readonly RequestDelegate _next;
        public WopiMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            await ProcessRequestAsync(httpContext, false).ConfigureAwait(false);

            // Call next in the chain if exists
            if (_next != null)
                await _next(httpContext).ConfigureAwait(false);
        }

        internal async Task ProcessRequestAsync(HttpContext context, bool calledFromTest)
        {
            var wopiRequest = WopiRequest.Parse(context);

            // set current user based on the access token
            if (!calledFromTest)
            {
                var user = await GetCurrentUserAsync(wopiRequest, context.RequestAborted).ConfigureAwait(false);
                if (user != null)
                    User.Current = user;
            }

            var webResponse = context.Response;
            var wopiResponse = await GetResponseAsync(wopiRequest, context.RequestAborted).ConfigureAwait(false);

            // Set content type if it is known.
            if (!string.IsNullOrEmpty(wopiResponse.ContentType))
                webResponse.ContentType = wopiResponse.ContentType;

            // Set response headers.
            foreach (var item in wopiResponse.Headers)
                webResponse.Headers.Add(item.Key, item.Value);

            // Set HTTP Status code.
            webResponse.StatusCode = (int)wopiResponse.StatusCode;

            // Write binary content
            if (wopiResponse is IWopiBinaryResponse wopiBinaryResponse)
            {
                //TODO: provide custom binary field name if available
                var binaryHandler = new BinaryHandler(context, wopiBinaryResponse.File);                                
                await binaryHandler.ProcessRequestCore().ConfigureAwait(false);
                return;
            }

            // Write JSON body
            if (wopiResponse is IWopiObjectResponse)
            {
                var settings = new JsonSerializerSettings {Formatting = Formatting.Indented};
                var output = JsonConvert.SerializeObject(wopiResponse, settings);

                await webResponse.Body.WriteLimitedAsync(Encoding.UTF8.GetBytes(output), 0, output.Length)
                    .ConfigureAwait(false);
            }
        }
        internal async Task<WopiResponse> GetResponseAsync(WopiRequest wopiRequest, CancellationToken cancellationToken)
        {
            switch (wopiRequest.RequestType)
            {
                case WopiRequestType.CheckFileInfo:
                    return await ProcessCheckFileInfoRequestAsync((CheckFileInfoRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.GetLock:
                    return await ProcessGetLockRequestAsync((GetLockRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.Lock:
                    return await ProcessLockRequestAsync((LockRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.Unlock:
                    return await ProcessUnlockRequestAsync((UnlockRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.RefreshLock:
                    return await ProcessRefreshLockRequestAsync((RefreshLockRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.UnlockAndRelock:
                    return await ProcessUnlockAndRelockRequestAsync((UnlockAndRelockRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.GetFile:
                    return await ProcessGetFileRequestAsync((GetFileRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.PutFile:
                    return await ProcessPutFileRequestAsync((PutFileRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
                case WopiRequestType.PutRelativeFile:
                    return await ProcessPutRelativeFileRequestAsync((PutRelativeFileRequest)wopiRequest, cancellationToken)
                        .ConfigureAwait(false);
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

        private async Task<WopiResponse> ProcessCheckFileInfoRequestAsync(CheckFileInfoRequest wopiRequest, 
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};

            var user = User.Current;

            File file;
            using (new SystemAccount())
            {
                file = await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) as File;
                if (file == null)
                    return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            }

            // The owner have to load with original (not elevated) user
            var owner = file.Owner as IUser ?? User.Somebody;

            // Uses SystemAccount
            var userPermissions = GetUserPermissions(file, user);

            // this version has to be unique and change when the binary changes
            var version = $"{file.Version}.{file.Binary.FileId}." +
                          $"{file.VersionModificationDate:yyyy-MM-ddTHH:mm:ss.fffffffZ}";

            return new CheckFileInfoResponse
            {
                StatusCode = HttpStatusCode.OK,
                ContentType = "application/json",

                // Base properties
                BaseFileName = file.Name,
                OwnerId = GetUserId(owner),
                Size = file.Binary.Size,
                UserId = GetUserId(user),
                Version = version,

                // User metadata properties
                IsAnonymousUser = !user.IsAuthenticated,
                IsEduUser = false,
                LicenseCheckForEditIsEnabled = false,
                UserFriendlyName = user.FullName,
                UserInfo = null,

                // User permissions properties
                ReadOnly = userPermissions.ReadOnly,
                RestrictedWebViewOnly = userPermissions.RestrictedViewOnly,
                UserCanAttend = userPermissions.AttendBroadcast,
                UserCanNotWriteRelative = !userPermissions.Create,
                UserCanPresent = userPermissions.PresentBroadcast,
                UserCanRename = userPermissions.Rename,
                UserCanWrite = userPermissions.Write,

                // File URL properties
                CloseUrl = null,
                DownloadUrl = null,
                FileSharingUrl = null,
                FileUrl = null,
                FileVersionUrl = null,
                HostEditUrl = null,
                HostEmbeddedViewUrl = null,
                HostViewUrl = null,
                SignoutUrl = null,

                // Breadcrumb properties
                BreadcrumbBrandName = null,
                BreadcrumbBrandUrl = null,
                BreadcrumbDocName = null,
                BreadcrumbFolderName = null,
                BreadcrumbFolderUrl = null,

                // Other miscellaneous properties
                FileExtension = Path.GetExtension(file.Name),
                LastModifiedTime = file.ModificationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                SHA256 = null,
                UniqueContentId = version,
            };
        }

        private static readonly char[] DisabledUserIdChars = "<>\"#{}^[]`\\/".ToCharArray();
        public static readonly string AccessTokenFeatureName = "Wopi";

        private async Task<IUser> GetCurrentUserAsync(WopiRequest wopiRequest, CancellationToken cancellationToken)
        {            
            var tokenValue = wopiRequest.AccessTokenValue;
            var contentId = wopiRequest is FilesRequest fileRequest ? int.Parse(fileRequest.FileId) : 0;
            var token = await AccessTokenVault.GetTokenAsync(tokenValue, contentId, AccessTokenFeatureName, cancellationToken)
                .ConfigureAwait(false);
            if (token == null)
                throw new UnauthorizedAccessException(); // 404

            using (new SystemAccount())
            {
                if (await Node.LoadNodeAsync(token.UserId, cancellationToken).ConfigureAwait(false) is IUser user)
                {
                    // TODO: This method only sets the User.Current property in sensenet, not the
                    // main context User in Asp.Net. Check if it would be better if we changed 
                    // or modified the context user earlier in the pipeline.

                    return user;
                }
            }

            return null;
        }
        private string GetUserId(IUser user)
        {
            return DisabledUserIdChars.Aggregate(user.Name, (current, c) => current.Replace(c, '_'));
        }
        private class UserPermissions
        {
            public bool Write { get; set; }
            public bool RestrictedViewOnly { get; set; }

            public bool ReadOnly => !Write;
            public bool Rename => false;
            public bool Create { get; set; }
            public bool AttendBroadcast => !RestrictedViewOnly;
            public bool PresentBroadcast => !RestrictedViewOnly;
        }

        private UserPermissions GetUserPermissions(File file, IUser user)
        {
            using (new SystemAccount())
            {
                var entries = file.Security.GetEffectiveEntries();
                var identities = new List<int> { user.Id };

                // get all groups of the user, including Owners if necessary
                identities.AddRange(SecurityHandler.GetGroupsWithOwnership(file.Id, user));

                var allowBits = 0UL;
                var denyBits = 0UL;
                foreach (var entry in entries)
                {
                    if (identities.Contains(entry.IdentityId))
                    {
                        allowBits |= entry.AllowBits;
                        denyBits |= entry.DenyBits;
                    }
                }
                allowBits = allowBits & ~denyBits;

                if (IsReadOnlyMode)
                {
                    return new UserPermissions
                    {
                        Write = false,
                        RestrictedViewOnly = 0 == (allowBits & PermissionType.Open.Mask) &&
                                             0 != (allowBits & (PermissionType.Preview.Mask +
                                                                PermissionType.PreviewWithoutWatermark.Mask +
                                                                PermissionType.PreviewWithoutRedaction.Mask)),
                        Create = false,
                    };
                }
                return new UserPermissions
                {
                    Write = (allowBits & PermissionType.Save.Mask) > 0,
                    RestrictedViewOnly = 0 == (allowBits & PermissionType.Open.Mask) &&
                                         0 != (allowBits & (PermissionType.Preview.Mask +
                                                            PermissionType.PreviewWithoutWatermark.Mask +
                                                            PermissionType.PreviewWithoutRedaction.Mask)),
                    Create = false
                };
            }
        }

        private async Task<WopiResponse> ProcessPutRelativeFileRequestAsync(PutRelativeFileRequest wopiRequest, 
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var allowIncrementalNaming = wopiRequest.SuggestedTarget != null;
            var allowOverwrite = wopiRequest.OverwriteRelativeTarget;
            var targetName = wopiRequest.SuggestedTarget ?? wopiRequest.RelativeTarget;
            if (targetName.StartsWith("."))
                targetName = Path.GetFileNameWithoutExtension(file.Name) + targetName;

            File targetFile = null;
            if (!allowIncrementalNaming)
            {
                var targetPath = $"{file.ParentPath}/{targetName}";
                var node = await Node.LoadNodeAsync(targetPath, cancellationToken).ConfigureAwait(false);
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
                throw new NotImplementedException(); //TODO:WOPI: ProcessPutRelativeFileRequest Check lock
            }

            targetFile.Binary.FileName = targetName;
            targetFile.Binary.SetStream(wopiRequest.RequestStream);
            targetFile.Save(); //TODO:WOPI: ProcessPutRelativeFileRequest shared lock?

            var url = "__notimplemented__"; //TODO:WOPI: ProcessPutRelativeFileRequest Generate correct URL
            return new PutRelativeFileResponse
            {
                StatusCode = HttpStatusCode.NotImplemented,
                Name = targetFile.Name,
                Url = url,
            };
        }
        private async Task<WopiResponse> ProcessGetLockRequestAsync(GetLockRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};

            var existingLock = SharedLock.GetLock(file.Id, CancellationToken.None) ?? string.Empty;

            return new WopiResponse
            {
                StatusCode = HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    {WopiHeader.Lock, existingLock},
                }
            };
        }
        private async Task<WopiResponse> ProcessLockRequestAsync(LockRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id, CancellationToken.None);
            if (existingLock == null)
            {
                if (!file.Locked)
                {
                    SharedLock.Lock(file.Id, wopiRequest.Lock, CancellationToken.None);
                    return new WopiResponse { StatusCode = HttpStatusCode.OK };
                }
                return new WopiResponse
                {
                    StatusCode = HttpStatusCode.Conflict,
                    Headers = new Dictionary<string, string>
                    {
                        {WopiHeader.Lock, ""},
                        {WopiHeader.LockFailureReason, "CheckedOut"}
                    }
                };
            }
            if (existingLock != wopiRequest.Lock)
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
            SharedLock.RefreshLock(contentId, existingLock, CancellationToken.None);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private async Task<WopiResponse> ProcessUnlockRequestAsync(UnlockRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id, CancellationToken.None);
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
            if (existingLock != wopiRequest.Lock)
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
            SharedLock.Unlock(contentId, existingLock, CancellationToken.None);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private async Task<WopiResponse> ProcessRefreshLockRequestAsync(RefreshLockRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id, CancellationToken.None);
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
            if (existingLock != wopiRequest.Lock)
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
            SharedLock.RefreshLock(contentId, existingLock, CancellationToken.None);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private async Task<WopiResponse> ProcessUnlockAndRelockRequestAsync(UnlockAndRelockRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            var existingLock = SharedLock.GetLock(file.Id, CancellationToken.None);
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
            if (existingLock != wopiRequest.OldLock)
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
            SharedLock.ModifyLock(contentId, existingLock, wopiRequest.Lock, CancellationToken.None);
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }

        private async Task<WopiResponse> ProcessGetFileRequestAsync(GetFileRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse {StatusCode = HttpStatusCode.NotFound};
            if(!IsPreconditionOk(wopiRequest, file))
                return new WopiResponse { StatusCode = HttpStatusCode.PreconditionFailed };
            return new GetFileResponse
            {
                StatusCode = HttpStatusCode.OK,
                File = file,
                ContentType = file.Binary.ContentType
            };
        }
        private bool IsPreconditionOk(GetFileRequest wopiRequest, File file)
        {
            if (wopiRequest.MaxExpectedSize == null)
                return true;

            var bigLength = file.Binary.Size;
            if (bigLength > int.MaxValue)
                return false;

            var length = Convert.ToInt32(bigLength);
            return wopiRequest.MaxExpectedSize.Value >= length;
        }

        private async Task<WopiResponse> ProcessPutFileRequestAsync(PutFileRequest wopiRequest,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(wopiRequest.FileId, out var contentId))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };
            if (!(await Node.LoadNodeAsync(contentId, cancellationToken).ConfigureAwait(false) is File file))
                return new WopiResponse { StatusCode = HttpStatusCode.NotFound };

            return await ProcessPutFileRequestAsync(file, wopiRequest.Lock, wopiRequest.RequestStream, 
                    cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Method for tests</summary>
        internal static async Task<WopiResponse> ProcessPutFileRequestAsync(File file, string lockValue, Stream stream,
            CancellationToken cancellationToken)
        {
            static bool AreLocksEqual(string existing, string current)
            {
                if (existing == current)
                    return true;
                if (existing == null || current == null || current.Length < 40)
                    return false;

                // Locks are considered equal if the existing lock is a superset
                // of the one in the current request. OOS generates JSON formatted
                // locks and sometimes modifies them by adding additional properties.
                return existing.Contains(current.Trim('{', '}'));
            }

            var existingLock = SharedLock.GetLock(file.Id, CancellationToken.None);
            if (existingLock == null)
            {
                if (file.Binary.Size != 0)
                    return new WopiResponse { StatusCode = HttpStatusCode.Conflict };
            }
            if (!AreLocksEqual(existingLock, lockValue))
            {
                SnTrace.ContentOperation.WriteError("WOPI lock conflict during PUT file request. " +
                                               $"File id: {file.Id}. Request lock: {GetEscapedLock(lockValue)}");

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

            try
            {
                //TODO: find a more resource-friendly stream solution
                // We have to copy the whole stream to an intermediate storage
                // because the request stream does not support stream.Length
                // (at least not always), which is needed by the underlying
                // repository storage.
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var binaryData = file.Binary;
                binaryData.SetStream(memoryStream);

                file.Binary = binaryData;

                SaveFile(file, existingLock);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, $"Error saving file {file.Id} in WOPI middleware.");
                throw;
            }

            //TODO:WOPI Set X-WOPI-ItemVersion header if needed.
            return new WopiResponse { StatusCode = HttpStatusCode.OK };
        }
        private static string GetEscapedLock(string lockValue)
        {
            return lockValue?.Replace('{', '*').Replace('}', '*') ?? string.Empty;
        }

        private static void SaveFile(File file, string lockValue)
        {
            file.SetCachedData(WopiService.ExpectedSharedLock, lockValue);
            try
            {
                file.Save();
            }
            finally
            {
                file.ResetCachedData(WopiService.ExpectedSharedLock);
            }
        }
    }
}
