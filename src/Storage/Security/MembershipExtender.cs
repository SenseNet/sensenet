using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Security
{
    //UNDONE:! XMLDOC:
    public class MembershipExtension
    {
        private IEnumerable<int> _extensionIds;

        //UNDONE:! XMLDOC:
        public MembershipExtension(IEnumerable<ISecurityContainer> extension)
        {
            _extensionIds = extension == null ? new int[0] : extension.Select(x => x.Id).ToArray();
        }

        //UNDONE:! XMLDOC:
        public IEnumerable<int> ExtensionIds { get { return _extensionIds; } }
    }
    //UNDONE:! XMLDOC:
    public class MembershipExtenderBase
    {
        //UNDONE:! XMLDOC:
        public static readonly MembershipExtension EmptyExtension = new MembershipExtension(new ISecurityContainer[0]);
        private static MembershipExtenderBase _instance;
        private static MembershipExtenderBase Instance { get { return _instance; } }

        static MembershipExtenderBase()
        {
            _instance = TypeHandler.ResolveProvider<MembershipExtenderBase>();
        }

        //UNDONE:! XMLDOC:
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

        //UNDONE:! XMLDOC:
        public virtual MembershipExtension GetExtension(IUser user)
        {
            return EmptyExtension;
        }
    }

    /// <summary>
    /// Implements the default class of the <see cref="MembershipExtenderBase"/>.
    /// This class cannot be inherites.
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
