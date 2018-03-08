using System;
using System.Linq;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage
{
    public interface ISettingsManager
    {
        T GetValue<T>(string settingsName, string key, string contextPath, T defaultValue);
        bool IsSettingsAvailable(string settingsName, string contextPath = null);
    }

    /// <summary>
    /// This class servers as a representation of the Settings class that resides on the ContentRepository level.
    /// </summary>
    internal class Settings
    {
        private static ISettingsManager _settingsManager;
        private static ISettingsManager SettingsManager
        {
            get
            {
                if (_settingsManager == null)
                {
                    var defType = typeof (DefaultSettingsManager);
                    var smType = TypeResolver.GetTypesByInterface(typeof (ISettingsManager)).FirstOrDefault(t => t.FullName != defType.FullName) ?? defType;

                    _settingsManager = Activator.CreateInstance(smType) as ISettingsManager;
                }

                return _settingsManager;
            }
        }

        private class DefaultSettingsManager : ISettingsManager
        {
            T ISettingsManager.GetValue<T>(string settingsName, string key, string contextPath, T defaultValue)
            {
                return default(T);
            }
#pragma warning disable CS1066 // The default value specified will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            bool ISettingsManager.IsSettingsAvailable(string settingsName, string contextPath = null)
#pragma warning restore CS1066 // The default value specified will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
            {
                return true;
            }
        }

        public static T GetValue<T>(string settingsName, string key, string contextPath = null, T defaultValue = default(T))
        {
            return SettingsManager.GetValue(settingsName, key, contextPath, defaultValue);
        }
        public static bool IsSettingsAvailable(string settingsName, string contextPath = null)
        {
            return SettingsManager.IsSettingsAvailable(settingsName, contextPath);
        }
    }
}
