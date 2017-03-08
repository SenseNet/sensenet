using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class LockHandler
    {
        private Node _node;

        // ====================================================================== Properties

        public Node Node
        {
            get { return _node; }
        }

        public bool Locked
        {
            get
            {
                return _node.Version.Status == VersionStatus.Locked;
            }
        }

        public IUser LockedBy
        {
            get
            {
                if (this.Locked)
                {
                    return Node.LoadNode(_node.LockedById) as IUser;
                }
                else
                {
                    return null;
                }
            }
        }

        public string ETag
        {
            get
            {
                return _node.ETag;
            }
            set
            {
                _node.ETag = value;
            }
        }

        public int LockType
        {
            get
            {
                return _node.LockType;
            }
            set
            {
                _node.LockType = value;
            }
        }

        public int LockTimeout
        {
            get
            {
                return _node.LockTimeout;
            }
        }

        public DateTime LockDate
        {
            get
            {
                return _node.LockDate;
            }
        }

        public string LockToken
        {
            get
            {
                return _node.LockToken;
            }
        }

        public DateTime LastLockUpdate
        {
            get
            {
                return _node.LastLockUpdate;
            }
        }

        // ====================================================================== Construction

        public LockHandler(Node node)
        {
            _node = node;
        }

        // ====================================================================== Methods
        public void Lock()
        {
            Lock(RepositoryEnvironment.DefaultLockTimeout);
        }
        public void Lock(int timeout)
        {
            Lock(timeout, VersionRaising.None);
        }
        public void Lock(VersionRaising versionRaising)
        {
            Lock(RepositoryEnvironment.DefaultLockTimeout, versionRaising);
        }
        public void Lock(int timeout, VersionRaising versionRaising)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Node.LockHandler.Lock: NodeId:{0}, VersionId:{1}, Version:{2}, VersionRaising:{3}, Timeout:{4}.", _node.Id, _node.VersionId, _node.Version, versionRaising, timeout))
            {
                if (!this.Locked)
                {
                    _node.LockToken = Guid.NewGuid().ToString();
                    _node.LockedById = AccessProvider.Current.GetCurrentUser().Id;
                    _node.LockDate = DateTime.UtcNow;
                    _node.LastLockUpdate = DateTime.UtcNow;
                    _node.LockTimeout = timeout;

                    _node.Save(versionRaising, VersionStatus.Locked);
                }
                else
                {
                    RefreshLock(versionRaising);
                }
                op.Successful = true;
            }
        }

        public void RefreshLock()
        {
            RefreshLock(RepositoryEnvironment.DefaultLockTimeout, VersionRaising.None);
        }

        public void RefreshLock(VersionRaising versionRaising)
        {
            RefreshLock(RepositoryEnvironment.DefaultLockTimeout, versionRaising);
        }
        public void RefreshLock(int timeout, VersionRaising versionRaising)
        {
            IUser lockUser = this.LockedBy;
            if (lockUser.Id == AccessProvider.Current.GetCurrentUser().Id)
                RefreshLock(this.LockToken, timeout, versionRaising);
            else
                throw new SenseNetSecurityException(this.Node.Id, "Node is locked by another user");
        }
        public void RefreshLock(string token, int timeout, VersionRaising versionRaising)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Node.LockHandler.RefreshLock: NodeId:{0}, VersionId:{1}, Version:{2}, VersionRaising:{3}, Locktoken:{4}, Timeout:{5}."
                , _node.Id, _node.VersionId, _node.Version, versionRaising, token, timeout))
            {
                if (this.Locked && this.LockToken == token)
                {
                    _node.LastLockUpdate = DateTime.UtcNow;
                    _node.LockTimeout = timeout;
                    _node.Save(versionRaising, VersionStatus.Locked);
                }
                else
                {
                    if (Locked)
                        throw new LockedNodeException(this, "Node is locked but passed locktoken is invalid.");
                    else
                        throw new LockedNodeException(this, "Node is not locked or lock timed out");
                }
                op.Successful = true;
            }
        }

        public void Unlock(VersionStatus versionStatus, VersionRaising versionRaising)
        {
            if (this.LockedBy.Id != AccessProvider.Current.GetCurrentUser().Id)
                this.Node.Security.Assert("Node is locked by another user", PermissionType.ForceCheckin);
            this.Unlock(this.LockToken, versionStatus, versionRaising);
        }
        public void Unlock(string token, VersionStatus versionStatus, VersionRaising versionRaising)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Node.LockHandler.Unlock: NodeId:{0}, VersionId:{1}, Version:{2}, VersionRaising:{3}, Locktoken:{4}.", _node.Id, _node.VersionId, _node.Version, versionRaising, token))
            {
                if (Locked && this.LockToken == token)
                {
                    _node.LockedById = 0;
                    _node.LockToken = string.Empty;
                    _node.LockTimeout = 0;
                    _node.LockDate = new DateTime(1800, 1, 1);
                    _node.LastLockUpdate = new DateTime(1800, 1, 1);
                    _node.LockType = 0;
                    _node.Save(versionRaising, versionStatus);
                }
                else
                {
                    if (Locked)
                        throw new LockedNodeException(this, "Node is not locked or lock timed out");
                    else
                        throw new LockedNodeException(this, "Node is not locked or lock timed out");
                }
                op.Successful = true;
            }
        }

        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.DefaultLockTimeout instead.")]
        public static int DefaultLockTimeOut => RepositoryEnvironment.DefaultLockTimeout;

        /// <summary>
        /// Current user transfers the lock ownership to the target user.
        /// If target user is null, it will be the current user.
        /// Current user must have ForceCheckin permission.
        /// </summary>
        /// <param name="targetUser">Target user or null.</param>
        public void TakeLockOver(IUser targetUser)
        {
            _node.Security.Assert(PermissionType.ForceCheckin);

            if (targetUser == null)
                targetUser = AccessProvider.Current.GetCurrentUser();

            if (targetUser.Id == this.Node.LockedById)
                return;

            if (!_node.Locked)
                throw new ApplicationException("TakeLockOver is invalid action if the content is not locked.");

            if (!_node.Security.HasPermission(targetUser, PermissionType.Save))
                throw new ApplicationException("Cannot transfer the document's checked out state to the target user because he or she does not have enough permissions to save the document.");

            var oldLockerId = this.Node.LockedById;

            var auditProperties = new Dictionary<string, object>
            {
                { "Id", _node.Id },
                { "Path", _node.Path },
                { "OldLockerId", oldLockerId },
                { "NewLockerId", targetUser.Id }
            };
            using (var audit = new AuditBlock(AuditEvent.LockTakenOver, "Trying to take lock over.", auditProperties))
            {
                using (var op = SnTrace.ContentOperation.StartOperation("Node.LockHandler.TakeLockOver: NodeId:{0}, VersionId:{1}, Version:{2}, UserId:{3}, UserName:{4}.", _node.Id, _node.VersionId, _node.Version, targetUser.Id, targetUser.Username))
                {
                    if (_node.LockedById != targetUser.Id)
                    {
                        _node.LockToken = Guid.NewGuid().ToString();
                        _node.LockedById = targetUser.Id;
                        _node.LockDate = DateTime.UtcNow;
                        _node.LastLockUpdate = DateTime.UtcNow;
                        _node.LockTimeout = RepositoryEnvironment.DefaultLockTimeout;

                        _node.Save(VersionRaising.None, VersionStatus.Locked, true);
                    }

                    SnLog.WriteAudit(AuditEvent.LockTakenOver, auditProperties);

                    op.Successful = true;
                }
                audit.Successful = true;
            }
        }
    }
}