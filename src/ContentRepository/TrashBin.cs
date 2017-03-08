using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class TrashBin : Workspace
    {
        public const string TrashBinPath = "/Root/Trash";


        public TrashBin(Node parent) : this(parent, null) { }
        public TrashBin(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected TrashBin(NodeToken tk) : base(tk) { }


        [RepositoryProperty("MinRetentionTime", RepositoryDataType.Int)]
        public int MinRetentionTime
        {
            get { return (int)base.GetProperty("MinRetentionTime"); }
            set { this["MinRetentionTime"] = value; }
        }

        [RepositoryProperty("SizeQuota", RepositoryDataType.Int)]
        public int SizeQuota
        {
            get { return (int)base.GetProperty("SizeQuota"); }
            set { this["SizeQuota"] = value; }
        }

        [RepositoryProperty("BagCapacity", RepositoryDataType.Int)]
        public int BagCapacity
        {
            get { return (int)base.GetProperty("BagCapacity"); }
            set { this["BagCapacity"] = value; }
        }


        public static TrashBin Instance
        {
            get
            {
                var bin = Node.Load<TrashBin>(TrashBinPath);

                if (bin == null)
                    SnTrace.Repository.Write("Trashbin node not found under /Root/Trash, trashbin functionality unavailable.");

                return bin;
            }
        }


        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "MinRetentionTime":
                    return this.MinRetentionTime;
                case "SizeQuota":
                    return this.SizeQuota;
                case "BagCapacity":
                    return this.BagCapacity;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "MinRetentionTime":
                    this.MinRetentionTime = (int)value;
                    break;
                case "SizeQuota":
                    this.SizeQuota = (int)value;
                    break;
                case "BagCapacity":
                    this.BagCapacity = (int)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        public override void Save(SavingMode mode)
        {
            AssertTrashBinPath();
            base.Save(mode);
        }

        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

        public override void Delete()
        {
            ForceDelete();
        }

        // ====================================================================== GenericContent trash methods

        public static bool IsInTrash(GenericContent n)
        {
            // currently this is a simple path check, but
            // in the future, when local trash bins may exist,
            // it will be more complicated
            return n != null && (n.Path + "/").StartsWith(TrashBinPath + "/");
        }

        public static bool DeleteNode(GenericContent n)
        {
            if (Instance != null && Instance.IsActive && n.IsTrashable)
            {
                if (Instance.BagCapacity > 0 && n.NodesInTree > Instance.BagCapacity)
                    throw new ApplicationException("Node tree size exceeds trash bag limit, use ForceDelete to purge physically.");

                TrashBag.BagThis(n);
            }
            else
            {
                ForceDelete(n);
            }
            return true;
        }

        public static void ForceDelete(GenericContent n)
        {
            SnTrace.Repository.Write("Trashbin: Finally deleting from Repository. NodePath:{0}", n.Path);
            n.ForceDelete();
        }

        // ====================================================================== Purge and Restore

        public static void Purge()
        {
            foreach (TrashBag b in Instance.Children.OfType<TrashBag>().Where(n => (n.IsPurgeable && n.Security.HasSubTreePermission(PermissionType.Delete))))
                ForceDelete(b);
        }

        public static void Restore(TrashBag trashBag)
        {
            Restore(trashBag, trashBag.OriginalPath, false);
        }

        public static void Restore(TrashBag trashBag, bool addNewName)
        {
            Restore(trashBag, trashBag.OriginalPath, addNewName);
        }

        public static void Restore(TrashBag trashBag, string targetPath)
        {
            Restore(trashBag, targetPath, false);
        }

        public static void Restore(TrashBag trashBag, string targetPath, bool addNewName)
        {
            if (trashBag == null || string.IsNullOrEmpty(targetPath))
                throw new RestoreException(RestoreResultType.Nonedefined);

            targetPath = targetPath.TrimEnd(new [] {'/'});

            var node = trashBag.DeletedContent;
            if (node == null)
                throw new InvalidOperationException("TrashBag is empty");

            var targetContentPath = RepositoryPath.Combine(targetPath, node.Name);
            var targetParent = Node.Load<GenericContent>(targetPath);
            if (targetParent == null)
            {
                throw new RestoreException(RestoreResultType.NoParent,
                    RepositoryPath.GetParentPath(targetPath));
            }       

            // assert permissions
            if (!targetParent.Security.HasPermission(PermissionType.Open))
                throw new RestoreException(RestoreResultType.PermissionError, targetContentPath);

            // target type check: ContentTypes field
            AssertRestoreContentType(targetParent, node);

            if (Node.Exists(targetContentPath))
            {
                var newName = ContentNamingProvider.IncrementNameSuffixToLastName(node.Name, targetParent.Id);
                targetContentPath = RepositoryPath.Combine(targetPath, newName);

                if (addNewName)
                {
                    try
                    {
                        // there is no other way right now (rename and move cannot be done at the same time)
                        node.Name = newName;
                        node.Save();
                    }
                    catch (SenseNetSecurityException ex)
                    {
                        SnLog.WriteException(ex);
                        throw new RestoreException(RestoreResultType.PermissionError,
                            targetContentPath, ex);
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                        throw new RestoreException(RestoreResultType.UnknownError,
                            targetContentPath, ex);
                    }
                }
                else
                {
                    throw new RestoreException(RestoreResultType.ExistingName,
                            targetContentPath);
                }
            }

            var originalUser = User.Current;

            try
            {
                node.MoveTo(targetParent);
                
                AccessProvider.Current.SetCurrentUser(User.Administrator);
                
                trashBag.KeepUntil = DateTime.Today.AddDays(-1);
                trashBag.ForceDelete();
            }
            catch (SenseNetSecurityException ex)
            {
                SnLog.WriteException(ex);
                throw new RestoreException(RestoreResultType.PermissionError,
                    targetContentPath, ex);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                throw new RestoreException(RestoreResultType.UnknownError,
                            targetContentPath, ex);
            }
            finally
            {
                AccessProvider.Current.SetCurrentUser(originalUser);
            }
        }

        // ====================================================================== Helper methods

        private static void AssertRestoreContentType(GenericContent targetParent, Node node)
        {
            if (targetParent == null)
                throw new ArgumentNullException("targetParent");
            if (node == null)
                throw new ArgumentNullException("node");

            if (!(targetParent is IFolder))
                throw new RestoreException(RestoreResultType.ForbiddenContentType, targetParent.Path);

            var ctNames = targetParent.GetAllowedChildTypeNames().ToArray();

            if (ctNames.Length > 0 && !ctNames.Any(ctName => ctName == node.NodeType.Name))
                throw new RestoreException(RestoreResultType.ForbiddenContentType, targetParent.Path);
        }

        private void AssertTrashBinPath()
        {
            if (!Name.Equals("Trash") || !this.ParentPath.Equals(Repository.RootPath))
                throw new InvalidOperationException("A TrashBin instance can only be saved under the global location /Root/Trash");
        }
    }
}
