using System;
using System.Collections.Generic;
using System.Linq;
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
            CollectProperties(props);

            if (!props.ContainsKey("WorkingMode"))
                props.Add("WorkingMode", RepositoryEnvironment.WorkingMode.RawValue);

            foreach (var key in props.Keys.Where(key => props[key] == null).ToList())
                props[key] = string.Empty;

            return props;
        }

        /// <summary>
        /// Collects additional properties.
        /// Derived classes may add custom properties by overriding this method.
        /// </summary>
        protected virtual void CollectProperties(IDictionary<string, object> props)
        {
            // empty base method
        }

        private static void CollectUserProperties(IDictionary<string, object> properties)
        {
            IUser loggedUser = null;

            try
            {
                loggedUser = AccessProvider.Current.GetCurrentUser();
            }
            catch (Exception)
            {
                // Suppress error: loading the current user failed (e.g. because the repository is not present).
            }

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
    }
}
