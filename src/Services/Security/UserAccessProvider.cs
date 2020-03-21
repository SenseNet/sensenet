using SenseNet.ContentRepository.Storage.Security;
using System.Web;
using System.Threading;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Security
{
    public class UserAccessProvider : AccessProvider
    {
        public override int DefaultUserId => Identifiers.VisitorUserId;

        private IUser CurrentUser
        {
            get
            {
                if (Thread.CurrentPrincipal?.Identity is IUser user)
                    return user;

                CurrentUser = StartupUser;
                user = User.Administrator;
                CurrentUser = user;
                return user;
            }
            set => Thread.CurrentPrincipal = new PortalPrincipal(value);
        }

        public override IUser GetCurrentUser()
        {
            if (HttpContext.Current == null)
                return CurrentUser;

            IUser currentUser = null;
            if (HttpContext.Current.User != null)
                currentUser = HttpContext.Current.User.Identity as IUser;

            if (currentUser != null)
                return currentUser;

            SetCurrentUser(StartupUser);
            currentUser = StartupUser;

            return currentUser;
        }

        protected override void DoSetCurrentUser(IUser user)
        {
            if (HttpContext.Current == null)
                CurrentUser = user;
            else
                HttpContext.Current.User = new PortalPrincipal(user);
        }

        public override bool IsAuthenticated
        {
            get
            {
                if (HttpContext.Current == null)
                    return false;

                System.Security.Principal.IPrincipal currentPrincipal =
                    HttpContext.Current.User;

                return currentPrincipal?.Identity != null && currentPrincipal.Identity.IsAuthenticated;
            }
        }
    }
}