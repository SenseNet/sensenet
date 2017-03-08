using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository
{
    public class SavingAction : NodeSaveSettings
    {
        private static readonly string[] VERSIONING_ACTIONS = new[] { "checkin", "checkout", "undocheckout", "forceundocheckout", "publish", "approve", "reject" };

        public static SavingAction Create(Node node)
        {
            var genericContent = node as GenericContent;
            var savingAction = (genericContent == null) ? CreateForNode(node) : CreateForGenericContent(genericContent);
            if (node.Id == 0)
            {
                savingAction.ExpectedVersion = node.Version;
                savingAction.ExpectedVersionId = node.VersionId;
            }

            return savingAction;
        }
        private static SavingAction CreateForNode(Node node)
        {
            var savingAction = new SavingAction
            {
                Node = node,
                VersioningMode = VersioningMode.None,
                HasApproving = false
            };

            return savingAction;
        }
        private static SavingAction CreateForGenericContent(GenericContent genericContent)
        {
            var mode = VersioningMode.None;
            switch (genericContent.VersioningMode)
            {
                case VersioningType.MajorOnly: mode = VersioningMode.Major; break;
                case VersioningType.MajorAndMinor: mode = VersioningMode.Full; break;
            }
            var savingAction = new SavingAction
            {
                Node = genericContent,
                VersioningMode = mode,
                HasApproving = genericContent.HasApproving
            };

            return savingAction;
        }

        private void Create()
        {
            ExpectedVersion = ComputeNewVersion();
        }
        public void CheckOut()
        {
            AssertValidAction(StateAction.CheckOut);
            var currentUserId = SenseNet.ContentRepository.Storage.Security.AccessProvider.Current.GetOriginalUser().Id;
            this.LockerUserId = currentUserId;
            this.ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Locked);
        }
        public void CheckIn()
        {
            AssertValidAction(StateAction.CheckIn);

            if (!HasApproving)
            {
                // Approving OFF
                switch (VersioningMode)
                {
                    case VersioningMode.None:
                        DeleteVersionsAndApprove();
                        break;
                    case VersioningMode.Major:
                        // remove all working versions, except current
                        DeletableVersionIds.AddRange(GetLastWorkingVersions().Select(x => x.VersionId));
                        DeletableVersionIds.Remove(CurrentVersionId);

                        var lastApproved = GetLastApprovedVersion();
                        ExpectedVersion = lastApproved != null ?
                            GetNextPublicVersion(lastApproved.VersionNumber, VersionStatus.Approved) :
                            ComputeNewVersion();

                        ExpectedVersionId = CurrentVersionId;
                        break;
                    case VersioningMode.Full:
                        ExpectedVersion = CurrentVersion.ChangeStatus(VersionStatus.Draft);
                        ExpectedVersionId = CurrentVersionId;
                        break;
                }
            }
            else
            {
                // Approving ON
                switch (VersioningMode)
                {
                    case VersioningMode.None:
                        DeleteVersionsAndPreserveLastWorking();
                        break;
                    case VersioningMode.Major:
                        ExpectedVersion = CurrentVersion.ChangeStatus(VersionStatus.Pending);
                        ExpectedVersionId = CurrentVersionId;
                        break;
                    case VersioningMode.Full:
                        ExpectedVersion = CurrentVersion.ChangeStatus(VersionStatus.Draft);
                        ExpectedVersionId = CurrentVersionId;
                        break;
                }
            }

            // Unlock
            this.LockerUserId = 0;
        }
        
        public void UndoCheckOut()
        {
            UndoCheckOut(true);
        }

        public void UndoCheckOut(bool forceRefresh = true)
        {
            NeedToSaveData = false;
            AssertValidAction(StateAction.UndoCheckOut);
            if (VersionHistory.Length == 1)
            {
                // there is only one version that is locked
                throw new InvalidContentActionException(InvalidContentActionReason.UndoSingleVersion, this.Node.Path);
            }
            
            var lastNodeVersion = VersionHistory[VersionHistory.Length - 2];
            ExpectedVersion = lastNodeVersion.VersionNumber;
            ExpectedVersionId = lastNodeVersion.VersionId;

            var deletableNodeVersion = VersionHistory[VersionHistory.Length - 1];
            DeletableVersionIds.Add(deletableNodeVersion.VersionId);

            LockerUserId = 0;
            ForceRefresh = forceRefresh;
        }
        public void Publish()
        {
            AssertValidAction(StateAction.Publish);

            if (!HasApproving)
            {
                // Approving OFF
                switch (VersioningMode)
                {
                    case VersioningMode.None:
                        DeleteVersionsAndApprove();
                        break;
                    case VersioningMode.Major:
                        ExpectedVersion = GetNextPublicVersion(VersionStatus.Approved);
                        ExpectedVersionId = CurrentVersionId;
                        break;
                    case VersioningMode.Full:
                        ExpectedVersion = GetNextPublicVersion(VersionStatus.Approved);
                        ExpectedVersionId = CurrentVersionId;
                        break;
                }
            }
            else
            {
                // Approving ON
                if (VersioningMode != VersioningMode.Full)
                    throw new SnNotSupportedException();

                if (CurrentVersion.Status == VersionStatus.Rejected)
                {
                    ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Pending);
                }
                else
                {
                    ExpectedVersion = CurrentVersion.ChangeStatus(VersionStatus.Pending);
                    ExpectedVersionId = CurrentVersionId;
                }
            }

            // Unlock
            this.LockerUserId = 0;
        }
        public void Approve()
        {
            AssertValidAction(StateAction.Approve);

            if (HasApproving)
            {
                // Approving ON
                switch (VersioningMode)
                {
                    case VersioningMode.None:
                        DeleteVersionsAndApprove();
                        break;
                    case VersioningMode.Major:
                        var workingIds = GetLastWorkingVersions().Select(x => x.VersionId);

                        DeletableVersionIds.AddRange(workingIds);
                        DeletableVersionIds.Remove(CurrentVersionId);

                        var lastApproved = GetLastApprovedVersion();

                        ExpectedVersion = lastApproved == null ?
                            ComputeNewVersion().ChangeStatus(VersionStatus.Approved) :
                            GetNextPublicVersion(lastApproved.VersionNumber, VersionStatus.Approved);

                        ExpectedVersionId = CurrentVersionId;
                        break;
                    case VersioningMode.Full:
                        ExpectedVersion = GetNextPublicVersion(VersionStatus.Approved);
                        ExpectedVersionId = this.CurrentVersionId;
                        break;
                }
            }
        }
        public void Reject()
        {
            AssertValidAction(StateAction.Reject);

            if (!HasApproving)
                return;

            ExpectedVersion = CurrentVersion.ChangeStatus(VersionStatus.Rejected);
            ExpectedVersionId = CurrentVersionId;
        }
        public void SaveSameVersion()
        {
            if (this.Node.Id == 0)
            {
                Create();
                return;
            }
            else
            {
                if (this.Node.IsVersionChanged())
                    throw new InvalidContentActionException("Cannot modify the version.");
            }

            ExpectedVersionId = CurrentVersionId;
            AssertValidAction(StateAction.Save);
        }
        public void SaveExplicitVersion()
        {
            ExpectedVersion = this.Node.Version;
            if (this.Node.Id != 0 && this.LatestVersion == (VersionNumber)this.Node.GetStoredValue("Version"))
                ExpectedVersionId = VersionHistory.Last().VersionId;
        }
        public void CheckOutAndSave()
        {
            if (this.Node.Id == 0)
            {
                Create();
                return;
            }

            AssertValidAction(StateAction.Save);

            if (this.CurrentVersion.Status == VersionStatus.Locked)
                return;

            if (this.Node.LockedById == 0)
                this.LockerUserId = User.Current.Id;

            this.ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Locked);
        }
        public void CheckOutAndSaveAndCheckIn()
        {
            if (CurrentVersion == null)
            {
                Create();
                return;
            }
            AssertValidAction(StateAction.SaveAndCheckIn);
            // Expected version
            if (!HasApproving)
            {
                // Approving OFF
                switch (VersioningMode)
                {
                    case VersioningMode.None:
                        DeleteVersionsAndApprove();
                        break;
                    case VersioningMode.Major:
                        // remove all working versions, except current
                        var irrelevantIds = GetLastWorkingVersions().Select(x => x.VersionId);

                        DeletableVersionIds.AddRange(irrelevantIds);
                        DeletableVersionIds.Remove(CurrentVersionId);

                        var lastApproved = GetLastApprovedVersion();

                        ExpectedVersion = lastApproved != null ?
                            GetNextPublicVersion(lastApproved.VersionNumber, VersionStatus.Approved) :
                            ComputeNewVersion();

                        // preserve last working version
                        if (CurrentVersion.Status != VersionStatus.Approved && CurrentVersion.Status != VersionStatus.Rejected)
                            ExpectedVersionId = CurrentVersionId;

                        break;
                    case VersioningMode.Full:
                        this.ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Draft);
                        this.LockerUserId = 0;
                        ExpectedVersionId = 0;
                        break;
                }
            }
            else
            {
                // Approving ON
                switch (VersioningMode)
                {
                    case VersioningMode.None:
                        DeleteVersionsAndPreserveLastWorking();
                        break;
                    case VersioningMode.Major:
                        switch (CurrentVersion.Status)
                        {
                            case VersionStatus.Approved: // raise
                            case VersionStatus.Rejected: // raise
                                this.ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Pending);
                                break;
                            case VersionStatus.Draft:    // preserve
                            case VersionStatus.Pending:  // preserve
                                ExpectedVersion = CurrentVersion.ChangeStatus(VersionStatus.Pending);
                                ExpectedVersionId = CurrentVersionId;
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        break;
                    case VersioningMode.Full:
                        this.ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Draft);
                        ExpectedVersionId = 0;
                        break;
                }
            }

            // Unlock
            this.LockerUserId = 0;
        }

        public void SaveAndLock()
        {
            AssertValidAction(StateAction.Save);

            if (this.Node.Id != 0)
            {
                // The only use case for this is when the user tries to restore 
                // an old version of a checked out content, so it must be
                // checked out by her.
                if (this.Node.LockedById != 0 && this.Node.LockedById != User.Current.Id)
                    throw new InvalidContentActionException(InvalidContentActionReason.CheckedOutToSomeoneElse, this.Node.Path);

                ExpectedVersion = new VersionNumber(LatestVersion.Major, LatestVersion.Minor, VersionStatus.Locked);
                ExpectedVersionId = this.NodeHead.LastMinorVersionId;
            }
            else
            {
                Create();
                this.ExpectedVersion = new VersionNumber(this.ExpectedVersion.Major, this.ExpectedVersion.Minor, VersionStatus.Locked);
            }

            this.LockerUserId = User.Current.Id;
        }

        public void StartMultistepSave()
        {
            AssertValidAction(StateAction.Save);

            if (!this.Node.IsNew)
            {
                // The only use case for this is when a user starts a multistep saving operation on an existing 
                // content. In this case we need to perform a 'technical check out' on the content.
                if (this.CurrentVersion.Status == VersionStatus.Locked)
                {
                    if (this.Node.LockedById != User.Current.Id)
                        throw new InvalidContentActionException(InvalidContentActionReason.CheckedOutToSomeoneElse, this.Node.Path);

                    ExpectedVersion = new VersionNumber(LatestVersion.Major, LatestVersion.Minor, VersionStatus.Locked);
                    ExpectedVersionId = this.NodeHead.LastMinorVersionId;
                }
                else
                {
                    this.ExpectedVersion = GetNextNonpublicVersion(VersionStatus.Locked);
                }
            }
            else
            {
                Create();
                this.ExpectedVersion = new VersionNumber(this.ExpectedVersion.Major, this.ExpectedVersion.Minor, VersionStatus.Locked);
            }

            this.MultistepSaving = true;
            this.LockerUserId = User.Current.Id;
        }

        public void Execute()
        {
            if (this.Node.IsNew)
            {
                var gc = Node.Parent as GenericContent;
                if (gc != null)
                    gc.AssertAllowedChildType(Node);
            }

            ContentNamingProvider.ValidateName(this.Node.Name);

            var autoNamingAllowed = false;
            if (this.Node.AllowIncrementalNaming.HasValue)
                autoNamingAllowed = this.Node.AllowIncrementalNaming.Value;
            else
                autoNamingAllowed = this.Node.Id == 0 && ContentType.GetByName(this.Node.NodeType.Name).AllowIncrementalNaming;

            while (true)
            {
                try
                {
                    Node.Save(this);
                    break;
                }
                catch (Storage.Data.NodeAlreadyExistsException)
                {
                    if (!autoNamingAllowed)
                        throw;

                    this.Node.Name = ContentNamingProvider.IncrementNameSuffixToLastName(Node.Name, Node.ParentId);
                }
            }
        }

        // ================================================================================================

        private IEnumerable<NodeHead.NodeVersion> GetLastWorkingVersions()
        {
            var result = new List<NodeHead.NodeVersion>();
            for (var i = VersionHistory.Length - 1; i >= 0; i--)
            {
                var nodeVersion = VersionHistory[i];
                if (nodeVersion.VersionNumber.Status != VersionStatus.Approved)
                    result.Add(nodeVersion);
                else
                    break;
            }
            return result;
        }
        private NodeHead.NodeVersion GetLastApprovedVersion()
        {
            for (int i = VersionHistory.Length - 1; i >= 0; i--)
            {
                var nodeVersion = VersionHistory[i];
                if (nodeVersion.VersionNumber.Status == VersionStatus.Approved)
                    return nodeVersion;
            }
            return null;
        }
        private NodeHead.NodeVersion GetLastApprovedOrRejectedVersion()
        {
            for (int i = VersionHistory.Length - 1; i >= 0; i--)
            {
                var nodeVersion = VersionHistory[i];
                if (nodeVersion.VersionNumber.Status == VersionStatus.Approved || nodeVersion.VersionNumber.Status == VersionStatus.Rejected)
                    return nodeVersion;
            }
            return null;
        }
        private List<NodeHead.NodeVersion> GetNewerVersions(NodeHead.NodeVersion version)
        {
            var result = new List<NodeHead.NodeVersion>();
            for (int i = VersionHistory.Length - 1; i >= 0; i--)
            {
                var nodeVersion = VersionHistory[i];
                if (nodeVersion != version)
                    result.Add(nodeVersion);
                else
                    break;
            }
            return result;
        }

        // ================================================================================================
        public VersionNumber ComputeNewVersion()
        {
            return ComputeNewVersion(HasApproving, this.VersioningMode);
        }
        public static VersionNumber ComputeNewVersion(bool hasApproving, VersioningType versioningMode)
        {
            if (!hasApproving)
            {
                switch (versioningMode)
                {
                    case VersioningType.None: return new VersionNumber(1, 0, VersionStatus.Approved);
                    case VersioningType.MajorOnly: return new VersionNumber(1, 0, VersionStatus.Approved);
                    case VersioningType.MajorAndMinor: return new VersionNumber(0, 1, VersionStatus.Draft);
                }
            }
            else
            {
                switch (versioningMode)
                {
                    case VersioningType.None: return new VersionNumber(1, 0, VersionStatus.Pending);
                    case VersioningType.MajorOnly: return new VersionNumber(1, 0, VersionStatus.Pending);
                    case VersioningType.MajorAndMinor: return new VersionNumber(0, 1, VersionStatus.Draft);
                }
            }
            throw new SnNotSupportedException();
        }
        public static VersionNumber ComputeNewVersion(bool hasApproving, VersioningMode versioningMode)
        {
            if (!hasApproving)
            {
                switch (versioningMode)
                {
                    case VersioningMode.None: return new VersionNumber(1, 0, VersionStatus.Approved);
                    case VersioningMode.Major: return new VersionNumber(1, 0, VersionStatus.Approved);
                    case VersioningMode.Full: return new VersionNumber(0, 1, VersionStatus.Draft);
                }
            }
            else
            {
                switch (versioningMode)
                {
                    case VersioningMode.None: return new VersionNumber(1, 0, VersionStatus.Pending);
                    case VersioningMode.Major: return new VersionNumber(1, 0, VersionStatus.Pending);
                    case VersioningMode.Full: return new VersionNumber(0, 1, VersionStatus.Draft);
                }
            }
            throw new SnNotSupportedException();
        }

        private VersionNumber GetNextNonpublicVersion(VersionStatus status)
        {
            if (CurrentVersion == null)
                return ComputeNewVersion().ChangeStatus(status);

            var major = this.LatestVersion.Major;
            var minor = this.LatestVersion.Minor;

            if (this.VersioningMode == VersioningMode.Full)
            {
                minor++;
            }
            else
            {
                major++;
                minor = 0;
            }

            return new VersionNumber(major, minor, status);
        }
        private VersionNumber GetNextVersion(VersionNumber version, VersionStatus status)
        {
            var major = version.Major;
            var minor = version.Minor;
            if (this.VersioningMode == VersioningMode.Full)
            {
                minor++;
            }
            else
            {
                major++;
                minor = 0;
            }

            return new VersionNumber(major, minor, status);
        }
        private VersionNumber GetNextPublicVersion(VersionStatus status)
        {
            return GetNextPublicVersion(this.CurrentVersion, status);
        }
        private static VersionNumber GetNextPublicVersion(VersionNumber version, VersionStatus status)
        {
            var major = version.Major + 1;
            return new VersionNumber(major, 0, status);
        }

        private void DeleteVersionsAndApprove()
        {
            if (VersioningMode != VersioningMode.None)
                throw new NotSupportedException();

            // Remove unnecessary working versions. Preserve the last public version row
            // 1.0A	1.0A
            // 1.1D	1.1D
            // 2.0A	2.0A <--
            // 2.1D
            // 2.2L <--
            var workings = GetLastWorkingVersions();
            var workingIds = workings.Select(x => x.VersionId);
            DeletableVersionIds.AddRange(workingIds);

            var lastApproved = GetLastApprovedVersion();
            if (lastApproved != null)
            {
                ExpectedVersion = lastApproved.VersionNumber.ChangeStatus(VersionStatus.Approved);
                ExpectedVersionId = lastApproved.VersionId;
            }
            else
            {
                DeletableVersionIds.Remove(CurrentVersionId);
                ExpectedVersion = new VersionNumber(1, 0, VersionStatus.Approved);
                ExpectedVersionId = CurrentVersionId;
            }
        }
        private void DeleteVersionsAndPreserveLastWorking()
        {
            if (VersioningMode != VersioningMode.None)
                throw new NotSupportedException();

            // Remove unnecessary working versions. Preserve the last approved or rejected row
            var lastApprovedOrRejectedVersion = GetLastApprovedOrRejectedVersion();
            ExpectedVersionId = CurrentVersionId;

            List<NodeHead.NodeVersion> irrelevantVersions;
            if (lastApprovedOrRejectedVersion == null)
            {
                irrelevantVersions = new List<NodeHead.NodeVersion>(VersionHistory);
                ExpectedVersion = ComputeNewVersion();
            }
            else
            {
                if (lastApprovedOrRejectedVersion.VersionId == CurrentVersionId)
                    ExpectedVersionId = 0;
                irrelevantVersions = GetNewerVersions(lastApprovedOrRejectedVersion);
                ExpectedVersion = GetNextVersion(lastApprovedOrRejectedVersion.VersionNumber, VersionStatus.Pending);
            }

            var irrelevantIds = irrelevantVersions.Select(x => x.VersionId);
            DeletableVersionIds.AddRange(irrelevantIds);
            DeletableVersionIds.Remove(CurrentVersionId); // remove this version
        }

        // for tests only, do not remove
        private void SetNodeHead(NodeHead head)
        {
            _nodeHead = head;
        }

        // ================================================================================================

        #region EnabledActions table
        private static readonly bool[][] EnabledActions = new bool[30][]
        {
            //                         Save, CheckOut, CheckIn,  Undo, Publish, Approve, Reject, SaveAndCheckIn
            // ======   Approving OFF 
            //              None
            new bool[8]{ /*Approved*/  true,     true,    false, false,   false,   false, false,           true }, // 0
            new bool[8]{ /*Locked  */  true,    false,     true,  true,   false,   false, false,          false }, // 1
            new bool[8]{ /*Draft   */  true,     true,    false, false,    true,   false, false,           true }, // 2
            new bool[8]{ /*Rejected*/  true,     true,    false, false,    true,   false, false,           true }, // 3
            new bool[8]{ /*Pending */  true,     true,    false, false,   false,   false, false,           true }, // 4
            // ------        Major
            new bool[8]{ /*Approved*/  true,     true,    false, false,   false,   false, false,           true }, // 5
            new bool[8]{ /*Locked  */  true,    false,     true,  true,   false,   false, false,          false }, // 6
            new bool[8]{ /*Draft   */  true,     true,    false, false,    true,   false, false,           true }, // 7
            new bool[8]{ /*Rejected*/  true,     true,    false, false,    true,   false, false,           true }, // 8
            new bool[8]{ /*Pending */  true,     true,    false, false,   false,   false, false,           true }, // 9
            // ------       Full
            new bool[8]{ /*Approved*/  true,     true,    false, false,   false,   false, false,           true }, // 10
            new bool[8]{ /*Locked  */  true,    false,     true,  true,    true,   false, false,          false }, // 11
            new bool[8]{ /*Draft   */  true,     true,    false, false,    true,   false, false,           true }, // 12
            new bool[8]{ /*Rejected*/  true,     true,    false, false,    true,   false, false,           true }, // 13
            new bool[8]{ /*Pending */  true,     true,    false, false,   false,   false, false,           true }, // 14

            // ======    Approving ON
            //              None
            new bool[8]{ /*Approved*/  true,     true,    false, false,   false,   false, false,           true }, // 15
            new bool[8]{ /*Locked  */  true,    false,     true,  true,   false,   false, false,          false }, // 16
            new bool[8]{ /*Draft   */  true,     true,    false, false,   false,   false, false,           true }, // 17
            new bool[8]{ /*Rejected*/  true,     true,    false, false,   false,   false, false,           true }, // 18
            new bool[8]{ /*Pending */  true,     true,    false, false,   false,    true,  true,           true }, // 19
            // ------        Major
            new bool[8]{ /*Approved*/  true,     true,    false, false,   false,   false, false,           true }, // 20
            new bool[8]{ /*Locked  */  true,    false,     true,  true,   false,   false, false,          false }, // 21
            new bool[8]{ /*Draft   */  true,     true,    false, false,   false,   false, false,           true }, // 22
            new bool[8]{ /*Rejected*/  true,     true,    false, false,   false,   false, false,           true }, // 23
            new bool[8]{ /*Pending */  true,     true,    false, false,   false,    true,  true,           true }, // 24
            // ------       Full
            new bool[8]{ /*Approved*/  true,     true,    false, false,   false,   false, false,           true }, // 25
            new bool[8]{ /*Locked  */  true,    false,     true,  true,    true,   false, false,          false }, // 26
            new bool[8]{ /*Draft   */  true,     true,    false, false,    true,   false, false,           true }, // 27
            new bool[8]{ /*Rejected*/  true,     true,    false, false,    true,   false, false,           true }, // 28
            new bool[8]{ /*Pending */  true,     true,    false, false,   false,    true,  true,           true }, // 29
        };
        #endregion

        private enum ActionValidationResult { Valid, Invalid, InvalidOnNewNode }
        private void AssertValidAction(StateAction stateAction)
        {
            InvalidContentActionReason reason;
            var result = ValidateAction(stateAction, out reason);

            if (result == ActionValidationResult.Invalid || result == ActionValidationResult.InvalidOnNewNode)
                throw new InvalidContentActionException(reason, this.Node.Path);
        }
        private ActionValidationResult ValidateAction(StateAction stateAction)
        {
            InvalidContentActionReason reason;

            return ValidateAction(stateAction, out reason);
        }
        private ActionValidationResult ValidateAction(StateAction stateAction, out InvalidContentActionReason reason)
        {
            if (!HasPermission(stateAction, out reason))
                return ActionValidationResult.Invalid;

            if (this.Node.SavingState != ContentSavingState.Finalized && (stateAction != StateAction.CheckIn && stateAction != StateAction.UndoCheckOut))
            {
                reason = InvalidContentActionReason.MultistepSaveInProgress;
                return ActionValidationResult.Invalid;
            }

            reason = InvalidContentActionReason.InvalidStateAction;

            if (this.Node.Id == 0)
            {
                if (stateAction == StateAction.Save || stateAction == StateAction.SaveAndCheckIn)
                    return ActionValidationResult.Valid;

                return ActionValidationResult.InvalidOnNewNode;
            }

            var action = 0;
            switch (stateAction)
            {
                case StateAction.Save: action = 0; break;
                case StateAction.CheckOut: action = 1; break;
                case StateAction.CheckIn: action = 2; break;
                case StateAction.UndoCheckOut: action = 3; break;
                case StateAction.Publish: action = 4; break;
                case StateAction.Approve: action = 5; break;
                case StateAction.Reject: action = 6; break;
                case StateAction.SaveAndCheckIn: action = 7; break;
                default:
                    throw new SnNotSupportedException("Unknown StateAction: " + stateAction);
            }

            var status = 0;
            var versionStatus = this.Node.Locked ? VersionStatus.Locked : this.CurrentVersion.Status;

            switch (versionStatus)
            {
                case VersionStatus.Approved: status = 0; break;
                case VersionStatus.Locked: status = 1; break;
                case VersionStatus.Draft: status = 2; break;
                case VersionStatus.Rejected: status = 3; break;
                case VersionStatus.Pending: status = 4; break;
                default:
                    throw new SnNotSupportedException("Unknown VersionStatus: " + this.CurrentVersion.Status);
            }

            var mode = 0;
            switch (this.VersioningMode)
            {
                case VersioningMode.None: mode = 0; break;
                case VersioningMode.Major: mode = 1; break;
                case VersioningMode.Full: mode = 2; break;
                default:
                    throw new SnNotSupportedException("Unknown VersioningMode: " + this.VersioningMode);
            }
            var approving = this.HasApproving ? 1 : 0;

            if (!EnabledActions[15 * approving + 5 * mode + status][action])
                return ActionValidationResult.Invalid;
            return ActionValidationResult.Valid;
        }

        private bool HasPermission(StateAction stateAction, out InvalidContentActionReason reason)
        {
            // set this as permission error will be the most common reason
            reason = InvalidContentActionReason.NotEnoughPermissions;

            if (this.Node.Id == 0)
            {
                var parent = this.Node.Parent;

                // for new content, creator needs to have AddNew permission for the parent
                if (!parent.Security.HasPermission(PermissionType.AddNew))
                    return false;

                // if this is a list type, the user must have a manage container permission for the parent
                if (!CheckManageListPermission(this.Node.NodeType, parent))
                    return false;

                // do not call this.Node.Security.HasPermission method because the node has not exist yet.
                switch (stateAction)
                {
                    case StateAction.Save:
                        return true;
                    case StateAction.CheckOut:
                    case StateAction.SaveAndCheckIn:
                    case StateAction.CheckIn:
                    case StateAction.UndoCheckOut:
                    case StateAction.Publish:
                    case StateAction.Approve:
                    case StateAction.Reject:
                        return false;
                    default:
                        throw new SnNotSupportedException("Unknown StateAction: " + stateAction);
                }
            }
            else
            {
                // otherwise the user needs to have Save permission for every action
                if (!this.Node.Security.HasPermission(PermissionType.Save))
                    return false;

                // if this is a list type, the user must have a manage container permission for the node
                if (!CheckManageListPermission(this.Node.NodeType, this.Node))
                    return false;

                var checkedOutByAnotherUser = IsCheckedOutByAnotherUser(this.Node);
                if (checkedOutByAnotherUser)
                    reason = InvalidContentActionReason.CheckedOutToSomeoneElse;

                switch (stateAction)
                {
                    case StateAction.Save:
                    case StateAction.CheckOut:
                    case StateAction.SaveAndCheckIn:
                    case StateAction.CheckIn:
                        return !checkedOutByAnotherUser;

                    case StateAction.UndoCheckOut:
                        // force and 'normal' undo operations
                        return !checkedOutByAnotherUser || HasForceUndoCheckOutRight(this.Node);

                    case StateAction.Publish:
                        return this.Node.Security.HasPermission(User.Current, PermissionType.Publish) && !checkedOutByAnotherUser;

                    case StateAction.Approve:
                    case StateAction.Reject:
                        return this.Node.Security.HasPermission(User.Current, PermissionType.Approve);

                    default:
                        throw new SnNotSupportedException("Unknown StateAction: " + stateAction);
                }
            }
        }

        public static bool HasCheckIn(Node node)
        {
            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.CheckIn) == ActionValidationResult.Valid;
        }
        public static bool HasCheckOut(GenericContent node)
        {
            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.CheckOut) == ActionValidationResult.Valid;
        }
        public static bool HasUndoCheckOut(GenericContent node)
        {
            if (HasForceUndoCheckOutRight(node))
                return false;

            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.UndoCheckOut) == ActionValidationResult.Valid;
        }
        public static bool HasSave(GenericContent node)
        {
            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.Save) == ActionValidationResult.Valid;
        }
        public static bool HasPublish(GenericContent node)
        {
            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.Publish) == ActionValidationResult.Valid;
        }
        public static bool HasApprove(GenericContent node)
        {
            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.Approve) == ActionValidationResult.Valid;
        }
        public static bool HasReject(GenericContent node)
        {
            var s = SavingAction.Create(node);
            return s.ValidateAction(StateAction.Reject) == ActionValidationResult.Valid;
        }

        public static bool CheckManageListPermission(NodeType nodeType, Node targetNode)
        {
            // silent error handling
            if (nodeType == null || targetNode == null)
                return true;

            return (!nodeType.IsInstaceOfOrDerivedFrom("ContentList") && !nodeType.IsInstaceOfOrDerivedFrom("Workspace")) || targetNode.Security.HasPermission(PermissionType.ManageListsAndWorkspaces);
        }
        
        public static void AssertVersioningAction(Content content, string actionName, bool throwGeneral = false)
        {
            if (string.IsNullOrEmpty(actionName))
                return;

            actionName = actionName.ToLower();

            if (!VERSIONING_ACTIONS.Contains(actionName))
                return;

            var sa = Create(content.ContentHandler);

            switch (actionName)
            {
                case "checkin": sa.AssertValidAction(StateAction.CheckIn); break;
                case "checkout": sa.AssertValidAction(StateAction.CheckOut); break;
                case "undocheckout": sa.AssertValidAction(StateAction.UndoCheckOut); break;
                case "forceundocheckout": sa.AssertValidAction(StateAction.UndoCheckOut); break;
                case "publish": sa.AssertValidAction(StateAction.Publish); break;
                case "approve": sa.AssertValidAction(StateAction.Approve); break;
                case "reject": sa.AssertValidAction(StateAction.Reject); break;
            }

            // if none of the above threw an exception, than 
            // throw a general exception 
            throw new InvalidContentActionException(InvalidContentActionReason.InvalidStateAction, content.Path);
        }

        // ================================================================================================ From ContentStateMachine

        public static bool HasForceUndoCheckOutRight(Node content)
        {
            return IsCheckedOutByAnotherUser(content) && content.Security.HasPermission(PermissionType.ForceCheckin);
        }
        private static bool IsCheckedOutByAnotherUser(Node content)
        {
            if (User.Current.Id == -1)
                return false;
            return content.Locked && content.LockedById != User.Current.Id;
        }

    }
}
