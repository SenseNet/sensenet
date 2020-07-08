using System;
using System.Linq;
using SenseNet.ApplicationModel;

// ReSharper disable StringLiteralTypo

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Helper class containing OData operations for managing content items.
    /// Do not use it directly from your code, use the appropriate operation on the Content class instead.
    /// </summary>
    public static class ContentOperations
    {
        [ODataAction(Icon = "approve", Description = "$Action,Approve", DisplayName = "$Action,Approve-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save, N.P.Approve)]
        [Scenario(N.S.ListItem, N.S.ExploreActions, N.S.SimpleApprovableListItem, N.S.ContextMenu)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        public static Content Approve(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't approve content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.Approve();
            return content;
        }

        [ODataAction(Icon = "checkin", Description = "$Action,CheckIn", DisplayName = "$Action,CheckIn-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        [Scenario(N.S.ListItem, N.S.ExploreActions, N.S.SimpleApprovableListItem, N.S.ContextMenu)]
        public static Content CheckIn(Content content, string checkInComments = null)
        {
            checkInComments ??= string.Empty;

            if (string.IsNullOrEmpty(checkInComments) && content.CheckInCommentsMode == CheckInCommentsMode.Compulsory)
                throw new Exception($"Can't check in content '{content.Path}' without checkin comments " +
                                    "because its CheckInCommentsMode is set to CheckInCommentsMode.Compulsory.");
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't check in content '{content.Path}' because its " +
                                    "content handler is not a GenericContent. It needs to inherit " +
                                    "from GenericContent for collaboration feature support.");

            content["CheckInComments"] = checkInComments;
            content.CheckIn();

            return content;
        }

        [ODataAction(Icon = "checkout", Description = "$Action,CheckOut", DisplayName = "$Action,CheckOut-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save)]
        [Scenario(N.S.ListItem, N.S.ExploreActions, N.S.ContextMenu)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        public static Content CheckOut(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't check out content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.CheckOut();
            return content;
        }

        [ODataAction(Icon = "publish", Description = "$Action,Publish", DisplayName = "$Action,Publish-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save, N.P.Publish)]
        [Scenario(N.S.ListItem, N.S.ExploreActions, N.S.SimpleApprovableListItem, N.S.ContextMenu)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        public static Content Publish(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't publish content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.Publish();
            return content;
        }

        [ODataAction(Description = "$Action,Reject", DisplayName = "$Action,Reject-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        public static Content Reject(Content content, string rejectReason = null)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't reject content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");

            rejectReason = rejectReason ?? string.Empty;

            content["RejectReason"] = rejectReason;
            content.Reject();

            return content;
        }

        [ODataAction(Icon = "undocheckout", Description = "$Action,UndoCheckOut", DisplayName = "$Action,UndoCheckOut-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save)]
        [Scenario(N.S.ListItem, N.S.ExploreActions, N.S.ContextMenu)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        public static Content UndoCheckOut(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't undo check out on content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to inherit " +
                                    "from GenericContent for collaboration feature support.");
            content.UndoCheckOut();
            return content;
        }

        [ODataAction(Icon = "undocheckout", Description = "$Action,ForceUndoCheckOut", DisplayName = "$Action,ForceUndoCheckout-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save, N.P.ForceCheckin)]
        [Scenario(N.S.ListItem, N.S.ExploreActions, N.S.ContextMenu)]
        [RequiredPolicies(N.Pol.VersioningAndApproval)]
        public static Content ForceUndoCheckOut(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't force undo check out on content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.ForceUndoCheckOut();
            return content;
        }

        [ODataAction(Icon = "restoreversion", Description = "$Action,RestoreVersion", DisplayName = "$Action,RestoreVersion-DisplayName")]
        [AllowedRoles(N.R.Everyone)]
        [RequiredPermissions(N.P.Save, N.P.RecallOldVersion)]
        public static Content RestoreVersion(Content content, string version)
        {
            // Perform checks
            if (version == null)
                throw new Exception("Can't restore old version because the version parameter is empty.");
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't restore old version of content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");

            // Append 'V' if it is not there
            var versionText = version.StartsWith("V")
                ? version
                : string.Concat("V", version);

            // Find old version on the content

            // Try exact match on version string - example input: 1.0.A or V1.0.A
            var oldVersion = content.Versions.AsEnumerable().SingleOrDefault(n => n.Version.VersionString == versionText) as GenericContent;

            // Try adding status code - example input: 1.0 or V1.0
            if (oldVersion == null)
                oldVersion = content.Versions.AsEnumerable().SingleOrDefault(n => n.Version.VersionString == string.Concat(versionText, ".", n.Version.Status.ToString()[0])) as GenericContent;

            // Throw exception if still not found
            if (oldVersion == null)
                throw new Exception($"The requested version '{version}' does not exist on content '{content.Path}'.");

            // Restore old version
            oldVersion.Save();

            // Return actual state of content
            return Content.Load(content.Id);
        }
    }
}
