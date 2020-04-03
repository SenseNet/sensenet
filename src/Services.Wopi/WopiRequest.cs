﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Services.Wopi
{
    public enum WopiRequestType
    {
        NotDefined,
        CheckFileInfo,
        PutRelativeFile,
        GetLock,
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
    internal abstract class WopiRequest
    {
        public string AccessTokenValue { get; internal set; }

        internal WopiRequestType RequestType { get; }

        protected internal WopiRequest(WopiRequestType requestType)
        {
            RequestType = requestType;
        }

        /* ============================================================================== static part */

        public static readonly string AccessTokenParamName = "access_token";

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

        public static WopiRequest Parse(HttpContext context)
        {
            var segments = context.Request.Path.Value.ToLowerInvariant()
                .Split(new [] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var webRequest = context.Request;
            var headers = webRequest.Headers;
            var httpMethod = webRequest.Method;
            var xWopiOverride = headers[WopiHeader.Override].FirstOrDefault();
            var requestStream = webRequest.Body;
            var accessToken = webRequest.Query[AccessTokenParamName].FirstOrDefault();

            WopiRequest request;
            try
            {
                request = Parse(segments, httpMethod, xWopiOverride, headers, requestStream);
            }
            catch (Exception e)
            {
                request = new BadRequest(e);
            }

            request.AccessTokenValue = accessToken;
            return request;
        }
        private static WopiRequest Parse(string[] segments, string httpMethod, string xWopiOverride, 
            IHeaderDictionary headers, Stream requestStream)
        {
            string[] rest;
            switch (segments[0])
            {
                default:
                    throw new InvalidWopiRequestException(HttpStatusCode.BadRequest,
                        "Unknown first segment: " + segments[0]);
                case "wopibootstrapper":
                    return ParseWopiBootstrapper(httpMethod, headers);
                case "wopi":
                    string action;
                    switch (segments[1])
                    {
                        default:
                            throw new InvalidWopiRequestException(HttpStatusCode.BadRequest,
                                "Unknown second segment: " + segments[1]);
                        case "files":
                            var fileId = GetIdAndAction(segments, out action, out rest);
                            return ParseFiles(httpMethod, fileId, action, xWopiOverride, headers, requestStream);
                        case "containers":
                            var containerId = GetIdAndAction(segments, out action, out rest);
                            return ParseContainers(httpMethod, containerId, action, xWopiOverride, headers);
                        case "ecosystem":
                            rest = GetSegments(segments, 2);
                            return ParseEcosystem(httpMethod, headers);
                    }
            }
        }

        private static WopiRequest ParseFiles(string httpMethod, string fileId, string action, string xWopiOverride,
            IHeaderDictionary headers, Stream requestStream)
        {
            switch (action)
            {
                default:
                    throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, 
                        "Unknown 4th segment: " + action);
                case "ancestry":
                    return ParseEnumerateAncestors(httpMethod, fileId, headers);
                case "contents":
                    if (xWopiOverride == "PUT")
                        return ParsePutFile(httpMethod, fileId, headers, requestStream);
                    return ParseGetFile(httpMethod, fileId, headers);
                case null:
                    switch (xWopiOverride)
                    {
                        default:
                            throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, 
                                $"Unknown {WopiHeader.Override} header: " + xWopiOverride);
                        case "PUT_RELATIVE":
                            return ParsePutRelativeFile(httpMethod, fileId, headers, requestStream);
                        case "GET_LOCK":
                            return ParseGetLock(httpMethod, fileId, headers);
                        case "LOCK":
                            if (headers[WopiHeader.OldLock].FirstOrDefault() != null)
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
                        case null:
                            return ParseCheckFileInfo(httpMethod, fileId, headers);
                    }
            }
        }

        private static WopiRequest ParseCheckFileInfo(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Get.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_GET");
            var sessionContext = headers[WopiHeader.SessionContext].FirstOrDefault();
            return new CheckFileInfoRequest(fileId, sessionContext);
        }

        private static WopiRequest ParseEnumerateAncestors(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            throw new SnNotSupportedException();
        }
        private static WopiRequest ParsePutFile(string httpMethod, string fileId, IHeaderDictionary headers, Stream requestStream)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            var @lock = headers[WopiHeader.Lock].FirstOrDefault();
            return new PutFileRequest(fileId, @lock, requestStream);
        }
        private static WopiRequest ParseGetFile(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Get.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_GET");
            var maxExpectedSize = GetIntOrNullFromHeader(headers, WopiHeader.MaxExpectedSize, true);
            return new GetFileRequest(fileId, maxExpectedSize);
        }

        private static WopiRequest ParsePutRelativeFile(string httpMethod, string fileId, IHeaderDictionary headers, Stream requestStream)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            var suggestedTarget = headers[WopiHeader.SuggestedTarget].FirstOrDefault();
            var relativeTarget = headers[WopiHeader.RelativeTarget].FirstOrDefault();
            if(suggestedTarget != null && relativeTarget != null)
                throw new InvalidWopiRequestException(HttpStatusCode.BadRequest,
                    "The 'X-WOPI-SuggestedTarget' and 'X-WOPI-RelativeTarget' headers are mutually exclusive.");
            var overwriteRelativeTarget = GetBoolFromHeader(headers, WopiHeader.OverwriteRelativeTarget, false);
            var size = GetIntFromHeader(headers, WopiHeader.Size, false);
            var fileConversion = headers[WopiHeader.FileConversion].FirstOrDefault();

            return new PutRelativeFileRequest(fileId,
                suggestedTarget, relativeTarget, overwriteRelativeTarget, size, fileConversion, requestStream);
        }
        private static WopiRequest ParseGetLock(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            return new GetLockRequest(fileId);
        }
        private static WopiRequest ParseLock(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            var @lock = headers[WopiHeader.Lock].FirstOrDefault();
            if(@lock == null)
                throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Missing {WopiHeader.Lock} header.");
            return new LockRequest(fileId, @lock);
        }
        private static WopiRequest ParseUnlockAndRelock(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            var @lock = headers[WopiHeader.Lock].FirstOrDefault();
            if (@lock == null)
                throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Missing {WopiHeader.Lock} header.");
            var oldLock = headers[WopiHeader.OldLock].FirstOrDefault();
            return new UnlockAndRelockRequest(fileId, @lock, oldLock);
        }
        private static WopiRequest ParseUnlock(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            var @lock = headers[WopiHeader.Lock].FirstOrDefault();
            if (@lock == null)
                throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Missing {WopiHeader.Lock} header.");
            return new UnlockRequest(fileId, @lock);
        }
        private static WopiRequest ParseRefreshLock(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            if (httpMethod != HttpMethod.Post.Method)
                throw new InvalidWopiRequestException(HttpStatusCode.MethodNotAllowed, "The request need to be HTTP_POST");
            var @lock = headers[WopiHeader.Lock].FirstOrDefault();
            if (@lock == null)
                throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Missing {WopiHeader.Lock} header.");
            return new RefreshLockRequest(fileId, @lock);
        }
        private static WopiRequest ParseDeleteFile(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            throw new SnNotSupportedException();
        }
        private static WopiRequest ParseRenameFile(string httpMethod, string fileId, IHeaderDictionary headers)
        {
            throw new SnNotSupportedException();
        }
        
        private static WopiRequest ParseContainers(string httpMethod, string containerId, string action, string xWopiOverride, IHeaderDictionary headers)
        {
            throw new SnNotSupportedException();
        }
        private static WopiRequest ParseEcosystem(string httpMethod, IHeaderDictionary headers)
        {
            throw new SnNotSupportedException();
        }
        private static WopiRequest ParseWopiBootstrapper(string httpMethod, IHeaderDictionary headers)
        {
            throw new SnNotSupportedException();
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

        private static int GetIntFromHeader(IHeaderDictionary headers, string name, bool throwOnError)
        {
            var value = headers[name].FirstOrDefault();
            if (string.IsNullOrEmpty(value))
                return 0;
            if (int.TryParse(value, out var intValue))
                return intValue;
            if (!throwOnError)
                return 0;
            throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Invalid '{name}': {value}");
        }
        private static int? GetIntOrNullFromHeader(IHeaderDictionary headers, string name, bool throwOnError)
        {
            var value = headers[name].FirstOrDefault();
            if (string.IsNullOrEmpty(value))
                return null;
            if (int.TryParse(value, out var intValue))
                return intValue;
            if (!throwOnError)
                return null;
            throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Invalid '{name}': {value}");
        }
        private static bool GetBoolFromHeader(IHeaderDictionary headers, string name, bool throwOnError)
        {
            var value = headers[name].FirstOrDefault();
            if (string.IsNullOrEmpty(value))
                return false;
            if (bool.TryParse(value, out var boolValue))
                return boolValue;
            if (!throwOnError)
                return false;
            throw new InvalidWopiRequestException(HttpStatusCode.BadRequest, $"Invalid '{name}': {value}");
        }
    }
}
