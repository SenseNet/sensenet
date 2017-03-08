using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tools;

namespace SenseNet.Portal
{
    public abstract class LoginExtender
    {
        public virtual void LoggingIn(CancellableLoginInfo info)
        {
            // do nothing
        }
        public virtual void LoggedIn(LoginInfo info)
        {
            // do nothing
        }
        public virtual void LoggingOut(CancellableLoginInfo info)
        {
            // do nothing
        }
        public virtual void LoggedOut(LoginInfo info)
        {
            // do nothing
        }
        public virtual void LoginError(LoginInfo info)
        {
            // do nothing
        }

        // ==========================================================

        private static object _sync = new object();
        private static Type[] _extenderTypes;
        private static Type[] ExtenderTypes
        {
            get
            {
                if (_extenderTypes == null)
                    lock (_sync)
                        if (_extenderTypes == null)
                            _extenderTypes = TypeResolver.GetTypesByBaseType(typeof(LoginExtender));
                return _extenderTypes;
            }
        }

        public static void OnLoggingIn(CancellableLoginInfo info)
        {
            foreach (var extender in GetLoginExtenders())
                extender.LoggingIn(info);
        }
        public static void OnLoggedIn(LoginInfo info)
        {
            foreach (var extender in GetLoginExtenders())
                extender.LoggedIn(info);
        }
        public static void OnLoggingOut(CancellableLoginInfo info)
        {
            foreach (var extender in GetLoginExtenders())
                extender.LoggingOut(info);
        }
        public static void OnLoggedOut(LoginInfo info)
        {
            foreach (var extender in GetLoginExtenders())
                extender.LoggedOut(info);
        }
        public static void OnLoginError(LoginInfo info)
        {
            foreach (var extender in GetLoginExtenders())
                extender.LoginError(info);
        }
        private static IEnumerable<LoginExtender> GetLoginExtenders()
        {
            foreach (var extenderType in ExtenderTypes)
            {
                var extender = (LoginExtender)Activator.CreateInstance(extenderType);
                yield return extender;
            }
        }
    }
}
