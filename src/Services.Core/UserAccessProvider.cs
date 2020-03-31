using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Security
{
    public class UserAccessProvider : AccessProvider
    {
        public override int DefaultUserId => Identifiers.VisitorUserId;

        private readonly AsyncLocal<IUser> _currentUser = new AsyncLocal<IUser>();

        public override IUser GetCurrentUser()
        {
            var user = _currentUser.Value;
            if (user != null)
                return user;

            // not authenticated yet
            SetCurrentUser(StartupUser);

            return StartupUser;
        }

        protected override void DoSetCurrentUser(IUser user)
        {
            _currentUser.Value = user;
        }

        public override bool IsAuthenticated
        {
            get
            {
                var user = _currentUser?.Value;
                if (user == null)
                    return false;

                return user.Id != Identifiers.StartupUserId && user.Id != Identifiers.VisitorUserId;
            }
        }
    }
}
