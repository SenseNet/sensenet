using System;
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

    public class ConnectionStringOptions
    {
        private string _securityDatabase;
        private string _signalRDatabase;

        //TODO: [DIREF] remove legacy configuration when upper layers are ready.
        /// <summary>
        /// DO NOT USE THIS IN YOUR CODE. This method is intended for internal use only and will be removed in the near future.
        /// </summary>
        /// <returns>A new instance of ConnectionStringOptions filled with static configuration values.</returns>
        [Obsolete]
        public static ConnectionStringOptions GetLegacyConnectionStrings()
        {
            return new ConnectionStringOptions
            {
                ConnectionString = ConnectionStrings.ConnectionString,
                SecurityDatabase = ConnectionStrings.SecurityDatabaseConnectionString,
                SignalRDatabase = ConnectionStrings.SignalRDatabaseConnectionString
            };
        }

        //UNDONE: find a proper name for connection string properties
        // Maybe: Repository, Security, SignalR?
        public string ConnectionString { get; set; }

        public string SecurityDatabase
        {
            get => _securityDatabase ?? ConnectionString;
            set => _securityDatabase = value;
        }

        public string SignalRDatabase
        {
            get => _signalRDatabase ?? ConnectionString;
            set => _signalRDatabase = value;
        }
    }
}
