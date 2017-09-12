using System;
using System.Collections.Generic;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
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
            typeof(ContentRepository.Storage.Security.SenseNetPasswordHashProvider).FullName);
        public static string OutdatedPasswordHashProviderClassName { get; internal set; } = GetProvider("OutdatedPasswordHashProvider",
            typeof(ContentRepository.Storage.Security.Sha256PasswordHashProviderWithoutSalt).FullName);
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
            DataProvider dbp;

            try
            {
                dbp = (DataProvider)TypeResolver.CreateInstance(DataProviderClassName);
            }
            catch (TypeNotFoundException)
            {
                throw new ConfigurationException($"{SR.Exceptions.Configuration.Msg_DataProviderImplementationDoesNotExist}: {DataProviderClassName}");
            }
            catch (InvalidCastException)
            {
                throw new ConfigurationException(SR.Exceptions.Configuration.Msg_InvalidDataProviderImplementation);
            }

            CommonComponents.TransactionFactory = dbp;
            SnLog.WriteInformation("DataProvider created: " + DataProviderClassName);

            return dbp;
        });
        public virtual DataProvider DataProvider
        {
            get { return _dataProvider.Value; }
            set { _dataProvider = new Lazy<DataProvider>(() => value); }
        }
        #endregion

        #region private Lazy<AccessProvider> _accessProvider = new Lazy<AccessProvider>
        private Lazy<AccessProvider> _accessProvider = new Lazy<AccessProvider>(() =>
        {
            try
            {
                var provider = (AccessProvider)TypeResolver.CreateInstance(AccessProviderClassName);
                provider.InitializeInternal();

                SnLog.WriteInformation("AccessProvider created: " + AccessProviderClassName);

                return provider;
            }
            catch (TypeNotFoundException) // rethrow
            {
                throw new ConfigurationException($"{SR.Exceptions.Configuration.Msg_AccessProviderImplementationDoesNotExist}: {AccessProviderClassName}");
            }
            catch (InvalidCastException) // rethrow
            {
                throw new ConfigurationException($"{SR.Exceptions.Configuration.Msg_InvalidAccessProviderImplementation}: {AccessProviderClassName}");
            }
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

        #region private Lazy<ElevatedModificationVisibilityRule> _elevatedModificationVisibilityRuleProvider
        private Lazy<ElevatedModificationVisibilityRule> _elevatedModificationVisibilityRuleProvider =
            new Lazy<ElevatedModificationVisibilityRule>(() =>
            {
                try
                {
                    return (ElevatedModificationVisibilityRule)TypeResolver.CreateInstance(ElevatedModificationVisibilityRuleProviderName);
                }
                catch (TypeNotFoundException)
                {
                    throw new ConfigurationException($"Elevated modification visibility rule provider implementation not found: {ElevatedModificationVisibilityRuleProviderName}");
                }
                catch (InvalidCastException)
                {
                    throw new ConfigurationException($"Invalid Elevated modification visibility rule provider implementation: {ElevatedModificationVisibilityRuleProviderName}");
                }
            });
        public virtual ElevatedModificationVisibilityRule ElevatedModificationVisibilityRuleProvider
        {
            get { return _elevatedModificationVisibilityRuleProvider.Value; }
            set { _elevatedModificationVisibilityRuleProvider = new Lazy<ElevatedModificationVisibilityRule>(() => value); }
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
    }
}
