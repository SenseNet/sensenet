using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Sharing
{
    internal class SharingMembershipExtender : MembershipExtenderBase
    {
        public override MembershipExtension GetExtension(IUser user)
        {
            var context = HttpContext.Current;
            if (context != null && user != null)
            {
                return SystemAccount.Execute(() => GetSharingExtension(context.Request.Params,
                    context.Session != null 
                        ? new HttpSessionStateWrapper(context.Session)
                        : null));
            }

            return base.GetExtension(user);
        }

        internal static MembershipExtension GetSharingExtension(NameValueCollection parameters, HttpSessionStateBase session)
        {
            Group sharingGroup;

            var sharingIdentity = (int)(session?[Constants.SharingSessionKey] ?? 0);
            if (sharingIdentity != 0)
            {
                // found a sharing group in the session
                sharingGroup = Node.Load<Group>(sharingIdentity);
            }
            else
            {
                // check the url
                var groupContent = SharingHandler.GetSharingGroupFromUrl(parameters);

                sharingGroup = groupContent?.ContentHandler as Group;
                if (sharingGroup != null)
                {
                    // Check if the related shared content exists.
                    if (!(groupContent[Constants.SharedContentFieldName] is IEnumerable<Node> relatedContent) 
                        || !relatedContent.Any())
                    {
                        // Invalid sharing group: no related content. Delete the group and move on.
                        sharingGroup.ForceDelete();
                        sharingGroup = null;
                    }

                    // Found a sharing group for the id: put it into the session 
                    // and add it to the list of extensions of the user.
                    if (sharingGroup != null && session != null)
                        session[Constants.SharingSessionKey] = sharingGroup.Id;
                }
            }

            // if found, return a new extension collection containing the sharing group
            if (sharingGroup?.ContentType.IsInstaceOfOrDerivedFrom(Constants.SharingGroupTypeName) ?? false)
                return new MembershipExtension(new[] { sharingGroup.Id });

            return EmptyExtension;
        }
    }
}
