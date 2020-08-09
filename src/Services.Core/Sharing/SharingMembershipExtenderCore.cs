using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Sharing
{
    /// <summary>
    /// A built-in membership extender for handling sharing requests. When a request contains
    /// a sharing identifier, this extender looks for the content that was shared using this id.
    /// If the content is found and the sharing group that represents this sharing id exists,
    /// this extender adds it to the extended identity list of the user and inserts it
    /// into a cookie. Subsequent requests do not have to contain the sharing id in the url,
    /// we will use the one in the cookie.
    /// </summary>
    public class SharingMembershipExtenderCore : IMembershipExtender
    {
        private readonly HttpContext _httpContext;

        public SharingMembershipExtenderCore(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public MembershipExtension GetExtension(IUser user)
        {
            if (_httpContext == null || user == null) 
                return MembershipExtension.Placeholder;

            //TODO: handle multiple group ids in context
            var cookieValue = _httpContext.Request.Cookies[Constants.SharingTokenKey];

            var extension = SystemAccount.Execute(() => GetSharingExtension(cookieValue));
            var extensionGroupId = extension.ExtensionIds.FirstOrDefault();
            if (extensionGroupId > 0)
            {
                // the url value takes precedence
                var currentValue = _httpContext.Request.Query[Constants.SharingUrlParameterName];
                if (string.IsNullOrEmpty(currentValue))
                    currentValue = cookieValue;

                // set cookie only if it is different from the current value
                if (currentValue != cookieValue)
                {
                    SnTrace.Security.Write($"SharingMembershipExtender: setting sharing cookie containing group id {extensionGroupId}.");

                    _httpContext.Response.Cookies.Append(Constants.SharingTokenKey, currentValue, new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddDays(1),
                        HttpOnly = true,
                        Secure = true
                    });
                }
            }
            else if (cookieValue != null)
            {
                // No sharing group or invalid group or content: clear cookie
                // (only if there was a cookie before).
                _httpContext.Response.Cookies.Delete(Constants.SharingTokenKey);
            }

            return extension;
        }

        internal MembershipExtension GetSharingExtension(string contextValue)
        {
            // check the url first
            var sharingGroup = GetSharingGroupByUrlParameter()?.ContentHandler as Group;

            // check the context param next
            if (sharingGroup == null && contextValue != null)
                sharingGroup = SharingHandler.GetSharingGroupBySharingId(contextValue)?.ContentHandler as Group;

            if (sharingGroup == null)
                return MembershipExtension.Placeholder;

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

            return MembershipExtension.Placeholder;
        }

        private Content GetSharingGroupByUrlParameter()
        {
            var sharingGuid = _httpContext?.Request.Query[Constants.SharingUrlParameterName];

            return string.IsNullOrEmpty(sharingGuid)
                ? null
                : SharingHandler.GetSharingGroupBySharingId(sharingGuid);
        }
    }
}
