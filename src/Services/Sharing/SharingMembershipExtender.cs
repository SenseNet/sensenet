using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Sharing
{
    /// <summary>
    /// A built-in membership extender for handling sharing requests. When a request contains
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
                //TODO: handle multiple group ids in context
                var cookieValue = context.Request.Cookies[Constants.SharingTokenKey]?.Value;

                var extension = SystemAccount.Execute(() => GetSharingExtension(context.Request.Params, cookieValue));
                var extensionGroupId = extension.ExtensionIds.FirstOrDefault();
                if (extensionGroupId > 0)
                {
                    // the url value takes precedence
                    var currentValue = context.Request.Params[Constants.SharingUrlParameterName];
                    if (string.IsNullOrEmpty(currentValue))
                        currentValue = cookieValue;

                    // set cookie only if it is different from the current value
                    if (currentValue != cookieValue)
                    {
                        SnTrace.Security.Write($"SharingMembershipExtender: setting sharing cookie containing group id {extensionGroupId}.");

                        context.Response.Cookies.Set(new HttpCookie(Constants.SharingTokenKey, currentValue)
                        {
                            Expires = DateTime.UtcNow.AddDays(1)
                        });
                    }
                }
                else if (cookieValue != null)
                {
                    // No sharing group or invalid group or content: clear cookie
                    // (only if there was a cookie before).
                    context.Response.Cookies.Set(new HttpCookie(Constants.SharingTokenKey, string.Empty)
                    {
                        Expires = DateTime.UtcNow.AddDays(-1)
                    });
                }

                return extension;
            }

            return base.GetExtension(user);
        }

        internal static MembershipExtension GetSharingExtension(NameValueCollection parameters, string contextValue = null)
        {
            // check the url first
            var sharingGroup = GetSharingGroupByUrlParameter(parameters)?.ContentHandler as Group;

            // check the context param next
            if (sharingGroup == null && contextValue != null)
                sharingGroup = SharingHandler.GetSharingGroupBySharingId(contextValue)?.ContentHandler as Group;
            
            if (sharingGroup == null)
                return EmptyExtension;

            var sharedNode = sharingGroup.GetReference<GenericContent>(Constants.SharedContentFieldName);

            // Check if the related shared content exists.
            if (sharedNode == null)
            {
                // Invalid sharing group: no related content. Delete the group and move on.
                SnTrace.Security.Write($"SharingMembershipExtender: Deleting orphaned sharing group {sharingGroup.Id} ({sharingGroup.Path}).");

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

            return EmptyExtension;
        }

        private static Content GetSharingGroupByUrlParameter(NameValueCollection parameters)
        {
            var sharingGuid = parameters?[Constants.SharingUrlParameterName];

            return string.IsNullOrEmpty(sharingGuid)
                ? null
                : SharingHandler.GetSharingGroupBySharingId(sharingGuid);
        }
    }
}
