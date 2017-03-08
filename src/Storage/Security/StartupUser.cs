using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Security
{
    internal sealed class StartupUser : IUser
    {

        // ================================================================================================== IUser Members

        public bool Enabled
        {
            get { return true; }
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Interface implementation")]
        public string Domain
        {
            get { return IdentityManagement.BuiltInDomainName; }
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        public string Email
        {
            get { throw new InvalidOperationException("You cannot get the Email property of the STARTUP user."); }
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        public string FullName
        {
            get { return "STARTUP"; }
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        public string Password
        {
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        public string PasswordHash
        {
            get { throw new InvalidOperationException("You cannot get the PasswordHash property of the STARTUP user."); }
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Interface implementation")]
        public string Username
        {
            get { return "STARTUP"; }
            set { throw new InvalidOperationException("You cannot set a property of the STARTUP user."); }
        }
        public bool IsInGroup(IGroup group)
        {
            throw new InvalidOperationException("The STARTUP user is not a member of any group.");
        }
        public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit)
        {
            throw new InvalidOperationException("The STARTUP user is not a member of any organizational unit.");
        }
        public bool IsInContainer(ISecurityContainer container)
        {
            throw new InvalidOperationException("The STARTUP user is not a member of any container (group or organizational unit).");
        }
        public bool IsInGroup(int securityGroupId)
        {
            throw new InvalidOperationException("The STARTUP user is not a member of any role (group or organizational unit).");
        }

        public MembershipExtension MembershipExtension { get { return null; } set { } }

        // ================================================================================================== ISecurityMember Members

        public int Id => Identifiers.StartupUserId;

        public string Path
        {
            get { throw new InvalidOperationException("You cannot get the Path property of the STARTUP user."); }
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
            get { return "STARTUP"; }
        }

        // ================================================================================================== SenseNet.Security.ISecurityUser

        public IEnumerable<int> GetDynamicGroups(int entityId)
        {
            return Empty.IntArray;
        }
    }
}
