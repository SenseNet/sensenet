using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Security;
using System.Threading;
using SenseNet.Configuration;

namespace SenseNet.Diagnostics
{
    public static class Utility
    {
        private const string LoggedUserNameKey = "UserName";
        private const string LoggedUserNameKey2 = "LoggedUserName";
        private const string SpecialUserNameKey = "SpecialUserName";

        [Obsolete("Use PropertyCollector instance in the SnLog.", true)]
        public static IDictionary<string, object> CollectAutoProperties(IDictionary<string, object> properties)
        {
            var props = properties;
            if (props == null)
                props = new Dictionary<string, object>();
            if (props.IsReadOnly)
                props = new Dictionary<string, object>(props);

            CollectUserProperties(props);
            CollectContextProperties(props);

            var nullNames = new List<string>();
            foreach (var key in props.Keys)
                if (props[key] == null)
                    nullNames.Add(key);
            foreach (var key in nullNames)
                props[key] = String.Empty;

            return props;
        }
        private static void CollectUserProperties(IDictionary<string, object> properties)
        {
            IUser loggedUser = GetCurrentUser();

            if (loggedUser == null)
                return;

            IUser specialUser = null;

            if (loggedUser is StartupUser)
            {
                specialUser = loggedUser;
                loggedUser = null;
            }
            else
            {
                var systemUser = loggedUser as SystemUser;
                if (systemUser != null)
                {
                    specialUser = systemUser;
                    loggedUser = systemUser.OriginalUser;
                }
            }

            if (loggedUser != null)
            {
                if (properties.ContainsKey(LoggedUserNameKey))
                {
                    if (properties.ContainsKey(LoggedUserNameKey2))
                        properties[LoggedUserNameKey2] = loggedUser.Username ?? String.Empty;
                    else
                        properties.Add(LoggedUserNameKey2, loggedUser.Username ?? String.Empty);
                }
                else
                {
                    properties.Add(LoggedUserNameKey, loggedUser.Username ?? String.Empty);
                }
            }
            if (specialUser != null)
            {
                if (properties.ContainsKey(SpecialUserNameKey))
                    properties[SpecialUserNameKey] = specialUser.Username ?? String.Empty;
                else
                    properties.Add(SpecialUserNameKey, specialUser.Username ?? String.Empty);
            }
        }
        private static void CollectContextProperties(IDictionary<string, object> properties)
        {
            if (!properties.ContainsKey("WorkingMode"))
                properties.Add("WorkingMode", RepositoryEnvironment.WorkingMode.RawValue);

            if (!properties.ContainsKey("IsHttpContext"))
            {
                var ctx = System.Web.HttpContext.Current;
                properties.Add("IsHttpContext", ctx == null ? "no" : "yes");
                if (ctx != null)
                {
                    System.Web.HttpRequest req = null;
                    try
                    {
                        req = ctx.Request;
                    }
                    catch { }// does nothing
                    if (req != null)
                    {
                        if (!properties.ContainsKey("Url"))
                            properties.Add("Url", ctx.Request.Url);
                        if (!properties.ContainsKey("Referrer"))
                            properties.Add("Referrer", ctx.Request.UrlReferrer);
                    }
                    else
                    {
                        if (!properties.ContainsKey("Url"))
                            properties.Add("Url", "// not available //");
                    }
                }
            }
        }

        private static IUser GetCurrentUser()
        {
            if ((System.Web.HttpContext.Current != null) && (System.Web.HttpContext.Current.User != null))
                return System.Web.HttpContext.Current.User.Identity as IUser;
            return Thread.CurrentPrincipal.Identity as IUser;
        }
    }
}
