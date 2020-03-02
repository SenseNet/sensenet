using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Preview;
using File = SenseNet.ContentRepository.File;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Virtualization
{
    /// <summary>
    /// Base class for serving binary properties of Sense/Net content items.
    /// </summary>
    public class BinaryHandler
    {
        ////TODO: use binary url prefix during routing
        ///// <summary>
        ///// URL part used for accessing this handler by default
        ///// </summary>
        //public static string UrlPart => "sn-binary";

        /// <summary>
        /// Use this if the binary is an image. If not null, the image will be resized to the specified width.
        /// </summary>
        private int? Width { get; set; }

        /// <summary>
        /// Use this if the binary is an image. If not null, the image will be resized to the specified height.
        /// </summary>
        private int? Height { get; set; }

        private int? RequestedNodeId { get; set; }
        private string RequestedNodePath { get; set; }
        private NodeHead RequestedNodeHead { get; set; }

        /// <summary>
        /// The node whose binary property should be served.
        /// </summary>
        private Node RequestedNode { get; set; }

        /// <summary>
        /// Name of the binary property to be served.
        /// </summary>
        private string PropertyName { get; set; }

        /// <summary>
        /// If not null, the HTTP Max-Age header will be set to this value.
        /// </summary>
        private TimeSpan? MaxAge { get; set; }

        private readonly HttpContext _context;
        /// <summary>
        /// Creates a new instance of BinaryHandlerBase
        /// </summary>
        public BinaryHandler(HttpContext context)
        {
            _context = context;

            ParsePropertiesFromContext();
        }

        //TODO: alternative binary handler constructor: call this from routing
        /// <summary>
        /// Creates a new instance of BinaryHandlerBase
        /// </summary>
        internal BinaryHandler(Node requestedNode, string propertyName, TimeSpan? maxAge = null, int? width = null, int? height = null)
        {
            RequestedNode = requestedNode;
            PropertyName = propertyName;
            Width = width;
            Height = height;
            MaxAge = maxAge;
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        public async Task ProcessRequestCore()
        {
            if (!CheckPermissions())
            {
                _context.Response.StatusCode = 404;
                return;
            }

            var httpHeaderTools = new HttpHeaderTools(_context);
            var endResponse = HandleResponseForClientCache(httpHeaderTools);
            if (endResponse)
                return;

            //TODO: CheckExecutableType feature is not ported yet from .net framework Services
            // It was designed to handle executable files like aspx or cshtml.
            // See the Repository.ExecutableExtensions property for the full list.

            //TODO: the ImgResizeApplication feature is not ported yet from .net framework Services

            await InitializeRequestedNodeAsync();
            
            if (RequestedNode == null)
            {
                _context.Response.StatusCode = 404;
                return;
            }

            using (var binaryStream = GetConvertedStream(out var contentType, out var fileName))
            {
                if (binaryStream == null)
                    return;

                _context.Response.ContentType = contentType;
                _context.Response.Headers.Append(HeaderNames.ContentLength, binaryStream.Length.ToString());
                
                httpHeaderTools.SetContentDispositionHeader(fileName);
                httpHeaderTools.SetCacheControlHeaders(lastModified: RequestedNode.ModificationDate, maxAge: MaxAge);

                _context.Response.StatusCode = 200;
                
                binaryStream.Position = 0;

                // Let ASP.NET handle sending bytes to the client.
                await binaryStream.CopyToAsync(_context.Response.Body);
            }

            // Let the client code log file downloads
            if (RequestedNode is File file)
                File.Downloaded(file.Id);
        }

        private async Task InitializeRequestedNodeAsync()
        {
            if (RequestedNode != null || RequestedNodeHead == null)
                return;

            // TODO: Not ported feature: User's Avatar
            // If the user does not have Open permission for the requested content
            // which is a user's image than serve a default avatar image.
            // Prerequisite: port the IdentityTools.GetDefaultUserAvatarPath
            // from the old Services project.
            // if (!SecurityHandler.HasPermission(RequestedNodeHead, PermissionType.Open))
            // {
            // }

            //TODO: parse VersionRequest globally, as this value may be needed elsewhere too
            var versionRequest = _context.Request.Query["version"].FirstOrDefault();
            if (string.IsNullOrEmpty(versionRequest))
            {
                RequestedNode = await Node
                    .LoadNodeAsync(RequestedNodeHead, VersionNumber.LastFinalized, _context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else
            {
                if (VersionNumber.TryParse(versionRequest, out var version))
                {
                    var node = await Node.LoadNodeAsync(RequestedNodeHead, version, _context.RequestAborted)
                        .ConfigureAwait(false);
                    if (node != null && node.SavingState == ContentSavingState.Finalized)
                        RequestedNode = node;
                }
            }
        }
        private void ParsePropertiesFromContext()
        {
            var nodeIdStr = _context.Request.Query["nodeid"].FirstOrDefault();
            if (!string.IsNullOrEmpty(nodeIdStr) && int.TryParse(nodeIdStr, out var nodeId))
                RequestedNodeId = nodeId;

            RequestedNodePath = _context.Request.Query["nodepath"].FirstOrDefault();

            // precedence for identifying the node:
            //   1. nodeid parameter
            //   2. nodepath parameter
            //   3. request path

            RequestedNodeHead = RequestedNodeId.HasValue
                ? NodeHead.Get(RequestedNodeId.Value)
                : !string.IsNullOrEmpty(RequestedNodePath)
                    ? NodeHead.Get(RequestedNodePath)
                    : NodeHead.Get(_context.Request.Path);

            var propertyName = _context.Request.Query["propertyname"].FirstOrDefault();
            PropertyName = string.IsNullOrEmpty(propertyName) ? null : propertyName.Replace("$", "#");

            var widthStr = _context.Request.Query["width"].FirstOrDefault();
            if (!string.IsNullOrEmpty(widthStr) && int.TryParse(widthStr, out var width))
                Width = width;

            var heightStr = _context.Request.Query["height"].FirstOrDefault();
            if (!string.IsNullOrEmpty(heightStr) && int.TryParse(heightStr, out var height))
                Height = height;

            var maxAgeInDaysStr = _context.Request.Query["maxAge"].FirstOrDefault();
            if (!string.IsNullOrEmpty(maxAgeInDaysStr) && int.TryParse(maxAgeInDaysStr, out var maxAgeInDays))
                MaxAge = TimeSpan.FromDays(maxAgeInDays);
        }

        private bool HandleResponseForClientCache(HttpHeaderTools headerTools)
        {
            if (RequestedNodeHead == null)
                return false;
            
            var cacheSetting = GetCacheHeaderSetting(headerTools);
            if (!cacheSetting.HasValue) 
                return false;

            // cache header (public or private) depends on whether the content is available for anyone
            var accessibleForVisitor = SystemAccount.Execute(() =>
                SecurityHandler.HasPermission(User.Visitor, RequestedNodeHead.Id, PermissionType.Open));
            
            // set MaxAge by type or extension or a global value as a fallback
            headerTools.SetCacheControlHeaders(cacheSetting.Value,
                accessibleForVisitor ? HttpCacheability.Public : HttpCacheability.Private);

            // in case of preview images do NOT return 304, because _undetectable_ permission changes
            // (on the image or on one of its parents) may change the preview image (e.g. display redaction or not).
            if (DocumentPreviewProvider.Current?.IsPreviewOrThumbnailImage(RequestedNodeHead) ?? false) 
                return false;

            // Handle If-Modified-Since and Last-Modified headers: end the response
            // if the content has not changed since the value posted by the client.
            var endResponse = headerTools.EndResponseForClientCache(RequestedNodeHead.ModificationDate);

            return endResponse;
        }
        private bool CheckPermissions()
        {
            if (RequestedNodeHead == null)
                return false;

            using (new SystemAccount())
            {
                return SecurityHandler.HasPermission(User.LoggedInUser, RequestedNodeHead.Id, PermissionType.Open);
            }
        }
        private int? GetCacheHeaderSetting(HttpHeaderTools headerTools)
        {
            if (RequestedNodeHead == null)
                return null;

            // shortcut: deal with real files only
            var extension = Path.GetExtension(RequestedNodeHead.Path)?.ToLower().Trim(' ', '.');
            if (string.IsNullOrEmpty(extension))
                return null;

            var contentType = RequestedNodeHead.GetNodeType().Name;
            if (string.IsNullOrEmpty(contentType))
                return null;

            // PORT: there was a global binary cache setting as a fallback
            // before, but here it would not make sense because it would
            // mean caching all types of files (including Office files)
            // which is not desirable.

            // load setting based on extension and content type
            return headerTools.GetCacheHeaderSetting(RequestedNodeHead.Path, contentType, extension);
        }

        private Stream GetConvertedStream(out string contentType, out BinaryFileName fileName)
        {
            // We have to treat images differently: the image handler takes care
            // of putting redactions on the preview image if permissions require that.
            if (RequestedNode is Image img)
            {
                var parameters = new Dictionary<string, object>();
                if (Width.HasValue)
                    parameters.Add("width", Width.Value);
                if (Height.HasValue)
                    parameters.Add("height", Height.Value);

                return img.GetImageStream(PropertyName, parameters, out contentType, out fileName);
            }

            // Get the stream through our provider to let 3rd party developers serve custom data.
            var stream = DocumentBinaryProvider.Instance.GetStream(RequestedNode, PropertyName, _context,
                out contentType, out fileName);

            if (stream == null) 
                return null;

            // try to treat the binary value as an image and resize it
            if (Width.HasValue && Height.HasValue)
                return Image.CreateResizedImageFile(stream, string.Empty, Width.Value, Height.Value, 0,
                    contentType);

            return stream;
        }
    }
}
