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
    [ContentHandler]
    public class Workspace : Folder
    {
        public Workspace(Node parent) : this(parent, null) { }
        public Workspace(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Workspace(NodeToken nt) : base(nt) { }

        public const string LocalGroupsFolderName = "Groups";
        
        // duplicate code from ContentList, needs refactor
        public static Workspace GetWorkspaceForNode(Node child)
        {
            return Node.GetAncestorOfType<Workspace>(child);
        }

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

        [RepositoryProperty("IsActive", RepositoryDataType.Int)]
        public bool IsActive
        {
            get { return base.GetProperty<int>("IsActive") != 0; }
            set { this["IsActive"] = value ? 1 : 0; }
        }

        [RepositoryProperty("IsWallContainer", RepositoryDataType.Int)]
        public bool IsWallContainer
        {
            get { return base.GetProperty<int>("IsWallContainer") != 0; }
            set { this["IsWallContainer"] = value ? 1 : 0; }
        }

        [RepositoryProperty("WorkspaceSkin", RepositoryDataType.Reference)]
        public Node WorkspaceSkin
        {
            get { return this.GetReference<Node>("WorkspaceSkin"); }
            set { this.SetReference("WorkspaceSkin", value); }
        }

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

        public override void Save(NodeSaveSettings settings)
        {
            if(this.IsNew)
                SecurityHandler.Assert(this.ParentId, PermissionType.ManageListsAndWorkspaces);
            else
                this.Security.Assert(PermissionType.ManageListsAndWorkspaces);

            base.Save(settings);
        }

        public override void ForceDelete()
        {
            Security.Assert(PermissionType.ManageListsAndWorkspaces);
            base.ForceDelete();
        }

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
