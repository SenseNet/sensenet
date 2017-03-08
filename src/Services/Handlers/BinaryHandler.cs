using System;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Services;

namespace SenseNet.Portal.Handlers
{
    /// <summary>
    /// Serves binary properties of Sense/Net content items according to the supplied query string.
    /// </summary>
    public class BinaryHandler : BinaryHandlerBase
    {
        /* ============================================================================= Public Properties */

        public static NodeHead RequestedNodeHead
        {
            get
            {
                var nodeid = RequestedNodeId;
                if (nodeid.HasValue)
                    return NodeHead.Get(nodeid.Value);

                var nodepath = RequestedNodePath;
                if (!string.IsNullOrEmpty(nodepath))
                    return NodeHead.Get(nodepath);

                return null;
            }
        }
        
        /* ============================================================================= Properties */

        private string _propertyName;
        protected override string PropertyName
        {
            get
            {
                if (_propertyName == null)
                {
                    var propertyName = HttpContext.Current.Request.QueryString["propertyname"];

                    _propertyName = string.IsNullOrEmpty(propertyName) ? null : propertyName.Replace("$", "#");
                }

                return _propertyName;
            }
            set
            {
                _propertyName = value; 
            }
        }

        private static int? RequestedNodeId
        {
            get
            {
                var nodeidStr = HttpContext.Current.Request.QueryString["nodeid"];
                if (!string.IsNullOrEmpty(nodeidStr))
                {
                    int nodeid;
                    var success = Int32.TryParse(nodeidStr, out nodeid);
                    if (success)
                        return nodeid;
                }
                return null;
            }
        }

        private static string RequestedNodePath
        {
            get
            {
                var nodePathStr = HttpContext.Current.Request.QueryString["nodepath"];
                return nodePathStr;
            }
        }

        private Node _requestedNode;

        protected override Node RequestedNode
        {
            get
            {
                if (_requestedNode != null)
                    return _requestedNode;

                var head = PortalContext.Current.BinaryHandlerRequestedNodeHead;
                if (head == null)
                    return null;

                if (!SecurityHandler.HasPermission(head, PermissionType.Open))
                {
                    // Feature: User's Avatar
                    // In case this was an attempt to download a user's avatar that the current 
                    // user does not have access to, serve the default (skin-relative) avatar.
                    if (head.GetNodeType().IsInstaceOfOrDerivedFrom("User") &&
                        string.CompareOrdinal(PropertyName, "ImageData") == 0)
                    {
                        var avatarPath = IdentityTools.GetDefaultUserAvatarPath();
                        if (string.IsNullOrEmpty(avatarPath))
                            return null;

                        // Set property to default because the default avatar image 
                        // does not have an ImageData property, only Binary.
                        PropertyName = "Binary";

                        return _requestedNode = Node.LoadNode(avatarPath);
                    }

                    return null;
                }

                if (string.IsNullOrEmpty(PortalContext.Current.VersionRequest))
                {
                    _requestedNode = Node.LoadNode(head, VersionNumber.LastFinalized);
                }
                else
                {
                    VersionNumber version;
                    if (VersionNumber.TryParse(PortalContext.Current.VersionRequest, out version))
                    {
                        var node = Node.LoadNode(head, version);
                        if (node != null && node.SavingState == ContentSavingState.Finalized)
                            _requestedNode = node;
                    }
                }

                return _requestedNode;
            }
        }

        protected override int? Width
        {
            get
            {
                var widthStr = HttpContext.Current.Request.QueryString["width"];
                if (!string.IsNullOrEmpty(widthStr))
                {
                    int width;
                    var success = Int32.TryParse(widthStr, out width);
                    if (success)
                        return width;
                }
                return null;
            }
        }

        protected override int? Height
        {
            get
            {
                var heightStr = HttpContext.Current.Request.QueryString["height"];
                if (!string.IsNullOrEmpty(heightStr))
                {
                    int height;
                    var success = Int32.TryParse(heightStr, out height);
                    if (success)
                        return height;
                }
                return null;
            }
        }

        protected override TimeSpan? MaxAge
        {
            get
            {
                var maxAgeInDaysStr = HttpContext.Current.Request.QueryString["maxAge"] as string;
                int maxAgeInDays;

                if (!string.IsNullOrEmpty(maxAgeInDaysStr) && int.TryParse(maxAgeInDaysStr, out maxAgeInDays))
                {
                    return TimeSpan.FromDays(maxAgeInDays);
                }

                return null;
            }
        }
    }
}
