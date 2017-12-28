using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using System.Web;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Workspaces
{
    /// <summary>
    /// A Content handler that represents a root Content of a collaborative workspace in the sensenet Content Repository.
    /// </summary>
    [ContentHandler]
    public class Workspace : Folder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Workspace"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Workspace(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Workspace"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Workspace(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Workspace"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected Workspace(NodeToken nt) : base(nt) { }

        /// <summary>
        /// Defines a constant for the name of the local groups folder.
        /// </summary>
        public const string LocalGroupsFolderName = "Groups";

        /// <summary>
        /// Returs a parent <see cref="Workspace"/> of the given <see cref="Node"/> if it is found.
        /// </summary>
        /// <param name="child">The <see cref="Node"/> instance to find the owner workspace for.</param>
        /// <returns>The existing <see cref="Workspace"/> instance or null if it was not found.</returns>
        public static Workspace GetWorkspaceForNode(Node child)
        {
            return Node.GetAncestorOfType<Workspace>(child);
        }

        /// <summary>
        /// Finds the given <see cref="Node"/>'s ancestor <see cref="Workspace"/> that supports a wall functionality.
        /// </summary>
        /// <param name="child">The <see cref="Node"/> instance to find the owner workspace for.</param>
        /// <returns>The existing <see cref="Workspace"/> instance or null if it was not found.</returns>
        public static Workspace GetWorkspaceWithWallForNode(Node child)
        {
            var parentWs = Node.GetAncestorOfType<Workspace>(child);
            if (parentWs == null || parentWs.IsWallContainer)
                return parentWs;

            return GetWorkspaceWithWallForNode(parentWs.Parent);
        }
        
        /// <summary>
        /// Returns all members from workspace local groups 
        /// </summary>
        public IEnumerable<IUser> GetWorkspaceMembers()
        {
            var members = new List<IUser>();

            var groupFolderPath = RepositoryPath.Combine(this.Path, LocalGroupsFolderName);

            var settings = new SenseNet.Search.QuerySettings { EnableAutofilters = FilterStatus.Disabled };
            var workspaceGroups = SenseNet.Search.ContentQuery.Query(SafeQueries.InTreeAndTypeIs, settings, groupFolderPath, typeof(Group).Name).Nodes;

            foreach (var group in workspaceGroups.OfType<IGroup>())
            {
                members.AddRange(group.Members.OfType<IUser>());
            }

            return members.Distinct();
        }

        /// <summary>
        /// Gets or sets whether this workspace is active or not.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("IsActive", RepositoryDataType.Int)]
        public bool IsActive
        {
            get { return base.GetProperty<int>("IsActive") != 0; }
            set { this["IsActive"] = value ? 1 : 0; }
        }

        /// <summary>
        /// Gets or sets true if this instance supports the wall functionality.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("IsWallContainer", RepositoryDataType.Int)]
        public bool IsWallContainer
        {
            get { return base.GetProperty<int>("IsWallContainer") != 0; }
            set { this["IsWallContainer"] = value ? 1 : 0; }
        }

        /// <summary>
        /// Gets or sets the custom Skin used under this workspace.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty("WorkspaceSkin", RepositoryDataType.Reference)]
        public Node WorkspaceSkin
        {
            get { return this.GetReference<Node>("WorkspaceSkin"); }
            set { this.SetReference("WorkspaceSkin", value); }
        }

        /// <summary>
        /// Gets true if this <see cref="Workspace"/> instance is followed by the logged-in user.
        /// </summary>
        public bool IsFollowed
        {
            get
            {
                var user = User.Current as User;
                if (user == null)
                    return false;

                return user.HasReference("FollowedWorkspaces", this);
            }
        }

        // ===================================================================================== Overrides

        /// <inheritdoc />
        public override void Save(NodeSaveSettings settings)
        {
            if(this.IsNew)
                SecurityHandler.Assert(this.ParentId, PermissionType.ManageListsAndWorkspaces);
            else
                this.Security.Assert(PermissionType.ManageListsAndWorkspaces);

            base.Save(settings);
        }

        /// <inheritdoc />
        public override void ForceDelete()
        {
            Security.Assert(PermissionType.ManageListsAndWorkspaces);
            base.ForceDelete();
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "IsActive":
                    return this.IsActive;
                case "IsWallContainer":
                    return this.IsWallContainer;
                case "WorkspaceSkin":
                    return this.WorkspaceSkin;
                case "IsFollowed":
                    return this.IsFollowed;
                default:
                    return base.GetProperty(name);
            }
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "IsActive":
                    this.IsActive = (bool)value;
                    break;
                case "IsWallContainer":
                    this.IsWallContainer = (bool)value;
                    break;
                case "WorkspaceSkin":
                    this.WorkspaceSkin = (Node)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
