using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    public enum VersioningMode { None, Major, Full }
    internal enum SavingAlgorithm { CreateNewNode, UpdateSameVersion, CopyToNewVersionAndUpdate, CopyToSpecifiedVersionAndUpdate }

    public class NodeSaveSettings
    {
        private VersionNumber __expectedVersion;

        protected NodeHead _nodeHead;
        protected NodeHead.NodeVersion[] _versionHistory;
        private VersionNumber _latestVersion;

        /// <summary>
        /// If set, node will be refreshed from the database after save. Used mainly with undo check out operation.
        /// </summary>
        public bool ForceRefresh { get; protected set; }

        public Node Node { get; set; }
        public bool HasApproving { get; set; }
        public VersioningMode VersioningMode { get; set; }
        public NodeHead NodeHead
        {
            get
            {
                if (_nodeHead == null)
                    _nodeHead = NodeHead.Get(this.Node.Id);
                return _nodeHead;
            }
        }
        public NodeHead.NodeVersion[] VersionHistory
        {
            get
            {
                if (_versionHistory == null)
                    _versionHistory = this.NodeHead != null ? this.NodeHead.Versions : new NodeHead.NodeVersion[0];

                return _versionHistory;
            }
        }
        public VersionNumber CurrentVersion
        {
            get
            {
                if (Node == null)
                    return null;
                if (Node.Id == 0)
                    return null;
                if (Node.Data.SharedData == null)
                    return Node.Data.Version;
                return Node.Data.Version;
            }
        }
        public VersionNumber LatestVersion
        {
            get
            {
                if (_latestVersion == null)
                    _latestVersion = (CurrentVersionId == NodeHead.LastMinorVersionId) ? CurrentVersion : NodeHead.GetLastMinorVersion().VersionNumber;
                return _latestVersion;
            }
        }
        public int CurrentVersionId
        {
            get
            {
                if (Node == null)
                    return 0;
                if (Node.Id == 0)
                    return 0;
                return Node.Data.VersionId;
            }
        }             // 0: new node, not null: current versionId of Node
        public VersionNumber ExpectedVersion
        {
            get
            {
                if (__expectedVersion != null)
                    return __expectedVersion;
                if (Node.Id == 0)
                    return Node.Data.Version;
                return CurrentVersion;
            }
            set
            {
                __expectedVersion = value;
            }
        }    // always must be specified
        public int ExpectedVersionId { get; set; } // null: new version will be created, not null: specified version will be overwritten
        public int? LockerUserId { get; set; }     // null: not changed, 0: unlock. not 0: lock
        public bool NeedToSaveData { get; set; }
        public List<int> DeletableVersionIds { get; set; } // this versions will be deleted
        public int LastMajorVersionIdBefore { get; set; }
        public int LastMinorVersionIdBefore { get; set; }
        public int LastMajorVersionIdAfter { get; set; }
        public int LastMinorVersionIdAfter { get; set; }

        internal bool TakingLockOver { get; set; }

        public bool MultistepSaving { get; set; }

        public string ExpectedSharedLock { get; set; }

        public NodeSaveSettings()
        {
            NeedToSaveData = true;
            DeletableVersionIds = new List<int>();
        }

        public bool IsNewVersion()
        {
            if (ExpectedVersion == null)
                return false;
            if (CurrentVersion == null)
                return true;
            return ExpectedVersionId == 0;
        }
        public bool IsPublic()
        {
            return GetVersionAfterSave().Status == VersionStatus.Approved;
        }
        private VersionNumber GetVersionAfterSave()
        {
            if (ExpectedVersion == null)
                return CurrentVersion;
            return ExpectedVersion;
        }

        public NodeHead.NodeVersion GetLastMajorVersion()
        {
            return this.NodeHead.GetLastMajorVersion();
        }
        public NodeHead.NodeVersion GetLastMinorVersion()
        {
            return this.NodeHead.GetLastMinorVersion();
        }

        internal bool NodeChanged()
        {
            if(Node.IsModified)
                return true;
            if (ExpectedVersionId != 0 && CurrentVersionId != 0 && ExpectedVersionId != CurrentVersionId)
                return true;
            if (ExpectedVersion != null && CurrentVersion != null && ExpectedVersion != CurrentVersion)
                return true;
            return false;
        }

        // for dataprovider
        internal void Validate()
        {
            if(Node == null)
                throw new InvalidOperationException("Invalid setting: Node cannot be null.");
            if (CurrentVersion != null && CurrentVersionId == 0)
                throw new InvalidOperationException("Invalid version combination: CurrentVersion is not null and CurrentVersionId is 0.");
            if (CurrentVersion == null && CurrentVersionId != 0)
                throw new InvalidOperationException("Invalid version combination: CurrentVersion is null and CurrentVersionId is not 0.");
            if (CurrentVersion == null && ExpectedVersion == null)
                throw new InvalidOperationException("Invalid version combination: CurrentVersion is null and ExpectedVersion is null.");
            if (CurrentVersion == null && ExpectedVersionId > 0)
                throw new InvalidOperationException("Invalid version combination: CurrentVersion is null and ExpectedVersionId is not 0.");
        }
        internal SavingAlgorithm GetSavingAlgorithm()
        {
            Validate();

            if (CurrentVersionId == 0)
                return SavingAlgorithm.CreateNewNode;
            if (ExpectedVersionId == 0)
                return SavingAlgorithm.CopyToNewVersionAndUpdate;
            if (CurrentVersionId == ExpectedVersionId)
                return SavingAlgorithm.UpdateSameVersion;
            if (CurrentVersionId != ExpectedVersionId)
                return SavingAlgorithm.CopyToSpecifiedVersionAndUpdate;

            throw new ApplicationException("Invalid version combination.");
        }
    }
}
