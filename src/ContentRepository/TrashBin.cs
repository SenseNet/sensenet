using System;
using System.Linq;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
// ReSharper disable ArrangeStaticMemberQualifier

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A Content handler for a Content that implements the Trash Bin functionality.
    /// </summary>
    [ContentHandler]
    public class TrashBin : Workspace
    {
        /// <summary>
        /// Defines a constant for the path of the centralized Trash Bin in the Content Repository.
        /// </summary>
        public const string TrashBinPath = "/Root/Trash";


        /// <summary>
        /// Initializes a new instance of the <see cref="TrashBin"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public TrashBin(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="TrashBin"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public TrashBin(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="TrashBin"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected TrashBin(NodeToken tk) : base(tk) { }

        /// <summary>
        /// Gets or sets the count of days the content should stay in the trash before deleting it permanently.
        /// If the value is greater than 0, users (or any automatism) cannot remove the content from the 
        /// trash before the expiration date. Changing this value does not effect previously deleted content.
        /// Persisted as <see cref="RepositoryDataType.Int"/>.
        /// </summary>
        [RepositoryProperty("MinRetentionTime", RepositoryDataType.Int)]
        public int MinRetentionTime
        {
            get => (int)base.GetProperty("MinRetentionTime");
            set => this["MinRetentionTime"] = value;
        }

        /// <summary>
        /// Gets or sets the size quota in megabytes that can be stored in the trash. 
        /// This value is only a UI hint, it does not effect deleting content. 
        /// If the size is exceeded the Trash Bin main page will display a message about how much space is used. 
        /// The administrator should take care of purging content from the trash manually.
        /// </summary>
        [RepositoryProperty("SizeQuota", RepositoryDataType.Int)]
        public int SizeQuota
        {
            get => (int)base.GetProperty("SizeQuota");
            set => this["SizeQuota"] = value;
        }

        /// <summary>
        /// Gets or sets the maximum acceptable count of content in a <see cref="TrashBag"/>.
        /// The value 0 means no restriction.
        ///  </summary>
        [RepositoryProperty("BagCapacity", RepositoryDataType.Int)]
        public int BagCapacity
        {
            get => (int)base.GetProperty("BagCapacity");
            set => this["BagCapacity"] = value;
        }


        /// <summary>
        /// Gets the centralized <see cref="TrashBin"/>.
        /// Note that this property always returns a new instance.
        /// </summary>
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


        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "MinRetentionTime":
                    return MinRetentionTime;
                case "SizeQuota":
                    return SizeQuota;
                case "BagCapacity":
                    return BagCapacity;
                default:
                    return base.GetProperty(name);
            }
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "MinRetentionTime":
                    MinRetentionTime = (int)value;
                    break;
                case "SizeQuota":
                    SizeQuota = (int)value;
                    break;
                case "BagCapacity":
                    BagCapacity = (int)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        /// <inheritdoc />
        public override void Save(SavingMode mode)
        {
            AssertTrashBinPath();
            base.Save(mode);
        }

        /// <inheritdoc />
        /// <remarks>In this case returns false.</remarks>
        public override bool IsTrashable => false;

        /// <inheritdoc />
        public override void Delete()
        {
            ForceDelete();
        }

        // ====================================================================== GenericContent trash methods

        /// <summary>
        /// Tool method that returns true if the given content is found in the trash bin.
        /// </summary>
        /// <param name="n">The <see cref="GenericContent"/> instance to check.</param>
        public static bool IsInTrash(GenericContent n)
        {
            // currently this is a simple path check, but
            // in the future, when local trash bins may exist,
            // it will be more complicated
            return n != null && (n.Path + "/").StartsWith(TrashBinPath + "/");
        }

        /// <summary>
        /// Tool method that deletes the given Content depending on the state of the Trash Bin and Content.
        /// The Trash Bin is used if the Trash Bin is active, the value of the content's IsTrashable property is true
        /// and the <see cref="TrashBag"/> of the content is not too big (see <see cref="BagCapacity"/> property).
        /// If any condition is false, the Content will be deleted permanently.
        /// </summary>
        /// <param name="n">The <see cref="GenericContent"/> instance that will be deleted.</param>
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
                n.ForceDelete();
            }
            return true;
        }

        /// <summary>
        /// Deletes the content permanently. This method is obsolete, use node.ForceDelete instead.
        /// </summary>
        [Obsolete("Use ForceDelete method on the given instance.")]
        public static void ForceDelete(GenericContent n)
        {
            SnTrace.Repository.Write("Trashbin: Finally deleting from Repository. NodePath:{0}", n.Path);
            n.ForceDelete();
        }

        // ====================================================================== Purge and Restore

        /// <summary>
        /// Deletes the outdated <see cref="TrashBag"/>s (see <see cref="TrashBag.IsPurgeable"/> property).
        /// Caller user needs to have <see cref="PermissionType.Delete"/> permission to 
        /// the <see cref="TrashBag"/>'s whole subtree, otherwise the bag will not be deleted.
        /// </summary>
        public static void Purge()
        {
            foreach (TrashBag b in Instance.Children.OfType<TrashBag>().Where(n => (n.IsPurgeable && n.Security.HasSubTreePermission(PermissionType.Delete))))
                b.ForceDelete();
        }

        /// <summary>
        /// Undeletes the Content in the given <see cref="TrashBag"/>.
        /// </summary>
        /// <param name="trashBag">The <see cref="TrashBag"/> that will be restored.</param>
        public static void Restore(TrashBag trashBag)
        {
            Restore(trashBag, trashBag.OriginalPath, false);
        }

        /// <summary>
        /// Undeletes the Content in the given <see cref="TrashBag"/> with the specified name.
        /// Use this if the name of the restored item should be different than the original name.
        /// </summary>
        /// <param name="trashBag">The <see cref="TrashBag"/> that will be restored.</param>
        /// <param name="addNewName">The new name of the restored item.</param>
        public static void Restore(TrashBag trashBag, bool addNewName)
        {
            Restore(trashBag, trashBag.OriginalPath, addNewName);
        }

        /// <summary>
        /// Undeletes the Content in the given <see cref="TrashBag"/> into the specified container.
        /// </summary>
        /// <param name="trashBag">The <see cref="TrashBag"/> that will be restored.</param>
        /// <param name="targetPath">The path of the container that will contain the restored item.</param>
        public static void Restore(TrashBag trashBag, string targetPath)
        {
            Restore(trashBag, targetPath, false);
        }

        /// <summary>
        /// Undeletes the Content in the given <see cref="TrashBag"/> into the specified container.
        /// Use if the name of the restored item is different from the name when deleted.
        /// </summary>
        /// <param name="trashBag">The <see cref="TrashBag"/> that will be restored.</param>
        /// <param name="targetPath">The path of the container that will contain the restored item.</param>
        /// <param name="addNewName">The new name of the restored item.</param>
        public static void Restore(TrashBag trashBag, string targetPath, bool addNewName)
        {
            if (trashBag == null || string.IsNullOrEmpty(targetPath))
                throw new RestoreException(RestoreResultType.Nonedefined);

            targetPath = targetPath.TrimEnd('/');

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
                node.MoveToTrash(targetParent);
                
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
                throw new ArgumentNullException(nameof(targetParent));
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (!(targetParent is IFolder))
                throw new RestoreException(RestoreResultType.ForbiddenContentType, targetParent.Path);

            var ctNames = targetParent.GetAllowedChildTypeNames().ToArray();

            if (ctNames.Length > 0 && ctNames.All(ctName => ctName != node.NodeType.Name))
                throw new RestoreException(RestoreResultType.ForbiddenContentType, targetParent.Path);
        }

        private void AssertTrashBinPath()
        {
            if (!Name.Equals("Trash") || !ParentPath.Equals(Repository.RootPath))
                throw new InvalidOperationException("A TrashBin instance can only be saved under the global location /Root/Trash");
        }
    }
}
