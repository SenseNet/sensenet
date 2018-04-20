using SenseNet.Security;
using System;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Defines an interface for classes that encapsulate metadata and functionality 
    /// of the Content Repository's users.
    /// </summary>
    public interface IUser : ISecurityMember, System.Security.Principal.IIdentity, ISecurityUser
    {
        /// <summary>
        /// Gets or sets a value that describes whether the user can log in or not.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Gets the domain name of the user.
        /// </summary>
        string Domain { get; }
        /// <summary>
        /// Gets or sets the e-mail address of the user.
        /// </summary>
        string Email { get; set; }
        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        string FullName { get; set; }
        /// <summary>
        /// Sets the password of the user.
        /// WARNING! Storing of this value is forbidden. The value must be hashed 
        /// immediately and the hash value must be stored in the PasswordHash property.
        /// </summary>
        string Password { set; }
        /// <summary>
        /// Gets or sets the hash value of the user's password.
        /// </summary>
        string PasswordHash { get; set; }
        /// <summary>
        /// Gets the login name of the user.
        /// </summary>
        string Username { get; } // = Domain + "\" + Node.Name

        /// <summary>
        /// Gets true if the user is a member of the given group.
        /// </summary>
        /// <param name="group">An instance of an <see cref="IGroup"/> implementation.</param>
        bool IsInGroup(IGroup group);
        /// <summary>
        /// Gets true if the user is a member of the given organizational unit.
        /// </summary>
        /// <param name="orgUnit">An instance of an <see cref="IOrganizationalUnit"/> implementation.</param>
        bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit);
        /// <summary>
        /// Gets true if the user is a member of the given group or any other security container.
        /// </summary>
        /// <param name="container">An instance of an <see cref="ISecurityContainer"/> implementation.</param>
        bool IsInContainer(ISecurityContainer container);

        /// <summary>
        /// Gets the last exact time of the user's ultimate logout.
        /// </summary>
        DateTime LastLoggedOut { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MembershipExtension"/> instance that can customize the membership of this user. 
        /// </summary>
        MembershipExtension MembershipExtension { get; set; }
    }
}