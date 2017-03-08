using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.Packaging.Steps
{
    public class BreakPermissionInheritance : Step
    {
        /// <summary>Repository path of the target content.</summary>
        [DefaultProperty]
        [Annotation("Repository path of the target content.")]
        public string Path { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(Path))
                throw new PackagingException(SR.Errors.InvalidParameters);

            using (new SystemAccount())
            {
                var content = Content.Load(Path);
                if (content == null)
                {
                    Logger.LogWarningMessage("Content not found: " + Path);
                    return;
                }
                ChangeInheritance(content);
            }
        }
        protected virtual void ChangeInheritance(Content content)
        {
            if (content.ContentHandler.IsInherited)
            {
                content.Security.BreakInheritance();
                Logger.LogMessage("Permission inheritance break successfully performed on " + content.Path);
            }
            else
            {
                Logger.LogMessage("Permission inheritance did not change because of a previous inheritance break on " + content.Path);
            }
        }
    }
    public class RemoveBreakPermissionInheritance : BreakPermissionInheritance
    {
        protected override void ChangeInheritance(Content content)
        {
            if (!content.ContentHandler.IsInherited)
            {
                content.Security.RemoveBreakInheritance();
                Logger.LogMessage("Permission inheritance break is removed from " + content.Path);
            }
            else
            {
                Logger.LogMessage("Permission inheritance did not change because '" + content.Path + "' already inherits its permissions.");
            }
        }
    }
    
    /// <summary>
    /// Base class for permission-related operations.
    /// </summary>
    public abstract class ModifyPermissions : Step
    {
        [DefaultProperty]
        [Annotation("Repository path of the content to change permissions on.")]
        public string Path { get; set; }

        /// <summary>"Repository path of the user or group to set permissions for." </summary>
        [Annotation("Repository path of the user or group to modify permissions for.")]
        public string Identity { get; set; }
    }

    /// <summary>
    /// Remove explicite permission entries from a content. If Identity is provided,
    /// only entries related to that identity will be removed.
    /// </summary>
    public class RemovePermissionEntries : ModifyPermissions
    {
        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(Path))
                throw new PackagingException(SR.Errors.InvalidParameters);

            using (new SystemAccount())
            {
                var path = (string) context.ResolveVariable(Path);
                var content = Content.Load(path);
                var aclEditor = SecurityHandler.CreateAclEditor();
                if (content == null)
                {
                    Logger.LogWarningMessage("Content not found: " + path);
                    return;
                }

                if (string.IsNullOrEmpty(Identity))
                {
                    aclEditor.RemoveExplicitEntries(content.Id);
                }
                else
                {
                    var identity = Node.LoadNode(Identity) as ISecurityMember;
                    if (identity == null)
                    {
                        Logger.LogWarningMessage("Identity not found: " + Identity);
                        return;
                    }
                    var pbitmask = PermissionBitMask.All;
                    aclEditor.Reset(content.Id, identity.Id, false, pbitmask);
                    aclEditor.Reset(content.Id, identity.Id, true, pbitmask);
                }
                aclEditor.Apply();
            }
        }
    }

    /// <summary>
    /// Sets permission entries on a content for an identity. This step will clear all 
    /// other permissions that are not defined either in the Allow or the Deny property.
    /// If you only want to modify existing permissions, please use the EditPermissions
    /// step instead.
    /// </summary>
    public class SetPermissions : ModifyPermissions
    {
        /// <summary>"Comma-separated list of permissions to allow."</summary>
        [Annotation("Comma-separated list of permissions to allow.")]
        public string Allow { get; set; }

        /// <summary>"Comma-separated list of permissions to deny."</summary>
        [Annotation("Comma-separated list of permissions to deny.")]
        public string Deny { get; set; }

        /// <summary>Permission entry should be local only (not inherited).</summary>
        [Annotation("Permission entry should be local only (not inherited).")]
        public bool LocalOnly { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            CheckParameters();

            using (new SystemAccount())
            {
                var path = (string)context.ResolveVariable(Path);
                var content = Content.Load(path);
                if (content == null)
                {
                    Logger.LogWarningMessage("Content not found: " + path);
                    return;
                }

                var identity = Node.LoadNode(Identity) as ISecurityMember;
                if (identity == null)
                {
                    Logger.LogWarningMessage("Identity not found: " + Identity);
                    return;
                }

                var aclEditor = SecurityHandler.CreateAclEditor();
                var permissionBitMmask = new PermissionBitMask
                {
                    AllowBits = SecurityHandler.GetPermissionMask(GetPermissionTypes(Allow)),
                    DenyBits = SecurityHandler.GetPermissionMask(GetPermissionTypes(Deny))
                };

                ChangePermissions(content.Id, identity.Id, aclEditor, permissionBitMmask);

                aclEditor.Apply(); 
            }
        }

        protected virtual void CheckParameters()
        {
            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(Identity) || (string.IsNullOrEmpty(Allow) && string.IsNullOrEmpty(Deny)))
                throw new PackagingException(SR.Errors.InvalidParameters);
        }

        protected virtual void ChangePermissions(int contentId, int identityId, SnAclEditor aclEditor, PermissionBitMask permissionBitMmask)
        {
            aclEditor.Reset(contentId, identityId, LocalOnly, PermissionBitMask.All);
            aclEditor.Set(contentId, identityId, LocalOnly, permissionBitMmask);
        }

        protected IEnumerable<PermissionType> GetPermissionTypes(string permissionNames)
        {
            if (string.IsNullOrEmpty(permissionNames))
                return new PermissionType[0];

            var permissionTypeNames = permissionNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var permissionTypes = permissionTypeNames.Select(ptn => PermissionType.GetByName(ptn))
                .Where(pt => pt != null).ToArray();

            if (permissionTypeNames.Length > permissionTypes.Length)
                throw new PackagingException("Invalid permission types: " + permissionNames);

            return permissionTypes;
        }
    }

    /// <summary>
    /// Modifies existing permission entries on a content for an identity. This step will 
    /// clear only those permissions that are defined in the Clear property.
    /// </summary>
    public class EditPermissions : SetPermissions
    {
        /// <summary>Clear permissions on the target content before settings.</summary>
        [Annotation("Comma-separated list of permissions to clear.")]
        public string Clear { get; set; }

        protected override void CheckParameters()
        {
            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(Identity) || (string.IsNullOrEmpty(Allow) && string.IsNullOrEmpty(Deny) && string.IsNullOrEmpty(Clear)))
                throw new PackagingException(SR.Errors.InvalidParameters);
        }

        protected override void ChangePermissions(int contentId, int identityId, SnAclEditor aclEditor, PermissionBitMask permissionBitMmask)
        {
            if (!string.IsNullOrEmpty(Clear))
            {
                var clearBits = SecurityHandler.GetPermissionMask(GetPermissionTypes(Clear));
                aclEditor.Reset(contentId, identityId, LocalOnly,
                    new PermissionBitMask { AllowBits = clearBits, DenyBits = clearBits });
            }

            aclEditor.Set(contentId, identityId, LocalOnly, permissionBitMmask);
        }
    }
}
