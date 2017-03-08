using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Security.ADSync
{
    [ContentHandler]
    public class ADFolder : Folder, IADSyncable
    {
        public ADFolder(Node parent) : this(parent, null) { }
        public ADFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ADFolder(NodeToken nt) : base(nt) { }

        /*=================================================================================== Members */
        private bool _syncObject = true;


        /*=================================================================================== Methods */

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

        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

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