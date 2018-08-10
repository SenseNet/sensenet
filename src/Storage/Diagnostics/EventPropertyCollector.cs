using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class EventPropertyCollector : IEventPropertyCollector
    {
        private const string LoggedUserNameKey = "UserName";
        private const string LoggedUserNameKey2 = "LoggedUserName";
        private const string SpecialUserNameKey = "SpecialUserName";

        public IDictionary<string, object> Collect(IDictionary<string, object> properties)
        {
            var props = properties ?? new Dictionary<string, object>();
            if (props.IsReadOnly)
                props = new Dictionary<string, object>(props);

            CollectUserProperties(props);
            CollectContextProperties(props);

            foreach (var key in props.Keys.Where(key => props[key] == null).ToList())
                props[key] = string.Empty;

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
                if (loggedUser is SystemUser systemUser)
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
                    catch
                    {
                        // ignored
                    }
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
