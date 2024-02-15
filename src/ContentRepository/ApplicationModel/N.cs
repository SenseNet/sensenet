﻿// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace SenseNet.ApplicationModel
{
    /// <summary>
    /// Defines constants for the OData method attributes.
    /// </summary>
    public static class N
    {
        /* ==================================================================== CONTENT TYPE NAMES */

        /// <summary>
        /// Defines content type name constants for the <see cref="ContentTypesAttribute"/> attribute.
        /// </summary>
        public static class CT
        {
            public const string ContentType = "ContentType";
            public const string GenericContent = "GenericContent";
            public const string Folder = "Folder";
            public const string File = "File";
            public const string Image = "Image";
            public const string PreviewImage = "PreviewImage";
            public const string Group = "Group";
            public const string User = "User";
            public const string PortalRoot = "PortalRoot";
            public const string TrashBag = "TrashBag";
        }

        /* ==================================================================== ROLE NAMES */

        /// <summary>
        /// Defines role name constants for the <see cref="AllowedRolesAttribute"/> attribute.
        /// </summary>
        public static class R
        {
            public const string Administrators = "/Root/IMS/BuiltIn/Portal/Administrators";
            public const string PublicAdministrators = "/Root/IMS/Public/Administrators";
            public const string Developers = "/Root/IMS/BuiltIn/Portal/Developers";
            public const string Editors = "/Root/IMS/BuiltIn/Portal/Editors";
            public const string Everyone = "/Root/IMS/BuiltIn/Portal/Everyone";
            public const string IdentifiedUsers = "/Root/IMS/BuiltIn/Portal/IdentifiedUsers";
            public const string Visitor = "/Root/IMS/BuiltIn/Portal/Visitor";
            public const string AITextUsers = "/Root/IMS/BuiltIn/Portal/AITextUsers";
            public const string AIVisionUsers = "/Root/IMS/BuiltIn/Portal/AIVisionUsers";
            public const string All = "All";
        }

        /* ==================================================================== PERMISSION NAMES */

        /// <summary>
        /// Defines permission name constants for the <see cref="RequiredPermissionsAttribute"/> attribute.
        /// </summary>
        public static class P
        {
            public const string See = "See";
            public const string Preview = "Preview";
            public const string PreviewWithoutWatermark = "PreviewWithoutWatermark";
            public const string PreviewWithoutRedaction = "PreviewWithoutRedaction";
            public const string Open = "Open";
            public const string OpenMinor = "OpenMinor";
            public const string Save = "Save";
            public const string Publish = "Publish";
            public const string ForceCheckin = "ForceCheckin";
            public const string AddNew = "AddNew";
            public const string Approve = "Approve";
            public const string Delete = "Delete";
            public const string RecallOldVersion = "RecallOldVersion";
            public const string DeleteOldVersion = "DeleteOldVersion";
            public const string SeePermissions = "SeePermissions";
            public const string SetPermissions = "SetPermissions";
            public const string RunApplication = "RunApplication";
            public const string ManageListsAndWorkspaces = "ManageListsAndWorkspaces";
            public const string TakeOwnership = "TakeOwnership";
        }

        /* ==================================================================== SCENARIO NAMES */

        /// <summary>
        /// Defines scenario name constants for the <see cref="ScenarioAttribute"/> attribute.
        /// </summary>
        public static class S
        {
            public const string ListItem = "ListItem";
            public const string ExploreActions = "ExploreActions";
            public const string WorkspaceActions = "WorkspaceActions";
            public const string SimpleApprovableListItem = "SimpleApprovableListItem";
            public const string GridToolbar = "GridToolbar";
            public const string UserActions = "UserActions";
            public const string ExploreToolbar = "ExploreToolbar";
            public const string ManageViewsListItem = "ManageViewsListItem";
            public const string ListActions = "ListActions";
            public const string SimpleListItem = "SimpleListItem";
            public const string ReadOnlyListItem = "ReadOnlyListItem";
            public const string DocumentDetails = "DocumentDetails";
            public const string ContextMenu = "ContextMenu";
            public const string BatchActions = "BatchActions";
        }

        /* ==================================================================== POLICY NAMES */

        /// <summary>
        /// Defines policy name constants for the <see cref="RequiredPoliciesAttribute"/> attribute.
        /// </summary>
        public static class Pol
        {
            public const string VersioningAndApproval = "VersioningAndApproval";
        }
    }
}
