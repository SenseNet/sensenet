using System;
using System.Collections.Generic;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.EF6SecurityStore;
using SenseNet.Security.Messaging;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Providers : SnConfig
    {
        private const string SectionName = "sensenet/providers";

        public static string DataProviderClassName { get; internal set; } = GetProvider("DataProvider", typeof(SqlProvider).FullName);
        public static string AccessProviderClassName { get; internal set; } = GetProvider("AccessProvider",
            "SenseNet.ContentRepository.Security.UserAccessProvider");
        public static string ContentNamingProviderClassName { get; internal set; } = GetProvider("ContentNamingProvider");
        public static string TaskManagerClassName { get; internal set; } = GetProvider("TaskManager");
        public static string PasswordHashProviderClassName { get; internal set; } = GetProvider("PasswordHashProvider",
            typeof(SenseNetPasswordHashProvider).FullName);
        public static string OutdatedPasswordHashProviderClassName { get; internal set; } = GetProvider("OutdatedPasswordHashProvider",
            typeof(Sha256PasswordHashProviderWithoutSalt).FullName);
        public static string SkinManagerClassName { get; internal set; } = GetProvider("SkinManager", "SenseNet.Portal.SkinManager");
        public static string DirectoryProviderClassName { get; internal set; } = GetProvider("DirectoryProvider");
        public static string SecurityDataProviderClassName { get; internal set; } = GetProvider("SecurityDataProvider",
            typeof(EF6SecurityDataProvider).FullName);
        public static string SecurityMessageProviderClassName { get; internal set; } = GetProvider("SecurityMessageProvider", 
            typeof(DefaultMessageProvider).FullName);
        public static string DocumentPreviewProviderClassName { get; internal set; } = GetProvider("DocumentPreviewProvider",
            "SenseNet.Preview.DefaultDocumentPreviewProvider");
        public static string ClusterChannelProviderClassName { get; internal set; } = GetProvider("ClusterChannelProvider",
            typeof(VoidChannel).FullName);
        public static string SearchEngineClassName { get; internal set; } = GetProvider("SearchEngine",
            "SenseNet.Search.Lucene29.Lucene29SearchEngine");
        public static string MembershipExtenderClassName { get; internal set; } = GetProvider("MembershipExtender",
            "SenseNet.ContentRepository.Storage.Security.DefaultMembershipExtender");
        public static string CacheClassName { get; internal set; } = GetProvider("Cache",
            typeof(AspNetCache).FullName);

        public static string ElevatedModificationVisibilityRuleProviderName { get; internal set; } =
            GetProvider("ElevatedModificationVisibilityRuleProvider",
                "SenseNet.ContentRepository.SnElevatedModificationVisibilityRule");

        public static bool RepositoryPathProviderEnabled { get; internal set; } = GetValue<bool>(SectionName, "RepositoryPathProviderEnabled", true);

        private static string GetProvider(string key, string defaultValue = null)
        {
            return GetString(SectionName, key, defaultValue);
        }

        //===================================================================================== Instance

        /// <summary>
        /// Lets you access the replaceable providers in the system. This instance may be replaced 
        /// by a derived special implementation that stores instances on a thread context 
        /// for testing purposes.
        /// </summary>
        public static Providers Instance { get; set; } = new Providers();

        //===================================================================================== Named providers

        #region private Lazy<DataProvider> _dataProvider = new Lazy<DataProvider>
        private Lazy<DataProvider> _dataProvider = new Lazy<DataProvider>(() =>
        {
            var dbp = CreateProviderInstance<DataProvider>(DataProviderClassName, "DataProvider");
            
            CommonComponents.TransactionFactory = dbp;

            return dbp;
        });
        public virtual DataProvider DataProvider
        {
            get { return _dataProvider.Value; }
            set { _dataProvider = new Lazy<DataProvider>(() => value); }
        }
        #endregion

        #region private Lazy<ISearchEngine> _searchEngine = new Lazy<ISearchEngine>
        private Lazy<ISearchEngine> _searchEngine =
            new Lazy<ISearchEngine>(() => CreateProviderInstance<ISearchEngine>(SearchEngineClassName, "SearchEngine"));
        public virtual ISearchEngine SearchEngine
        {
            get { return _searchEngine.Value; }
            set { _searchEngine = new Lazy<ISearchEngine>(() => value); }
        }
        #endregion

        #region private Lazy<AccessProvider> _accessProvider = new Lazy<AccessProvider>
        private Lazy<AccessProvider> _accessProvider = new Lazy<AccessProvider>(() =>
        {
            var provider = CreateProviderInstance<AccessProvider>(AccessProviderClassName, "AccessProvider");
            provider.InitializeInternal();

            return provider;
        });
        public virtual AccessProvider AccessProvider
        {
            get { return _accessProvider.Value; }
            set { _accessProvider = new Lazy<AccessProvider>(() => value); }
        }
        #endregion

        #region private Lazy<ISecurityDataProvider> _securityDataProvider = new Lazy<ISecurityDataProvider>
        private Lazy<ISecurityDataProvider> _securityDataProvider = new Lazy<ISecurityDataProvider>(() =>
        {
            ISecurityDataProvider securityDataProvider = null;

            try
            {
                // if other than the known implementation, create it automatically
                if (string.Compare(SecurityDataProviderClassName, typeof(EF6SecurityDataProvider).FullName, StringComparison.Ordinal) != 0)
                    securityDataProvider = (ISecurityDataProvider)TypeResolver.CreateInstance(SecurityDataProviderClassName);
            }
            catch (TypeNotFoundException)
            {
                throw new ConfigurationException($"Security data provider implementation not found: {SecurityDataProviderClassName}");
            }
            catch (InvalidCastException)
            {
                throw new ConfigurationException($"Invalid security data provider implementation: {SecurityDataProviderClassName}");
            }

            if (securityDataProvider == null)
            {
                // default implementation
                securityDataProvider = new EF6SecurityDataProvider(
                    Security.SecurityDatabaseCommandTimeoutInSeconds,
                    ConnectionStrings.SecurityDatabaseConnectionString);
            }

            SnLog.WriteInformation("SecurityDataProvider created: " + securityDataProvider.GetType().FullName);

            return securityDataProvider;
        });
        public virtual ISecurityDataProvider SecurityDataProvider
        {
            get { return _securityDataProvider.Value; }
            set { _securityDataProvider = new Lazy<ISecurityDataProvider>(() => value); }
        }
        #endregion

        #region private Lazy<IMessageProvider> _securityMessageProvider = new Lazy<IMessageProvider>
        private Lazy<IMessageProvider> _securityMessageProvider = new Lazy<IMessageProvider>(() =>
        {
            var msgProvider = CreateProviderInstance<IMessageProvider>(SecurityMessageProviderClassName,
                "SecurityMessageProvider");
            msgProvider.Initialize();

            return msgProvider;
        });
        public virtual IMessageProvider SecurityMessageProvider
        {
            get { return _securityMessageProvider.Value; }
            set { _securityMessageProvider = new Lazy<IMessageProvider>(() => value); }
        }
        #endregion

        #region private Lazy<ElevatedModificationVisibilityRule> _elevatedModificationVisibilityRuleProvider
        private Lazy<ElevatedModificationVisibilityRule> _elevatedModificationVisibilityRuleProvider =
            new Lazy<ElevatedModificationVisibilityRule>(() => CreateProviderInstance<ElevatedModificationVisibilityRule>(
                ElevatedModificationVisibilityRuleProviderName, "ElevatedModificationVisibilityRule"));
        public virtual ElevatedModificationVisibilityRule ElevatedModificationVisibilityRuleProvider
        {
            get { return _elevatedModificationVisibilityRuleProvider.Value; }
            set { _elevatedModificationVisibilityRuleProvider = new Lazy<ElevatedModificationVisibilityRule>(() => value); }
        }
        #endregion

        #region private Lazy<MembershipExtenderBase> _membershipExtender = new Lazy<MembershipExtenderBase>
        private Lazy<MembershipExtenderBase> _membershipExtender = new Lazy<MembershipExtenderBase>(() => CreateProviderInstance<MembershipExtenderBase>(MembershipExtenderClassName, "MembershipExtender"));
        public virtual MembershipExtenderBase MembershipExtender
        {
            get { return _membershipExtender.Value; }
            set { _membershipExtender = new Lazy<MembershipExtenderBase>(() => value); }
        }
        #endregion
        
        #region private Lazy<ICache> _cacheProvider = new Lazy<ICache>
        private Lazy<ICache> _cacheProvider =
            new Lazy<ICache>(() => CreateProviderInstance<ICache>(CacheClassName, "CacheProvider"));
        public virtual ICache CacheProvider
        {
            get { return _cacheProvider.Value; }
            set { _cacheProvider = new Lazy<ICache>(() => value); }
        }
        #endregion

        #region private Lazy<IClusterChannel> _clusterChannelProvider = new Lazy<IClusterChannel>
        private Lazy<IClusterChannel> _clusterChannelProvider = new Lazy<IClusterChannel>(() =>
        {
            IClusterChannel provider;

            try
            {
                provider = (IClusterChannel)TypeResolver.CreateInstance(ClusterChannelProviderClassName, 
                    new BinaryMessageFormatter(), ClusterMemberInfo.Current);
            }
            catch (TypeNotFoundException)
            {
                throw new ConfigurationException($"ClusterChannel implementation does not exist: {ClusterChannelProviderClassName}");
            }
            catch (InvalidCastException)
            {
                throw new ConfigurationException($"Invalid ClusterChannel implementation: {ClusterChannelProviderClassName}");
            }
            
            provider.Start();

            SnTrace.Messaging.Write("Cluster channel created: " + ClusterChannelProviderClassName);
            SnLog.WriteInformation($"ClusterChannel created: {ClusterChannelProviderClassName}");

            return provider;
        });
        public virtual IClusterChannel ClusterChannelProvider
        {
            get { return _clusterChannelProvider.Value; }
            set { _clusterChannelProvider = new Lazy<IClusterChannel>(() => value); }
        }

        #endregion

        //UNDONE: :() Configure or not: IPermissionFilterFactory
        #region private Lazy<IPermissionFilterFactory> _permissionFilterFactory = new Lazy<IPermissionFilterFactory>
        private Lazy<IPermissionFilterFactory> _permissionFilterFactory = new Lazy<IPermissionFilterFactory>(() =>
        {
            return new SnPermissionFilterFactory();
        });
        public virtual IPermissionFilterFactory PermissionFilterFactory
        {
            get { return _permissionFilterFactory.Value; }
            set { _permissionFilterFactory = new Lazy<IPermissionFilterFactory>(() => value); }
        }

        #endregion

        //===================================================================================== General provider API

        private readonly Dictionary<string, object> _providersByName = new Dictionary<string, object>();
        private readonly Dictionary<Type, object> _providersByType = new Dictionary<Type, object>();

        public virtual T GetProvider<T>(string name) where T: class 
        {
            object provider;
            if (_providersByName.TryGetValue(name, out provider))
                return provider as T;

            return null;
        }
        public virtual T GetProvider<T>() where T : class
        {
            object provider;
            if (_providersByType.TryGetValue(typeof(T), out provider))
                return provider as T;

            return null;
        }

        public virtual void SetProvider(string providerName, object provider)
        {
            _providersByName[providerName] = provider;
        }
        public virtual void SetProvider(Type providerType, object provider)
        {
            _providersByType[providerType] = provider;
        }

        private static T CreateProviderInstance<T>(string className, string providerName)
        {
            T provider;

            try
            {
                provider = (T)TypeResolver.CreateInstance(className);
            }
            catch (TypeNotFoundException)
            {
                throw new ConfigurationException($"{providerName} implementation does not exist: {className}");
            }
            catch (InvalidCastException)
            {
                throw new ConfigurationException($"Invalid {providerName} implementation: {className}");
            }

            SnLog.WriteInformation($"{providerName} created: {className}");

            return provider;
        }
    }
}
