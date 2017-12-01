using SenseNet.Security;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Helps filtering permission query results for different kinds of identities.
    /// </summary>
    public enum IdentityKind { All, Users, Groups, OrganizationalUnits, UsersAndGroups, UsersAndOrganizationalUnits, GroupsAndOrganizationalUnits }

    /// <summary>
    /// Defines an interface for representing member or group that are stored in the sensenet repository.
    /// </summary>
    public interface ISecurityMember : ISecurityIdentity
    {
        /// <summary>
        /// Gets the path in the repository.
        /// </summary>
        string Path { get; }
        /// <summary>
        /// Gets true if the represented instance is a member of the group identified by the given securityGroupId.
        /// </summary>
        /// <param name="securityGroupId">Id of the queryed group.</param>
        bool IsInGroup(int securityGroupId);
    }

    /// <summary>
    /// Defines an interface for representing a member that can contain other members.
    /// </summary>
    public interface ISecurityContainer : ISecurityMember
    {
    }
}
