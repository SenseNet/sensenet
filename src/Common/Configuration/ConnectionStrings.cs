using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace SenseNet.Configuration
{
    public class ConnectionStrings : SnConfig
    {
        private static readonly string DefaultConnectionString = "Persist Security Info=False;Initial Catalog=SenseNetContentRepository;Data Source=MySenseNetContentRepositoryDatasource;User ID=SenseNetContentRepository;password=SenseNetContentRepository";
        
        /// <summary>
        /// Gets the configured SenseNet connection string (or the default one if there is no config).
        /// </summary>
        public static string ConnectionString { get; internal set; } = GetConnectionString("SnCrMsSql", DefaultConnectionString);
        /// <summary>
        /// Connection string for the permission storage. By default this is the same as the main Content Repository connection string.
        /// </summary>
        public static string SecurityDatabaseConnectionString { get; internal set; } = GetConnectionString("SecurityStorage");
        /// <summary>
        /// Connection string for the SignalR backplane. By default this is the same as the main Content Repository connection string.
        /// </summary>
        public static string SignalRDatabaseConnectionString { get; internal set; } = GetConnectionString("SignalRDatabase");

        public static IReadOnlyDictionary<string, string> AllConnectionStrings { get; internal set; } = 
            ConfigurationManager.ConnectionStrings
            .Cast<ConnectionStringSettings>()
            .ToDictionary(c => c.Name, c => c.ConnectionString);
        private static string GetConnectionString(string key, string defaultValue = null)
        {
            var configValue = ConfigurationManager.ConnectionStrings[key];

            return configValue == null 
                ? (defaultValue ?? ConnectionString) 
                : configValue.ConnectionString;
        }
    }
}
