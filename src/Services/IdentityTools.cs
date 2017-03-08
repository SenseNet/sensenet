using System.Net.Http;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services
{
    public static class IdentityTools
    {
        public static string GetDefaultUserAvatarPath()
        {
            return SkinManagerBase.Resolve("$skin/images/default_avatar.png");
        }
        public static string GetDefaultGroupAvatarPath()
        {
            return SkinManagerBase.Resolve("$skin/images/default_groupavatar.png");
        }

        [ODataFunction]
        public static object BrowseProfile(Content content, string back)
        {
            back = HttpUtility.UrlDecode(back);
            string url;
            User user = null;

            try
            {
                user = content.ContentHandler as User;
            }
            catch (SenseNetSecurityException)
            {
                // suppress this
            }

            if (user != null)
            {
                if (IdentityManagement.UserProfilesEnabled)
                {
                    if (!user.IsProfileExist())
                        user.CreateProfile();
                }

                url = user.ProfilePath;

                if (!string.IsNullOrEmpty(back))
                    url = $"{url}?{PortalContext.BackUrlParamName}={back}";
            }
            else
            {
                url = back;
            }

            if (string.IsNullOrEmpty(url))
                return null;

            HttpContext.Current.Response.Redirect(url, true);
            return null;
        }
    }
}
