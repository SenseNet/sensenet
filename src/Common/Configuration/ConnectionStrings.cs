using System.Collections;
using System.Collections.Generic;

namespace SenseNet.Configuration
{
    public class ConnectionStrings : SnConfig
    {
        #region Internal infrastructure

        /// <summary>
        /// Partially implemented IReadOnlyDictionary class for backward compatibility purposes.
        /// Its main feature is to return a connection string by its key from the configuration.
        /// </summary>
        private class ConnectionStringCollection : IReadOnlyDictionary<string, string>
        {
            public string this[string key] => SnConfig.GetString("ConnectionStrings", key);

            public bool ContainsKey(string key)
            {
                return this[key] != null;
            }

            #region Not supported
            public IEnumerable<string> Keys => throw new SnNotSupportedException();
            public IEnumerable<string> Values => throw new SnNotSupportedException();
            public int Count => throw new SnNotSupportedException();
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() { throw new SnNotSupportedException(); }
            public bool TryGetValue(string key, out string value) { throw new SnNotSupportedException(); }
            IEnumerator IEnumerable.GetEnumerator() { throw new SnNotSupportedException(); }
            #endregion
        }

        #endregion

        private static readonly string DefaultConnectionString = "Persist Security Info=False;Initial Catalog=SenseNetContentRepository;Data Source=MySenseNetContentRepositoryDatasource;User ID=SenseNetContentRepository;password=SenseNetContentRepository";
        
        /// <summary>
        /// Gets the configured SenseNet connection string (or the default one if there is no config).
        /// </summary>
        public static string ConnectionString { get; set; } = GetConnectionString("SnCrMsSql", DefaultConnectionString);
        /// <summary>
        /// Connection string for the permission storage. By default this is the same as the main Content Repository connection string.
        /// </summary>
        public static string SecurityDatabaseConnectionString { get; internal set; } = GetConnectionString("SecurityStorage");
        /// <summary>
        /// Connection string for the SignalR backplane. By default this is the same as the main Content Repository connection string.
        /// </summary>
        public static string SignalRDatabaseConnectionString { get; internal set; } = GetConnectionString("SignalRDatabase");
        
        public static IReadOnlyDictionary<string, string> AllConnectionStrings { get; internal set; } = new ConnectionStringCollection();

        private static string GetConnectionString(string key, string defaultValue = null)
        {
            var configValue = GetString("ConnectionStrings", key, defaultValue);

            return string.IsNullOrEmpty(configValue)
                ? defaultValue ?? ConnectionString 
                : configValue;
        }
    }
}
