using SenseNet.Security;
using System;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Define an interface for classes that encapsulate metadata and functionality of the sensenet repository's users.
    /// </summary>
    public interface IUser : ISecurityMember, System.Security.Principal.IIdentity, ISecurityUser
    {
        /// <summary>
        /// Gets or sets a value that describes whether the represented user can log in or not.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Gets the domain name of the represented user.
        /// </summary>
        string Domain { get; }
        /// <summary>
        /// Gets or sets the e-mail address of the represented user.
        /// </summary>
        string Email { get; set; }
        /// <summary>
        /// Gets or sets the full name of the represented user.
        /// </summary>
        string FullName { get; set; }
        /// <summary>
        /// Sets the password of the represented user.
        /// WARNING!. Any storage of this value is forbidden. The value must be hashed 
        /// immediately and the hash value stored in the PasswordHash property.
        /// </summary>
        string Password { set; }
        /// <summary>
        /// Sets the hash value of the represented user's password.
        /// </summary>
        string PasswordHash { get; set; }
        /// <summary>
        /// Gets the login name of the represented user.
        /// </summary>
        string Username { get; } // = Domain + "\" + Node.Name

        /// <summary>
        /// Gets true if the represented instance is a member of the given group.
        /// </summary>
        /// <param name="group">An instance of the <see cref="IGroup"/> implementation.</param>
        bool IsInGroup(IGroup group);
        /// <summary>
        /// Gets true if the represented instance is a member of the given organizational unit.
        /// </summary>
        /// <param name="orgUnit">An instance of the <see cref="IOrganizationalUnit"/> implementation.</param>
        bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit);
        /// <summary>
        /// Gets true if the represented instance is a member of the given group or any other security container.
        /// </summary>
        /// <param name="container">An instance of the <see cref="ISecurityContainer"/> implementation.</param>
        bool IsInContainer(ISecurityContainer container);

        /// <summary>
        /// Gets or sets a <see cref="MembershipExtension"/> instance that can customize the membershp of this user. 
        /// </summary>
        MembershipExtension MembershipExtension { get; set; }
    }
}