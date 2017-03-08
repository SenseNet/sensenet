using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class MembershipExtension
    {
        private IEnumerable<int> _extensionIds;

        public MembershipExtension(IEnumerable<ISecurityContainer> extension)
        {
            _extensionIds = extension == null ? new int[0] : extension.Select(x => x.Id).ToArray();
        }

        public IEnumerable<int> ExtensionIds { get { return _extensionIds; } }
    }
    public class MembershipExtenderBase
    {
        public static readonly MembershipExtension EmptyExtension = new MembershipExtension(new ISecurityContainer[0]);
        private static MembershipExtenderBase _instance;
        private static MembershipExtenderBase Instance { get { return _instance; } }

        static MembershipExtenderBase()
        {
            _instance = TypeHandler.ResolveProvider<MembershipExtenderBase>();
        }

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

        public virtual MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

    public sealed class DefaultMembershipExtender : MembershipExtenderBase
    {
        public override MembershipExtension GetExtension(IUser user)
        {
            return MembershipExtenderBase.EmptyExtension;
        }
    }

}
