using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Storage.Security;

namespace SenseNet.Services.Core.Sharing
{
    public class SharingMembershipExtenderCore : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user, HttpContext httpContext)
        {
            if (httpContext != null && user != null)
            {
                //TODO: handle multiple group ids in context
                var cookieValue = httpContext.Request.Cookies[Constants.SharingTokenKey];

                var extension = SystemAccount.Execute(() => GetSharingExtension(cookieValue, httpContext));
                var extensionGroupId = extension.ExtensionIds.FirstOrDefault();
                if (extensionGroupId > 0)
                {
                    // the url value takes precedence
                    var currentValue = httpContext.Request.Query[Constants.SharingUrlParameterName];
                    if (string.IsNullOrEmpty(currentValue))
                        currentValue = cookieValue;

                    // set cookie only if it is different from the current value
                    if (currentValue != cookieValue)
                    {
                        SnTrace.Security.Write($"SharingMembershipExtender: setting sharing cookie containing group id {extensionGroupId}.");

                        httpContext.Response.Cookies.Append(Constants.SharingTokenKey, currentValue, new CookieOptions
                        {
                            Expires = DateTime.UtcNow.AddDays(1)
                        });
                    }
                }
                else if (cookieValue != null)
                {
                    // No sharing group or invalid group or content: clear cookie
                    // (only if there was a cookie before).
                    httpContext.Response.Cookies.Delete(Constants.SharingTokenKey);
                }

                return extension;
            }

            return MembershipExtension.Placeholder;
        }

        internal MembershipExtension GetSharingExtension(string contextValue, HttpContext httpContext)
        {
            // check the url first
            var sharingGroup = GetSharingGroupByUrlParameter(httpContext)?.ContentHandler as Group;

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

        private Content GetSharingGroupByUrlParameter(HttpContext httpContext)
        {
            var sharingGuid = httpContext?.Request.Query[Constants.SharingUrlParameterName];

            return string.IsNullOrEmpty(sharingGuid)
                ? null
                : SharingHandler.GetSharingGroupBySharingId(sharingGuid);
        }
    }
}
