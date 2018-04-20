using SenseNet.Security;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Helps filtering permission query results for different kinds of identities.
    /// </summary>
    public enum IdentityKind { All, Users, Groups, OrganizationalUnits, UsersAndGroups, UsersAndOrganizationalUnits, GroupsAndOrganizationalUnits }

    /// <summary>
    /// Defines an interface for representing an identity or group that is stored in the sensenet Content Repository.
    /// </summary>
    public interface ISecurityMember : ISecurityIdentity
    {
        /// <summary>
        /// Gets the path in the repository.
        /// </summary>
        string Path { get; }
        /// <summary>
        /// Returns true if the represented instance is a member of the group identified by the given securityGroupId.
        /// This should be transitive, which means it will return true even if the identity is member of the
        /// provided group through other groups.
        /// </summary>
        /// <param name="securityGroupId">Identifier of a group.</param>
        bool IsInGroup(int securityGroupId);
    }

    /// <summary>
    /// Defines an interface for representing a security container that can contain members.
    /// </summary>
    public interface ISecurityContainer : ISecurityMember
    {
    }
}
