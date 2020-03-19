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
            //UNDONE: configure user parent
            var user = Content.CreateNew("User", Node.LoadNode("/Root/IMS/BuiltIn/Portal"), userId);
            user["Email"] = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            user["LoginName"] = userId;
            user["FullName"] = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            user.DisplayName = userId;
            user.Save();

            //UNDONE: remove admin group membership
            Group.Administrators.AddMember(user.ContentHandler as User);

            return user.ContentHandler as User;
        }
    }
}
