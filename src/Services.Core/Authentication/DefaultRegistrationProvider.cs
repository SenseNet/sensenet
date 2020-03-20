using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Services.Core.Operations;
using System;
using System.Linq;
using System.Security.Claims;

namespace SenseNet.Services.Core.Authentication
{
    public class DefaultRegistrationProvider : IRegistrationProvider
    {
        public User CreateLocalUser(Content content, HttpContext context, string userName, string password)
        {
            throw new NotImplementedException();
        }

        public User CreateProviderUser(Content content, HttpContext context, string provider, string userId, ClaimInfo[] claims)
        {
            //UNDONE: configure user parent and user content type
            var parent = Node.LoadNode("/Root/IMS/BuiltIn/Portal");
            var providerFieldName = $"{provider}ProviderId";

            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

            //UNDONE: finalize user content name, it should not be this id
            var user = Content.CreateNew("User", parent, userId);
            user[providerFieldName] = userId;
            user["Email"] = email;
            user["LoginName"] = userId;
            user["FullName"] = fullName;
            user.DisplayName = fullName;
            user.Save();

            //UNDONE: remove admin group membership
            Group.Administrators.AddMember(user.ContentHandler as User);

            return user.ContentHandler as User;
        }
    }
}
