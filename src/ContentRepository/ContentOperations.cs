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
        /// <summary>
        /// Approves the requested content. The content's version number will be the next major version according to
        /// the content's versioning mode.
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Removes the exclusive lock from the requested content and persists the <paramref name="checkInComments"/>
        /// if there is. The version number is changed according to the content's versioning mode.
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <param name="checkInComments" example="Very good.">The modifier's comments.</param>
        /// <exception cref="Exception">An exception will be thrown if the <paramref name="checkInComments"/> is
        /// missing and the value of the requested content's <c>CheckInCommentsMode</c> is <c>Compulsory</c>.</exception>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Creates a new version of the requested content and locks it exclusively for the current user.
        /// The version number is changed according to the content's versioning mode.
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Publishes the requested content. The version number is changed to the next major version
        /// according to the content's versioning mode.
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Rejects the modifications of the requested content and persists the <paramref name="rejectReason"/>
        /// if there is.
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <param name="rejectReason" example="Rewrite please.">The reviewer's comments.</param>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Drops the last draft version of the requested content if there is. This operation is allowed only
        /// for the user who locked the content or an administrator with <c>ForceCheckin</c> permissions.
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Drops the last draft version of the requested content if there is. This operation is allowed only
        /// for users who have <c>ForceCheckin</c> permission on this content. 
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <returns>The modified content.</returns>
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

        /// <summary>
        /// Restores an old existing version as the last version according to the content's versioning mode.
        /// The old version is identified by the <paramref name="version"/> parameter that can be in
        /// one of the following forms:
        /// - [major].[minor] e.g. "1.2"
        /// - V[major].[minor] e.g. "V1.2"
        /// - [major].[minor].[status] e.g. "1.2.D"
        /// - V[major].[minor].[status] e.g. "V1.2.D"
        /// <para>Note that [status] is not required but an incorrect value causes an exception.</para>
        /// </summary>
        /// <snCategory>Collaboration</snCategory>
        /// <param name="content"></param>
        /// <param name="version">The old version number.</param>
        /// <returns>The modified content.</returns>
        /// <exception cref="Exception">Throws if the requested content version is not found.</exception>
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
