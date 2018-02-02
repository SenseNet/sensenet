using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Security.ADSync;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Security;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a content handler class for a unit of organization and a container to model organization hierarchy.
    /// It is also possible to define permissions for an organizational unit in the sensenet Content Repository.
    /// May contain <see cref="Group"/>s, <see cref="User"/>s and additional <see cref="OrganizationalUnit"/>s.
    /// </summary>
	[ContentHandler]
    public class OrganizationalUnit : Folder, IOrganizationalUnit, IADSyncable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationalUnit"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public OrganizationalUnit(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationalUnit"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
		public OrganizationalUnit(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationalUnit"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
		protected OrganizationalUnit(NodeToken token) : base(token) { }

        /// <summary>
        /// Returns the predefined <see cref="OrganizationalUnit"/> called "Portal".
        /// </summary>
        public static OrganizationalUnit Portal
        {
            get { return (OrganizationalUnit)Node.LoadNode(Identifiers.PortalOrgUnitId); }
        }

        private bool _syncObject = true;

        /// <inheritdoc />
        /// <remarks>Synchronizes the modifications via the current <see cref="DirectoryProvider"/>.</remarks>
        public override void Save(SavingMode mode)
        {
            var originalId = this.Id;

            base.Save(mode);

            // AD Sync
            if (_syncObject)
            {
                ADFolder.SynchADContainer(this, originalId);
            }
            // default: object should be synced. if it was not synced now (sync properties updated only) next time it should be.
            _syncObject = true;
        }

        /// <inheritdoc />
        /// <remarks>Synchronizes the modifications via the current <see cref="DirectoryProvider"/>.</remarks>
	    public override void ForceDelete()
        {
            base.ForceDelete();

            // AD Sync
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                ADProvider.DeleteADObject(this);
            }
        }

        /// <inheritdoc />
        /// <remarks>In this case returns false.</remarks>
        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        /// <remarks>Synchronizes the updates via the current <see cref="DirectoryProvider"/>.</remarks>
        public override void MoveTo(Node target)
        {
            base.MoveTo(target);

            // AD Sync
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                var targetNodePath = RepositoryPath.Combine(target.Path, this.Name);
                ADProvider.UpdateADContainer(this, targetNodePath);
            }
        }

        // =================================================================================== Events

        /// <summary>
        /// Checks whether the Move operation is acceptable by the current <see cref="DirectoryProvider"/>.
        /// The operation will be cancelled if it is prohibited.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnMoving(object sender, CancellableNodeOperationEventArgs e)
        {
            // AD Sync check
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                var targetNodePath = RepositoryPath.Combine(e.TargetNode.Path, this.Name);
                var allowMove = ADProvider.AllowMoveADObject(this, targetNodePath);
                if (!allowMove)
                {
                    e.CancelMessage = "Moving of synced nodes is only allowed within AD server bounds!";
                    e.Cancel = true;
                }
            }

            base.OnMoving(sender, e);
        }

        /// <summary>
        /// After creation adds this group to the nearest parent <see cref="OrganizationalUnit"/> in the security graph as a member.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnCreated(object sender, NodeEventArgs e)
        {
            base.OnCreated(sender, e);

            // insert this orgunit to the security graph
            using (new SystemAccount())
            {
                var parent = GroupMembershipObserver.GetFirstOrgUnitParent(this);
                if (parent != null)
                    SecurityHandler.AddGroupsToGroup(parent.Id, new[] { this.Id });
            }
        }

        // =================================================================================== IADSyncable Members

        /// <summary>
        /// Writes the given AD sync-id to the database.
        /// </summary>
        /// <param name="guid"></param>
        public void UpdateLastSync(System.Guid? guid)
        {
            if (guid.HasValue)
                this["SyncGuid"] = ((System.Guid)guid).ToString();
            this["LastSync"] = System.DateTime.UtcNow;

            // update object without syncing to AD
            _syncObject = false;

            this.Save();
        }

        // =================================================================================== ISecurityContainer members

        /// <summary>
        /// This method is obsolete. Use <see cref="IUser.IsInOrganizationalUnit"/> method instead.
        /// </summary>
        [Obsolete("Use User.IsInOrganizationalUnit instead.", false)]
        public bool IsMember(IUser user)
        {
            return user.IsInOrganizationalUnit(this);
        }

        // =================================================================================== ISecurityMember

        /// <summary>
        /// This method is obsolete. Use <see cref="Group.IsInGroup"/> method instead.
        /// </summary>
        /// <param name="securityGroupId">Id of the container group.</param>
	    [Obsolete("Use IsInGroup instead.", false)]
	    public bool IsInRole(int securityGroupId)
	    {
            return IsInGroup(securityGroupId);
	    }
        /// <summary>
        /// Returns true if this instance is a member of an <see cref="OrganizationalUnit"/> identified by the given groupId.
        /// This method is transitive, meaning it will look for relations in the whole group graph, not 
        /// only direct memberships.
        /// </summary>
        /// <param name="securityGroupId">Id of the container group.</param>
        public bool IsInGroup(int securityGroupId)
        {
            return SecurityHandler.IsInGroup(this.Id, securityGroupId);
        }
    }
}