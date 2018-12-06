using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Portal.OData;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Wopi
{
    public enum WopiRequestType
    {
        CheckFileInfo,
        PutRelativeFile,
        Lock,
        Unlock,
        RefreshLock,
        UnlockAndRelock,
        DeleteFile,
        RenameFile,
        GetFile,
        PutFile,
        CheckContainerInfo,
        CreateChildContainer,
        CreateChildFile,
        DeleteContainer,
        EnumerateAncestors,
        EnumerateChildren,
        RenameContainer,
        CheckEcosystem,
        GetFileWopiSrc,
        GetRootContainer,
        Bootstrap,
        GetNewAccessToken
    }
    public abstract class WopiRequest
    {
        public WopiRequestType RequestType { get; }

        protected WopiRequest(WopiRequestType requestType)
        {
            RequestType = requestType;
        }

        /* ============================================================================== static part */

        private static readonly string GET = "GET";
        private static readonly string POST = "POST";

        private static class WopiHeader
        {
            public static readonly string Override = "X-WOPI-Override";

            public static readonly string EcosystemOperation = "X-WOPI-EcosystemOperation";
            public static readonly string FileConversion = "X-WOPI-FileConversion";
            public static readonly string FileExtensionFilterList = "X-WOPI-FileExtensionFilterList";
            public static readonly string HostNativeFileName = "X-WOPI-HostNativeFileName";
            public static readonly string Lock = "X-WOPI-Lock";
            public static readonly string MaxExpectedSize = "X-WOPI-MaxExpectedSize";
            public static readonly string OldLock = "X-WOPI-OldLock";
            public static readonly string OverwriteRelativeTarget = "X-WOPI-OverwriteRelativeTarget";
            public static readonly string RelativeTarget = "X-WOPI-RelativeTarget";
            public static readonly string RequestedName = "X-WOPI-RequestedName";
            public static readonly string SessionContext = "X-WOPI-SessionContext";
            public static readonly string Size = "X-WOPI-Size";
            public static readonly string SuggestedTarget = "X-WOPI-SuggestedTarget";
            public static readonly string WopiSrc = "X-WOPI-WopiSrc";
        }

        public static WopiRequest Parse(string wopiPath, PortalContext portalContext)
        {
            if (!portalContext.IsWopiRequest)
                throw new InvalidOperationException("The Request is not a WOPI request.");

            var segments = wopiPath.ToLowerInvariant().Split('/');

            var ownerContext = portalContext.OwnerHttpContext;
            var webRequest = ownerContext.Request;
            var headers = webRequest.Headers;
            var httpMethod = webRequest.HttpMethod;
            var xWopiOverride = headers[WopiHeader.Override];

            return Parse(segments, httpMethod, xWopiOverride, headers);
        }
        private static WopiRequest Parse(string[] segments, string httpMethod, string xWopiOverride, NameValueCollection headers)
        {
            string[] rest;
            switch (segments[0])
            {
                default:
                    throw new InvalidWopiRequestException("Unknown first segment: " + segments[0]); //UNDONE: more informative message
                case "wopibootstrapper":
                    return ParseWopiBootstrapper(httpMethod, headers);
                case "wopi":
                    string action;
                    switch (segments[1])
                    {
                        default:
                            throw new InvalidWopiRequestException("Unknown second segment: " + segments[1]); //UNDONE: more informative message
                        case "files":
                            var fileId = GetIdAndAction(segments, out action, out rest);
                            return ParseFiles(httpMethod, fileId, action, xWopiOverride, headers);
                        case "containers":
                            var containerId = GetIdAndAction(segments, out action, out rest);
                            return ParseContainers(httpMethod, containerId, action, xWopiOverride, headers);
                        case "ecosystem":
                            rest = GetSegments(segments, 2);
                            return ParseEcosystem(httpMethod, headers);
                    }
            }
        }



        private static WopiRequest ParseFiles(string httpMethod, string fileId, string action, string xWopiOverride, NameValueCollection headers)
        {
            switch (action)
            {
                default:
                    throw new InvalidWopiRequestException("Unknown 4th segment: " + action); //UNDONE: more informative message
                case "ancestry":
                    return ParseEnumerateAncestors(httpMethod, fileId, headers);
                case "contents":
                    if (xWopiOverride == "PUT")
                        return ParsePutFile(httpMethod, fileId, headers);
                    return ParseGetFile(httpMethod, fileId, headers);
                case null:
                    switch (xWopiOverride)
                    {
                        default:
                            throw new InvalidWopiRequestException($"Unknown {WopiHeader.Override} header: " + xWopiOverride); //UNDONE: more informative message
                        case "PUT_RELATIVE":
                            return ParsePutRelativeFile(httpMethod, fileId, headers);
                        case "LOCK":
                            if(headers[WopiHeader.OldLock] != null)
                                return ParseUnlockAndRelock(httpMethod, fileId, headers);
                            return ParseLock(httpMethod, fileId, headers);
                        case "UNLOCK":
                            return ParseUnlock(httpMethod, fileId, headers);
                        case "REFRESH_LOCK":
                            return ParseRefreshLock(httpMethod, fileId, headers);
                        case "DELETE":
                            return ParseDeleteFile(httpMethod, fileId, headers);
                        case "RENAME_FILE":
                            return ParseRenameFile(httpMethod, fileId, headers);
                    }
            }
        }

        private static WopiRequest ParseEnumerateAncestors(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseEnumerateAncestors
        }
        private static WopiRequest ParsePutFile(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParsePutFile
        }
        private static WopiRequest ParseGetFile(string httpMethod, string fileId, NameValueCollection headers)
        {
            if (httpMethod != GET)
                throw new InvalidWopiRequestException("The FileRequest need to be HTTP_GET"); //UNDONE: more informative message
            var maxExpectedSize = GetIntOrNullFromHeader(headers, WopiHeader.MaxExpectedSize, true);
            return new GetFileRequest(fileId, maxExpectedSize);
        }

        private static WopiRequest ParsePutRelativeFile(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParsePutRelativeFile
        }
        private static WopiRequest ParseLock(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseLock
        }
        private static WopiRequest ParseUnlockAndRelock(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseUnlockAndRelock
        }
        private static WopiRequest ParseUnlock(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseUnlock
        }
        private static WopiRequest ParseRefreshLock(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseRefreshLock
        }
        private static WopiRequest ParseDeleteFile(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseDeleteFile
        }
        private static WopiRequest ParseRenameFile(string httpMethod, string fileId, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseRenameFile
        }


        private static WopiRequest ParseContainers(string httpMethod, string containerId, string action, string xWopiOverride, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseContainers
        }
        private static WopiRequest ParseEcosystem(string httpMethod, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseEcosystem
        }
        private static WopiRequest ParseWopiBootstrapper(string httpMethod, NameValueCollection headers)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ParseWopiBootstrapper
        }


        /* ----------------------------------------------------------------------------------------- */

        private static string GetIdAndAction(string[] segments, out string action, out string[] rest)
        {
            string id = null;
            action = null;
            rest = new string[0];
            if (segments.Length > 2)
                id = segments[2];
            if (segments.Length > 3)
                action = segments[3];
            if (segments.Length > 4)
                rest = GetSegments(segments, 3);
            return id;
        }
        private static string[] GetSegments(string[] segments, int startIndex)
        {
            return segments.Skip(startIndex).ToArray();
        }

        private static int GetIntFromHeader(NameValueCollection headers, string name, bool throwOnError)
        {
            var value = headers[name];
            if (string.IsNullOrEmpty(value))
                return 0;
            if (int.TryParse(value, out var intValue))
                return intValue;
            if (!throwOnError)
                return 0;
            throw new InvalidWopiRequestException($"Invalid '{name}': {value}"); //UNDONE: more informative message
        }
        private static int? GetIntOrNullFromHeader(NameValueCollection headers, string name, bool throwOnError)
        {
            var value = headers[name];
            if (string.IsNullOrEmpty(value))
                return null;
            if (int.TryParse(value, out var intValue))
                return intValue;
            if (!throwOnError)
                return null;
            throw new InvalidWopiRequestException($"Invalid '{name}': {value}"); //UNDONE: more informative message
        }

    }
}
