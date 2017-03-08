using SenseNet.Security;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Helps filtering permission query results for different kinds of identities.
    /// </summary>
    public enum IdentityKind { All, Users, Groups, OrganizationalUnits, UsersAndGroups, UsersAndOrganizationalUnits, GroupsAndOrganizationalUnits }

    public interface ISecurityMember : ISecurityIdentity
    {
        string Path { get; }
        bool IsInGroup(int securityGroupId);
    }
    public interface ISecurityContainer : ISecurityMember
    {
    }
}
