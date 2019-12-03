using System;
using System.Linq;
using SenseNet.ApplicationModel;

// ReSharper disable StringLiteralTypo

namespace SenseNet.ContentRepository
{
    public static class ContentOperations
    {
        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save, N.Approve)]
        [Scenario(N.ListItem, N.ExploreActions, N.SimpleApprovableListItem)]
        public static Content Approve(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't approve content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.Approve();
            return content;
        }

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save)]
        [Scenario(N.ListItem, N.ExploreActions, N.SimpleApprovableListItem)]
        public static Content CheckIn(Content content, string checkInComments = null)
        {
            checkInComments = checkInComments ?? string.Empty;

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

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save)]
        [Scenario(N.ListItem, N.ExploreActions)]
        public static Content CheckOut(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't check out content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.CheckOut();
            return content;
        }

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save, N.Publish)]
        [Scenario(N.ListItem, N.ExploreActions, N.SimpleApprovableListItem)]
        public static Content Publish(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't publish content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.Publish();
            return content;
        }

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save)]
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

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save)]
        [Scenario(N.ListItem, N.ExploreActions)]
        public static Content UndoCheckOut(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't undo check out on content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to inherit " +
                                    "from GenericContent for collaboration feature support.");
            content.UndoCheckOut();
            return content;
        }

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save, N.ForceCheckin)]
        [Scenario(N.ListItem, N.ExploreActions)]
        public static Content ForceUndoCheckOut(Content content)
        {
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't force undo check out on content '{content.Path}' because " +
                                    "its content handler is not a GenericContent. It needs to " +
                                    "inherit from GenericContent for collaboration feature support.");
            content.ForceUndoCheckOut();
            return content;
        }

        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone)]
        [RequiredPermissions(N.Save, N.RecallOldVersion)]
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
