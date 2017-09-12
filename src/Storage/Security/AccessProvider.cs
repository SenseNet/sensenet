using System;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Security
{
    public abstract class AccessProvider
    {
		protected readonly IUser StartupUser = new StartupUser();

        public static AccessProvider Current => Providers.Instance.AccessProvider;

        protected virtual void Initialize()
        {
            // do nothing
        }

        internal void InitializeInternal()
        {
            Initialize();
        }

        [Obsolete]
        public static bool IsInitialized => Current != null;

        public abstract IUser GetCurrentUser();

        public void SetCurrentUser(IUser user)
        {
            if (user.Id == Identifiers.SomebodyUserId)
                throw new SenseNetSecurityException("Cannot log in as 'Somebody' user.");
            DoSetCurrentUser(user);
        }
        protected abstract void DoSetCurrentUser(IUser user);

        public abstract bool IsAuthenticated { get; }

        private static SystemUser GetCurrentUserAsSystem()
        {
            return AccessProvider.Current.GetCurrentUser() as SystemUser;
        }

        public static void ChangeToSystemAccount()
        {
            // if the current user is the SYSTEM already, do nothing
            var sysuser = GetCurrentUserAsSystem();
            if (sysuser != null)
            {
                sysuser.Increment();
                return;
            }
            AccessProvider.Current.SetCurrentUser(new SystemUser(AccessProvider.Current.GetCurrentUser()));
        }
        public static void RestoreOriginalUser()
        {
            var sysuser = GetCurrentUserAsSystem();
            if (sysuser == null)
                return;
            if (sysuser.Decrement())
                return;
            AccessProvider.Current.SetCurrentUser(sysuser.OriginalUser);
        }

        public IUser GetOriginalUser()
        {
            var user = GetCurrentUser();
            var sysuser = user as SystemUser;
            if (sysuser == null)
                return user;
            return sysuser.OriginalUser;
        }
    }
}