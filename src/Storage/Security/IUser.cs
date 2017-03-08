using SenseNet.Security;
using System;

namespace SenseNet.ContentRepository.Storage.Security
{
    public interface IUser : ISecurityMember, System.Security.Principal.IIdentity, ISecurityUser
    {
        bool Enabled { get; set; }
        string Domain { get; }
        string Email { get; set; }
        string FullName { get; set; }
        string Password { set; }
        string PasswordHash { get; set; }
        string Username { get; } // = Domain + "\" + Node.Name

        bool IsInGroup(IGroup group);
        bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit);
        bool IsInContainer(ISecurityContainer container);

        MembershipExtension MembershipExtension { get; set; }
    }
}