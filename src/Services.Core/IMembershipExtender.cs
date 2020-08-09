using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core
{
    /// <summary>
    /// Defines methods for developing a membership extender. An implementation may get service dependencies
    /// in their constructor, because types will be registered as scoped services.
    /// Instances of these classes will be created on every request, so they cannot hold any state.
    /// </summary>
    public interface IMembershipExtender
    {
        /// <summary>
        /// Gets a list of additional group identifiers that should be added dynamically to the
        /// list of users' existing memberships.
        /// </summary>
        /// <param name="user">The current user to extend.</param>
        MembershipExtension GetExtension(IUser user);
    }
}
