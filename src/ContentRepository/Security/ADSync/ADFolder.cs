using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Security.ADSync
{
    /// <summary>
    /// Defines a content handler for representation of a synchronized container in the domain network.
    /// </summary>
    [ContentHandler]
    public class ADFolder : Folder, IADSyncable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ADFolder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public ADFolder(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ADFolder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public ADFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ADFolder"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected ADFolder(NodeToken nt) : base(nt) { }

        /*=================================================================================== Members */
        private bool _syncObject = true;


        /*=================================================================================== Methods */

        /// <inheritdoc />
        /// <remarks>Synchronizes the modifications via the current <see cref="DirectoryProvider"/>.</remarks>
        public override void Save(SavingMode mode)
        {
            var originalId = this.Id;

            base.Save(mode);

            // AD Sync
            if (_syncObject)
            {
                SynchADContainer(this, originalId);
            }
            // default: object should be synced. if it was not synced now (sync properties updated only) next time it should be.
            _syncObject = true;
        }

        /// <inheritdoc />
        /// <remarks>Synchronizes the deletion via the current <see cref="DirectoryProvider"/>.</remarks>
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
        /// <remarks>Synchronizes the modifications via the current <see cref="DirectoryProvider"/>.</remarks>
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

        /*=================================================================================== Events */

        /// <summary>
        /// Checks whether the Move operation is acceptable to the current <see cref="DirectoryProvider"/> and
        /// The operation will be cancelled if it is prohibited.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnMoving(object sender, SenseNet.ContentRepository.Storage.Events.CancellableNodeOperationEventArgs e)
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

        /*=================================================================================== Helper methods */

        /// <summary>
        /// Helper method that pushes the given <see cref="Node">container</see> modifications 
        /// to the current <see cref="DirectoryProvider"/>.
        /// </summary>
        /// <param name="node">The represented <see cref="Node">container</see>.</param>
        /// <param name="originalId">Id of the represented <see cref="Node">container</see> before the modifications.</param>
        public static void SynchADContainer(Node node, int originalId)
        {
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider == null) 
                return;

            if (originalId == 0)
                ADProvider.CreateNewADContainer(node);
            else
                ADProvider.UpdateADContainer(node, node.Path);
        }

        /*=================================================================================== IADSyncable Members */

        /// <summary>
        /// Updates the last sync id of this object.
        /// </summary>
        /// <param name="guid">A nullable GUID as sync id.</param>
        public void UpdateLastSync(Guid? guid)
        {
            if (guid.HasValue)
                this["SyncGuid"] = ((Guid)guid).ToString();
            this["LastSync"] = DateTime.UtcNow;

            // update object without syncing to AD
            _syncObject = false;

            this.Save();
        }
    }
}