using System;
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
        //UNDONE: use binary url prefix during routing
        /// <summary>
        /// URL part used for accessing this handler by default
        /// </summary>
        public static string UrlPart => "sn-binary";

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

            // parse property values from the request

            var nodeIdStr = _context.Request.Query["nodeid"].FirstOrDefault();
            if (!string.IsNullOrEmpty(nodeIdStr) && int.TryParse(nodeIdStr, out var nodeId))
                RequestedNodeId = nodeId;

            RequestedNodePath = _context.Request.Query["nodepath"].FirstOrDefault();

            RequestedNodeHead = RequestedNodeId.HasValue
                ? NodeHead.Get(RequestedNodeId.Value)
                : !string.IsNullOrEmpty(RequestedNodePath)
                    ? NodeHead.Get(RequestedNodePath)
                    : null;

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

            if (RequestedNodeHead != null)
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

                            RequestedNode = Node.LoadNode(avatarPath);
                        }
                    }
                }

                //TODO: parse VersionRequest globally, as this value may be needed elsewhere too
                var versionRequest = _context.Request.Query["version"].FirstOrDefault();
                if (string.IsNullOrEmpty(versionRequest))
                {
                    RequestedNode = Node.LoadNode(RequestedNodeHead, VersionNumber.LastFinalized);
                }
                else
                {
                    if (VersionNumber.TryParse(versionRequest, out var version))
                    {
                        var node = Node.LoadNode(RequestedNodeHead, version);
                        if (node != null && node.SavingState == ContentSavingState.Finalized)
                            RequestedNode = node;
                    }
                }
            }
        }

        //UNDONE: alternative binary handler constructor: call this from routing or remove it
        /// <summary>
        /// Creates a new instance of BinaryHandlerBase
        /// </summary>
        public BinaryHandler(Node requestedNode, string propertyName, TimeSpan? maxAge = null, int? width = null, int? height = null)
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
            //UNDONE: integrate this to the pipeline
            var requestedNode = RequestedNode;
            var propertyName = PropertyName;

            if (string.IsNullOrEmpty(propertyName) || requestedNode == null)
            {
                _context.Response.StatusCode = 404;
                return;
            }

            // Get the stream through our provider to let 3rd party developers serve custom data
            using (var binaryStream = DocumentBinaryProvider.Instance.GetStream(requestedNode, propertyName, _context, 
                out var contentType, out var fileName))
            {
                if (binaryStream == null)
                    return;

                using (var resizedStream = GetResizedOrOriginalStream(binaryStream, contentType))
                {
                    // We need to Flush the headers before we start to stream the actual binary.
                    _context.Response.ContentType = contentType;
                    _context.Response.Headers.Append("Content-Length", resizedStream.Length.ToString());

                    var httpHeaderTools = new HttpHeaderTools(_context);
                    httpHeaderTools.SetContentDispositionHeader(fileName);
                    httpHeaderTools.SetCacheControlHeaders(lastModified: requestedNode.ModificationDate, maxAge: this.MaxAge);

                    _context.Response.StatusCode = 200;

                    //UNDONE: find Flush equivalent in .net core or remove this
                    //_context.Response.Flush();

                    resizedStream.Position = 0;

                    // Let ASP.NET handle sending bytes to the client.
                    await resizedStream.CopyToAsync(_context.Response.Body);
                }
            }

            // Let the client code log file downloads
            if (requestedNode is File file)
                File.Downloaded(file.Id);
        }

        private Stream GetResizedOrOriginalStream(Stream binaryStream, string contentType)
        {
            //UNDONE: why don't we check image content type here
            // if this is an image and we need to resize it
            if (Width.HasValue && Height.HasValue)
                return Image.CreateResizedImageFile(binaryStream, string.Empty, Width.Value, Height.Value, 0, contentType);

            return binaryStream;
        }
    }
}
