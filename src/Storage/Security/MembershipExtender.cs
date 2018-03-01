using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Defines a class that contains a list of group ids for extending the membership of a user.
    /// The user will be a member of these groups temporarily, for the lifetime of the request.
    /// </summary>
    public class MembershipExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MembershipExtension"/>.
        /// </summary>
        /// <param name="extension">The collection of groups that extends the membership of a user.</param>
        public MembershipExtension(IEnumerable<ISecurityContainer> extension)
        {
            ExtensionIds = extension?.Select(x => x.Id).ToArray() ?? new int[0];
        }

        /// <summary>
        /// Gets the collection of group ids that extends the membership of a user.
        /// </summary>
        public IEnumerable<int> ExtensionIds { get; }
    }

    /// <summary>
    /// Defines a base class for extending a users's membership.
    /// Inherited classes can customize the algorithm of selecting additional groups.
    /// </summary>
    public class MembershipExtenderBase
    {
        /// <summary>
        /// Defines a constant for empty extension groups.
        /// </summary>
        public static readonly MembershipExtension EmptyExtension = new MembershipExtension(new ISecurityContainer[0]);
        private static MembershipExtenderBase Instance => Providers.Instance.MembershipExtender;

        static MembershipExtenderBase()
        {
        }

        /// <summary>
        /// Extends the specified user's membership by setting its MembershipExtension property.
        /// </summary>
        /// <param name="user">The <see cref="IUser"/> instance that's membership will be extended.</param>
        public static void Extend(IUser user)
        {
            Instance?.ExtendPrivate(user);
        }
        private void ExtendPrivate(IUser user)
        {
            user.MembershipExtension = GetExtension(user) ?? EmptyExtension;
        }

        /// <summary>
        /// Produces a <see cref="MembershipExtension"/> instance for the given user.
        /// The return value can be assigned to the given <see cref="IUser"/>'s MembershipExtension property.
        /// </summary>
        /// <param name="user">The <see cref="IUser"/> instance that's <see cref="MembershipExtension"/> will be created.</param>
        public virtual MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

    /// <summary>
    /// Implements the default class inheriting <see cref="MembershipExtenderBase"/>.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class DefaultMembershipExtender : MembershipExtenderBase
    {
        /// <summary>
        /// The return value is always <see cref="MembershipExtenderBase.EmptyExtension"/>.
        /// </summary>
        public override MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }
}
