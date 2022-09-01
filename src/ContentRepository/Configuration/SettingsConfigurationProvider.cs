using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository;

namespace SenseNet.Configuration
{
    internal class SettingsConfigurationProvider : JsonConfigurationProvider
    {
        public SettingsConfigurationProvider(SettingsConfigurationSource source): base(source)
        {}

        public override void Load()
        {
            if (!(Providers.Instance?.SearchManager?.ContentQueryIsAllowed ?? false))
            {
                Data = new Dictionary<string, string>();
                return;
            }

            var settings = SettingsCache.Instance.GetSettings();

            // build a JSON file containing all settings inside a 'sensenet' property
            var allSettingsObject = new JObject();
            var snObject = new JObject();
            allSettingsObject.Add("sensenet", snObject);

            //UNDONE: filter settings that must not be treated as configuration for security reasons. Whitelist?
            foreach (var setting in settings)
            {
                var jo = setting.BinaryAsJObject;
                if (jo == null) 
                    continue;

                var propertyName = setting.Name.Replace(".settings", string.Empty);
                snObject.Add(propertyName, jo);
            }

            var allSettingsText = JsonConvert.SerializeObject(allSettingsObject, Formatting.Indented);

            using var settingsStream = RepositoryTools.GetStreamFromString(allSettingsText);
            base.Load(settingsStream);

            OnReload();
        }
    }

    internal class SettingsConfigurationSource : JsonConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new SettingsConfigurationProvider(this);
        }
    }
    
    //UNDONE: temp class for testing the feature
    public class PortalSettingsOptions
    {
        public int BinaryHandlerClientCacheMaxAge { get; set; }
        public string PermittedAppsWithoutOpenPermission { get; set; }
        public string[] AllowedOriginDomains { get; set; }
    }

    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddSenseNetSettingsConfiguration(
            this IConfigurationBuilder builder)
        {
            // access other config values if necessary

            //var tempConfig = builder.Build();
            //var connectionString =
            //    tempConfig.GetConnectionString("WidgetConnectionString");

            return builder.Add(new SettingsConfigurationSource());
        }
    }
}
