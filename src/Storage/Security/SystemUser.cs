using System;
using System.Collections.Generic;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Security
{
    internal sealed class SystemUser : IUser
    {
        private IUser _originalUser;

        public IUser OriginalUser
        {
            get { return _originalUser; }
        }

        internal SystemUser(IUser originalUser)
        {
            _originalUser = originalUser;
        }

        // ================================================================================================== IUser Members

        public bool Enabled
        {
            get { return true; }
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Interface implementation")]
        public string Domain
        {
            get { return IdentityManagement.BuiltInDomainName; }
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        public string Email
        {
            get { throw new InvalidOperationException("You cannot get the Email property of the SYSTEM user."); }
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        public string FullName
        {
            get { return "SYSTEM"; }
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        public string Password
        {
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        public string PasswordHash
        {
            get { throw new InvalidOperationException("You cannot get the PasswordHash property of the SYSTEM user."); }
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Interface implementation")]
        public string Username
        {
            get { return "SYSTEM"; }
            set { throw new InvalidOperationException("You cannot set a property of the SYSTEM user."); }
        }
        public bool IsInGroup(IGroup group)
        {
            throw new InvalidOperationException("The SYSTEM user is not a member of any group.");
        }
        public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit)
        {
            throw new InvalidOperationException("The SYSTEM user is not a member of any organizational unit.");
        }
        public bool IsInContainer(ISecurityContainer container)
        {
            throw new InvalidOperationException("The SYSTEM user is not a member of any container (group or organizational unit).");
        }

        public DateTime? LastLoggedOut { get; set; }

        public bool IsInGroup(int securityGroupId)
        {
            throw new InvalidOperationException("The SYSTEM user is not a member of any role (group or organizational unit).");
        }

        public MembershipExtension MembershipExtension { get { return null; } set { } }

        // ================================================================================================== ISecurityMember Members

        public int Id
        {
            get { return -1; }
        }
        public string Path
        {
            get { throw new InvalidOperationException("You cannot get the Path property of the SYSTEM user."); }
        }

        // ================================================================================================== IIdentity Members

        public string AuthenticationType
        {
            get { return "Portal"; }
        }
        public bool IsAuthenticated
        {
            get { return true; }
        }
        public string Name
        {
            get { return "SYSTEM"; }
        }

        // ================================================================================================== SenseNet.Security.ISecurityUser

        public IEnumerable<int> GetDynamicGroups(int entityId)
        {
            return Empty.IntArray;
        }

        // ================================================================================================== Counter

        private int _counter;

        internal void Increment()
        {
            _counter++;
        }
        internal bool Decrement()
        {
            return --_counter >= 0;
        }

    }
}