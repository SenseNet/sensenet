using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services
{
    [Serializable]
    [DebuggerDisplay("Ident: {Identity.NodeId}-{Identity.Name}, Propagates: {Propagates}, Permissions: {PermissionsToString()} ")]
    public class SnAccessControlEntry
    {
        public SnIdentity Identity { get; set; }
        public IEnumerable<SnPermission> Permissions { get; set; }
        public bool Propagates { get; set; }

        public string PermissionsToString()
        {
            var chars = new char[PermissionType.PermissionCount];
            foreach (var perm in Permissions)
            {
                var i = chars.Length - PermissionType.GetByName(perm.Name).Index - 1;
                if (perm.Allow)
                    chars[i] = '+';
                else if (perm.Deny)
                    chars[i] = '-';
                else
                    chars[i] = '_';
            }
            return new String(chars);
        }

        public static SnAccessControlEntry CreateEmpty(int principalId, bool propagates)
        {
            var perms = new List<SnPermission>();
            foreach (var permType in PermissionType.PermissionTypes)
                perms.Add(new SnPermission { Name = permType.Name });
            return new SnAccessControlEntry { Identity = SnIdentity.Create(principalId), Permissions = perms, Propagates = propagates };
        }
        public void GetPermissionBits(out ulong allowBits, out ulong denyBits)
        {
            allowBits = 0uL;
            denyBits = 0uL;
            foreach (var perm in this.Permissions)
            {
                var index = PermissionType.GetByName(perm.Name).Index;
                if (perm.Deny)
                    denyBits |= 1uL << index;
                else if (perm.Allow)
                    allowBits |= 1uL << index;
            }
        }
        public void SetPermissionsBits(ulong allowBits, ulong denyBits)
        {
            var index = 0;
            foreach (var perm in this.Permissions)
            {
                index = PermissionType.GetByName(perm.Name).Index;
                var mask = 1uL << index;
                perm.Deny = (denyBits & mask) != 0;
                perm.Allow = (allowBits & mask) == mask;
            }
        }

    }
}
