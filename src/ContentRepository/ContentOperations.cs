using System;
using SenseNet.ApplicationModel;
// ReSharper disable StringLiteralTypo

namespace SenseNet.ContentRepository
{
    public static class ContentOperations
    {
        [ODataAction]
        [ContentType(N.GenericContent)]
        [SnAuthorize(Role = N.Everyone, Permission = N.Save + ", " + N.Approve )]
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
        [SnAuthorize(Role = N.Everyone, Permission = N.Save)]
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
        [SnAuthorize(Role = N.Everyone, Permission = N.Save)]
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
        [SnAuthorize(Role = N.Everyone, Permission = N.Save + ", " + N.Publish)]
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
        [SnAuthorize(Role = N.Everyone)] //UNDONE:? Reject permissions
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
        [SnAuthorize(Role = N.Everyone, Permission = N.Save)]
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

    }
}
