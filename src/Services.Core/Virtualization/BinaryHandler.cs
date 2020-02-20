using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
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

        ////UNDONE: alternative binary handler constructor: call this from routing or remove it
        ///// <summary>
        ///// Creates a new instance of BinaryHandlerBase
        ///// </summary>
        //public BinaryHandler(Node requestedNode, string propertyName, TimeSpan? maxAge = null, int? width = null, int? height = null)
        //{
        //    RequestedNode = requestedNode;
        //    PropertyName = propertyName;
        //    Width = width;
        //    Height = height;
        //    MaxAge = maxAge;
        //}
        
        /// <summary>
        /// Processes the request.
        /// </summary>
        public async Task ProcessRequestCore()
        {
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

                // Legacy: we need to Flush the headers before we start to stream the actual binary.
                _context.Response.ContentType = contentType;
                _context.Response.Headers.Append("Content-Length", binaryStream.Length.ToString());

                var httpHeaderTools = new HttpHeaderTools(_context);
                httpHeaderTools.SetContentDispositionHeader(fileName);
                httpHeaderTools.SetCacheControlHeaders(lastModified: RequestedNode.ModificationDate, maxAge: this.MaxAge);

                _context.Response.StatusCode = 200;

                //UNDONE: find Flush equivalent in .net core or remove this
                //_context.Response.Flush();

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
            if (RequestedNode == null && RequestedNodeHead != null)
            {
                if (!SecurityHandler.HasPermission(RequestedNodeHead, PermissionType.Open))
                {
                    // Feature: User's Avatar
                    // In case this was an attempt to download a user's avatar that the current 
                    // user does not have access to, serve the default (skin-relative) avatar.
                    if (RequestedNodeHead.GetNodeType().IsInstaceOfOrDerivedFrom("User") &&
                        string.CompareOrdinal(PropertyName, "ImageData") == 0)
                    {
                        //UNDONE: GetDefaultUserAvatarPath
                        var avatarPath = string.Empty; //IdentityTools.GetDefaultUserAvatarPath();
                        if (!string.IsNullOrEmpty(avatarPath))
                        {
                            // Set property to default because the default avatar image 
                            // does not have an ImageData property, only Binary.
                            PropertyName = "Binary";

                            RequestedNode = await Node.LoadNodeAsync(avatarPath, _context.RequestAborted)
                                .ConfigureAwait(false);
                        }
                    }
                }

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
