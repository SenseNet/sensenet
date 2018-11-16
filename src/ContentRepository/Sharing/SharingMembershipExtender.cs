﻿using System.Collections.Specialized;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// A built-in membership extender for handlin sharing requests. When a request contains
    /// a sharing identifier, this extender looks for the content that was shared using this id.
    /// If the content is found and the sharing group that represents this sharing id exists,
    /// this extender adds it to the extended identity list of the user and inserts it
    /// into the session. Subsequent requests do not have to contain the sharing id, the one
    /// in the session will take precedence.
    /// </summary>
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
                sharingGroup = SharingHandler.GetSharingGroupByUrlParameter(parameters)?.ContentHandler as Group;

                // Found a sharing group for the id: put it into the session 
                // and add it to the list of extensions of the user.
                if (sharingGroup != null && session != null)
                    session[Constants.SharingSessionKey] = sharingGroup.Id;
            }
            
            if (sharingGroup == null)
                return EmptyExtension;

            var sharedNode = sharingGroup.GetReference<GenericContent>(Constants.SharedContentFieldName);

            // Check if the related shared content exists.
            if (sharedNode == null)
            {
                // Invalid sharing group: no related content. Delete the group and move on.
                sharingGroup.ForceDelete();
                sharingGroup = null;
            }

            // If found and the content is not in the Trash, return a new extension collection 
            // containing the sharing group.
            if ((sharingGroup?.ContentType.IsInstaceOfOrDerivedFrom(Constants.SharingGroupTypeName) ?? false) &&
                !TrashBin.IsInTrash(sharedNode))
            {
                return new MembershipExtension(new[] { sharingGroup.Id });
            }

            // invalid group or content
            if (session != null)
                session[Constants.SharingSessionKey] = 0;

            return EmptyExtension;
        }
    }
}
