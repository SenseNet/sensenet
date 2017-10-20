using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class MembershipExtension
    {
        public MembershipExtension(IEnumerable<ISecurityContainer> extension)
        {
            ExtensionIds = extension?.Select(x => x.Id).ToArray() ?? new int[0];
        }

        public IEnumerable<int> ExtensionIds { get; }
    }
    public class MembershipExtenderBase
    {
        public static readonly MembershipExtension EmptyExtension = new MembershipExtension(new ISecurityContainer[0]);
        private static MembershipExtenderBase Instance => Providers.Instance.MembershipExtender;

        static MembershipExtenderBase()
        {
        }

        public static void Extend(IUser user)
        {
            Instance?.ExtendPrivate(user);
        }
        private void ExtendPrivate(IUser user)
        {
            user.MembershipExtension = GetExtension(user) ?? EmptyExtension;
        }

        public virtual MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

    public sealed class DefaultMembershipExtender : MembershipExtenderBase
    {
        public override MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

}
