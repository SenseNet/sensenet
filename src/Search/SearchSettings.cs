using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public class SearchEngineSettings
    {
        public static SearchEngineSettings Instance { get; private set; }

        public TimeSpan ForceReopenFrequency { get; private set; }

        public static void SetConfiguration(IDictionary<string, object> configuration)
        {
            var instance = new SearchEngineSettings();

            var settings = GetValue<int>(configuration, "ForceReopenFrequencyInSeconds", 0);
            instance.ForceReopenFrequency = TimeSpan.FromSeconds(settings == 0 ? 30.0 : settings);

            Instance = instance;
        }

        private static T GetValue<T>(IDictionary<string, object> configuration, string key, T defaultValue)
        {
            object settingValue;
            if (configuration.TryGetValue(key, out settingValue))
                return ConvertSettingValue<T>(settingValue, defaultValue);
            return defaultValue;
        }
        private static T ConvertSettingValue<T>(object value, T defaultValue)
        {
            if (value == null)
                return defaultValue;
            else if (value is T)
                return (T)value;
            else if (typeof(T).IsEnum && value is string)
                return (T)Enum.Parse(typeof(T), (string)value, true);
            else
                return (T)Convert.ChangeType(value, typeof(T));
        }

    }
}
