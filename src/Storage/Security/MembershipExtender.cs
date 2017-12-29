using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Defines a class that contains a list of group ids for extending the membership of a user.
    /// The user will be a member of these groups temporarily, for the lifetime of the request.
    /// </summary>
    public class MembershipExtension
    {
        private IEnumerable<int> _extensionIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="MembershipExtension"/>.
        /// </summary>
        /// <param name="extension">The collection of groups that extends the membership of a user.</param>
        public MembershipExtension(IEnumerable<ISecurityContainer> extension)
        {
            _extensionIds = extension == null ? new int[0] : extension.Select(x => x.Id).ToArray();
        }

        /// <summary>
        /// Gets the collection of group ids that extends the membership of a user.
        /// </summary>
        public IEnumerable<int> ExtensionIds { get { return _extensionIds; } }
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
        private static MembershipExtenderBase _instance;
        private static MembershipExtenderBase Instance { get { return _instance; } }

        static MembershipExtenderBase()
        {
            _instance = TypeHandler.ResolveProvider<MembershipExtenderBase>();
        }

        /// <summary>
        /// Extends the specified user's membership by setting its MembershipExtension property.
        /// </summary>
        /// <param name="user">The <see cref="IUser"/> instance that's membership will be extended.</param>
        public static void Extend(IUser user)
        {
            var instance = Instance;
            if (instance == null)
                return;
            instance.ExtendPrivate(user);
        }
        private void ExtendPrivate(IUser user)
        {
            var ext = GetExtension(user);
            if (ext == null)
                ext = EmptyExtension;
            user.MembershipExtension = ext;
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
            return MembershipExtenderBase.EmptyExtension;
        }
    }
}
