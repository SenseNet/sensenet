using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A Content handler that can wrap a temporarily deleted Content and its subtree.
    /// </summary>
    [ContentHandler]
    public class TrashBag : Folder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrashBag"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public TrashBag(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="TrashBag"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public TrashBag(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="TrashBag"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected TrashBag(NodeToken tk) : base(tk) { }

        /// <summary>
        /// Gets or sets the time until item can not be deleted.
        /// </summary>
        [RepositoryProperty("KeepUntil", RepositoryDataType.DateTime)]
        public DateTime KeepUntil
        {
            get { return (DateTime)base.GetProperty("KeepUntil"); }
            set { this["KeepUntil"] = value; }
        }

        /// <summary>
        /// Gets or sets the original path of the wrapped Content.
        /// Persisted as <see cref="RepositoryDataType.String"/>.
        /// </summary>
        [RepositoryProperty("OriginalPath", RepositoryDataType.String)]
        public string OriginalPath
        {
            get { return (string)base.GetProperty("OriginalPath"); }
            set { this["OriginalPath"] = value; }
        }

        private const string WORKSPACEIDPROPERTY = "WorkspaceId";
        /// <summary>
        /// Gets or sets the Id of the original owner Workspace of the wrapped Content. 
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty(WORKSPACEIDPROPERTY, RepositoryDataType.Int)]
        public int WorkspaceId
        {
            get { return (int)base.GetProperty(WORKSPACEIDPROPERTY); }
            set { this[WORKSPACEIDPROPERTY] = value; }
        }

        private const string WORKSPACERELATIVEPATHPROPERTY = "WorkspaceRelativePath";
        /// <summary>
        /// Gets or sets the relative path of the wrapped Content in the original owner Workspace.
        /// Persisted as <see cref="RepositoryDataType.String"/>.
        /// </summary>
        [RepositoryProperty(WORKSPACERELATIVEPATHPROPERTY, RepositoryDataType.String)]
        public string WorkspaceRelativePath
        {
            get { return (string)base.GetProperty(WORKSPACERELATIVEPATHPROPERTY); }
            set { this[WORKSPACERELATIVEPATHPROPERTY] = value; }
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "KeepUntil":
                    return this.KeepUntil;
                case "OriginalPath":
                    return this.OriginalPath;
                case WORKSPACERELATIVEPATHPROPERTY:
                    return this.WorkspaceRelativePath;
                case WORKSPACEIDPROPERTY:
                    return this.WorkspaceId;
                case DELETEDCONTENTROPERTY:
                    return this.DeletedContent;
                default:
                    return base.GetProperty(name);
            }
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "KeepUntil":
                    this.KeepUntil = (DateTime)value;
                    break;
                case "OriginalPath":
                    this.OriginalPath = (string)value;
                    break;
                case WORKSPACERELATIVEPATHPROPERTY:
                    this.WorkspaceRelativePath = (string)value;
                    break;
                case WORKSPACEIDPROPERTY:
                    this.WorkspaceId = (int)value;
                    break;
                case "Link":
                    this.Link = (Node)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        /// <summary>
        /// Gets whether this <see cref="TrashBag"/> can be deleted permanently. Returns true in case the time
        /// stored in the <see cref="KeepUntil"/> property has passed.
        /// </summary>
        public bool IsPurgeable
        {
            get { return (DateTime.UtcNow > KeepUntil); }
        }

        /// <inheritdoc />
        public override string Icon
        {
            get
            {
                return DeletedContent != null ? DeletedContent.Icon : base.Icon;
            }
        }

        /// <inheritdoc />
        /// <remarks>In this case returns false: trash bags themselves cannot be moved to the Trash.</remarks>
        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        /// <remarks>Cannot be deleted permanently before the minimum retention time - otherwise 
        /// an <see cref="ApplicationException"/> will be thrown.</remarks>
        public override void ForceDelete()
        {
            if (!IsPurgeable)
                throw new ApplicationException("Trashbags cannot be purged before their minimum retention date");
            base.ForceDelete();
        }

        /// <inheritdoc />
        public override void Delete()
        {
            ForceDelete();
        }

        private void Destroy()
        {
            using (new SystemAccount())
            {
                this.KeepUntil = DateTime.Today.AddDays(-1);
                this.ForceDelete();    
            }
        }

        /// <summary>
        /// Returns a new <see cref="TrashBag"/> instance that packages the 
        /// given <see cref="GenericContent"/> instance.
        /// </summary>
        /// <param name="node">The <see cref="GenericContent"/> instance that will be wrapped.</param>
        public static TrashBag BagThis(GenericContent node)
        {
            var bin = TrashBin.Instance;
            if (bin == null)
                return null;

            if (node == null)
                throw new ArgumentNullException("node");

            // creating a bag has nothing to do with user permissions: Move will handle that
            TrashBag bag = null;
            var wsId = 0;
            var wsRelativePath = string.Empty;
            var ws = SystemAccount.Execute(() => node.Workspace);
            if (ws != null)
            {
                wsId = ws.Id;
                wsRelativePath = node.Path.Substring(ws.Path.Length);
            }

            using (new SystemAccount())
            {
                bag = new TrashBag(bin)
                          {
                              KeepUntil = DateTime.UtcNow.AddDays(bin.MinRetentionTime),
                              OriginalPath = RepositoryPath.GetParentPath(node.Path),
                              WorkspaceRelativePath = wsRelativePath,
                              WorkspaceId = wsId,
                              DisplayName = node.DisplayName,
                              Link = node,
                              Owner = node.Owner
                          };
                bag.Save();

                CopyPermissions(node, bag);

                // add delete permission for the owner
                SecurityHandler.CreateAclEditor()
                    .Allow(bag.Id, node.OwnerId, false, PermissionType.Delete)
                    .Apply();
            }

            try
            {
                Node.Move(node.Path, bag.Path);
            }
            catch(Exception ex)
            {
                SnLog.WriteException(ex);

                bag.Destroy();

                throw new InvalidOperationException("Error moving item to the trash", ex);
            }

            return bag;
        }

        private static void CopyPermissions(Node source, Node target)
        {
            if (source == null || source.ParentId == 0 || target == null)
                return;

            // copy permissions from the source content, without reseting the permission system
            SecurityHandler.CopyPermissionsFrom(source.Id, target.Id, CopyPermissionMode.BreakAndClear);

            //UNDONE: do we need to deal with Sharing permissions here, or just Normal?

            // If there were any permission settings for the Creators group on the source content, we 
            // need to place an explicite entry with the same permissions onto the target for the creator 
            // user, as the creator of the trashbag (the user who deletes the content) may be different 
            // than the creator of the original document.
            var aces = SecurityHandler.GetEffectiveEntriesAsSystemUser(source.Id, new[] { Identifiers.OwnersGroupId });
            foreach (var ace in aces)
                SecurityHandler.CreateAclEditor().Set(target.Id, ace.IdentityId, ace.LocalOnly, ace.AllowBits, ace.DenyBits);
        }


        [RepositoryProperty("Link", RepositoryDataType.Reference)]
        private Node Link
        {
            get { return base.GetReference<Node>("Link"); }
            set
            {
                base.SetReference("Link", value);
                _originalContent = value as GenericContent;
            }
        }

        private bool _resolved;
        private GenericContent _originalContent;

        private const string DELETEDCONTENTROPERTY = "DeletedContent";
        /// <summary>
        /// Gets the wrapped <see cref="GenericContent"/> instance.
        /// </summary>
        public GenericContent DeletedContent
        {
            get
            {
                if (_resolved)
                    return _originalContent;
                 _originalContent = Link as GenericContent;
                 _resolved = true;
                return _originalContent;
            }
        }

    }
}
