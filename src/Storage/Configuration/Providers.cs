﻿using System;
using System.Collections.Generic;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Tools;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Events;
using SenseNet.Search.Querying;
using SenseNet.Tools.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Providers : SnConfig
    {
        private const string SectionName = "sensenet/providers";

        public static string EventLoggerClassName { get; internal set; } = GetProvider("EventLogger");
        public static string PropertyCollectorClassName { get; internal set; } = GetProvider("PropertyCollector",
            "SenseNet.Diagnostics.EventPropertyCollector");
        public static string AuditEventWriterClassName { get; internal set; } = GetProvider("AuditEventWriter",
            typeof(DatabaseAuditEventWriter).FullName);
        public static string AccessProviderClassName { get; internal set; } = GetProvider("AccessProvider",
            "SenseNet.ContentRepository.Security.DesktopAccessProvider");
        public static string ContentNamingProviderClassName { get; internal set; } = GetProvider("ContentNamingProvider");
        public static string TaskManagerClassName { get; internal set; } = GetProvider("TaskManager");
        public static string PasswordHashProviderClassName { get; internal set; } = GetProvider("PasswordHashProvider",
            typeof(SenseNetPasswordHashProvider).FullName);
        public static string OutdatedPasswordHashProviderClassName { get; internal set; } = GetProvider("OutdatedPasswordHashProvider",
            typeof(Sha256PasswordHashProviderWithoutSalt).FullName);
        public static string SkinManagerClassName { get; internal set; } = GetProvider("SkinManager", "SenseNet.Portal.SkinManager");
        public static string DirectoryProviderClassName { get; internal set; } = GetProvider("DirectoryProvider");
        public static string SecurityDataProviderClassName { get; internal set; } = GetProvider("SecurityDataProvider",
            "SenseNet.Security.EF6SecurityStore.EF6SecurityDataProvider");
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
            typeof(SnMemoryCache).FullName);

        public static string IndexDocumentProviderClassName { get; internal set; } = "SenseNet.ContentRepository.Search.Indexing.IndexDocumentProvider";

        public static string ApplicationCacheClassName { get; internal set; } = GetProvider("ApplicationCache", "SenseNet.ContentRepository.ApplicationCache");

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

        #region private Lazy<IEventLogger> _eventLogger = new Lazy<IEventLogger>

        private Lazy<IEventLogger> _eventLogger = new Lazy<IEventLogger>(() =>
            string.IsNullOrEmpty(EventLoggerClassName)
                ? new SnEventLogger(Logging.EventLogName, Logging.EventLogSourceName)
                : CreateProviderInstance<IEventLogger>(EventLoggerClassName, "EventLogger"));
        public virtual IEventLogger EventLogger
        {
            get => _eventLogger.Value;
            set { _eventLogger = new Lazy<IEventLogger>(() => value); }
        }
        #endregion

        #region private Lazy<IEventPropertyCollector> _propertyCollector = new Lazy<IEventPropertyCollector>

        private Lazy<IEventPropertyCollector> _propertyCollector = new Lazy<IEventPropertyCollector>(() =>
            string.IsNullOrEmpty(PropertyCollectorClassName)
                ? new EventPropertyCollector()
                : CreateProviderInstance<IEventPropertyCollector>(PropertyCollectorClassName, "PropertyCollector"));
        public virtual IEventPropertyCollector PropertyCollector
        {
            get => _propertyCollector.Value;
            set { _propertyCollector = new Lazy<IEventPropertyCollector>(() => value); }
        }
        #endregion

        #region private Lazy<IAuditEventWriter> _auditEventWriter = new Lazy<IAuditEventWriter>
        private Lazy<IAuditEventWriter> _auditEventWriter = new Lazy<IAuditEventWriter>(() =>
        {
            var aewr = CreateProviderInstance<IAuditEventWriter>(AuditEventWriterClassName, "AuditEventWriter");
            return aewr;
        });
        public virtual IAuditEventWriter AuditEventWriter
        {
            get { return _auditEventWriter.Value; }
            set { _auditEventWriter = new Lazy<IAuditEventWriter>(() => value); }
        }
        #endregion

        #region DataProvider & DataStore

        public virtual DataProvider DataProvider { get; set; }

        public virtual IDataStore DataStore { get; set; }

        /// <summary>
        /// Internal method for initializing the data provider and data store instances from the service container.
        /// DO NOT USE THIS METHOD IN YOUR CODE
        /// </summary>
        public void InitializeDataProvider(IServiceProvider provider)
        {
            if (DataProvider == null)
                DataProvider = provider?.GetRequiredService<DataProvider>();

            if (DataStore == null)
                InitializeDataStore();
        }

        /// <summary>
        /// Internal method for initializing the data store instance.
        /// DO NOT USE THIS METHOD IN YOUR CODE
        /// </summary>
        public void InitializeDataStore()
        {
            // This method is a temporary solution for initializing the datastore instance
            // without starting the whole repository.

            DataStore = new DataStore(DataProvider);
        }

        #endregion

        #region IBlobStorageMetaDataProvider

        public virtual IBlobStorageMetaDataProvider BlobMetaDataProvider { get; set; }

        #endregion

        #region IBlobProviderSelector

        public virtual IBlobProviderSelector BlobProviderSelector { get; set; }

        #endregion

        #region BlobStorage
        
        /// <summary>
        /// Legacy property for old APIs.
        /// </summary>
        public IBlobStorage BlobStorage { get; set; }

        #endregion

        public void ResetBlobProviders()
        {
            BlobStorage = null;
            BlobProviderSelector = null;
            BlobMetaDataProvider = null;
            BlobProviders.Clear();

            // add default internal blob provider
            BlobProviders.AddProvider(new BuiltInBlobProvider(Options.Create(DataOptions.GetLegacyConfiguration()),
                Options.Create(ConnectionStringOptions.GetLegacyConnectionStrings())));
        }

        public void InitializeBlobProviders()
        {
            // add built-in provider manually if necessary
            if (!BlobProviders.Values.Any(bp => bp is IBuiltInBlobProvider))
            {
                BlobProviders.AddProvider(new BuiltInBlobProvider(Options.Create(DataOptions.GetLegacyConfiguration()),
                    Options.Create(ConnectionStringOptions.GetLegacyConnectionStrings())));
            }

            if (BlobProviderSelector == null)
            {
                BlobProviderSelector = new BuiltInBlobProviderSelector(BlobProviders,
                    null, Options.Create(BlobStorageOptions.GetLegacyConfiguration()));
            }

            if (BlobMetaDataProvider == null)
                BlobMetaDataProvider = new MsSqlBlobMetaDataProvider(BlobProviders,
                    Options.Create(DataOptions.GetLegacyConfiguration()),
                    Options.Create(BlobStorageOptions.GetLegacyConfiguration()),
                    Options.Create(ConnectionStringOptions.GetLegacyConnectionStrings()));

            // assemble the main api instance if necessary (for tests)
            if (BlobStorage == null)
            {
                BlobStorage = new ContentRepository.Storage.Data.BlobStorage(
                    BlobProviders,
                    BlobProviderSelector,
                    BlobMetaDataProvider,
                    Options.Create(BlobStorageOptions.GetLegacyConfiguration()));
            }

            BlobStorage.Initialize();
        }

        public IBlobProviderStore BlobProviders { get; set; } = new BlobProviderStore(Array.Empty<IBlobProvider>());

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
            // We have to skip logging the creation of this provider, because the logger
            // itself tries to use the access provider when collecting event properties,
            // which would lead to a circular reference.
            var provider = CreateProviderInstance<AccessProvider>(AccessProviderClassName, "AccessProvider", true);
            provider.InitializeInternal();

            return provider;
        });
        public virtual AccessProvider AccessProvider
        {
            get => _accessProvider.Value;
            set { _accessProvider = new Lazy<AccessProvider>(() => value); }
        }
        #endregion

        public virtual ISecurityDataProvider SecurityDataProvider { get; set; }
        
        #region private Lazy<IPreviewProvider> _previewProvider = new Lazy<IPreviewProvider>
        private Lazy<IPreviewProvider> _previewProvider = new Lazy<IPreviewProvider>(() => 
            CreateProviderInstance<IPreviewProvider>(DocumentPreviewProviderClassName,"DocumentPreviewProvider"));

        /// <summary>
        /// Preview provider instance. Do NOT set this property directly,
        /// because it has to be an instance of the DocumentPreviewProvider class
        /// that resides in the ContentRepository layer.
        /// Use the extension methods on the IRepositoryBuilder api instead.
        /// </summary>
        public virtual IPreviewProvider PreviewProvider
        {
            get => _previewProvider.Value;
            set { _previewProvider = new Lazy<IPreviewProvider>(() => value); }
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
        private Lazy<ISnCache> _cacheProvider =
            new Lazy<ISnCache>(() =>
            {
                var cache = CreateProviderInstance<ISnCache>(CacheClassName, "CacheProvider", true);
                cache.Events = new CacheEventStore();
                return cache;
            });
        public virtual ISnCache CacheProvider
        {
            get => _cacheProvider.Value;
            set { _cacheProvider = new Lazy<ISnCache>(() =>
            {
                value.Events = new CacheEventStore();
                return value;
            }); }
        }
        #endregion

        #region private Lazy<IApplicationCache> _applicationCacheProvider = new Lazy<IApplicationCache>
        private Lazy<IApplicationCache> _applicationCacheProvider =
            new Lazy<IApplicationCache>(() => CreateProviderInstance<IApplicationCache>(ApplicationCacheClassName, "ApplicationCacheProvider"));
        public virtual IApplicationCache ApplicationCacheProvider
        {
            get { return _applicationCacheProvider.Value; }
            set { _applicationCacheProvider = new Lazy<IApplicationCache>(() => value); }
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
            
            provider.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

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

        #region private Lazy<IPermissionFilterFactory> _permissionFilterFactory = new Lazy<IPermissionFilterFactory>
        private Lazy<IPermissionFilterFactory> _permissionFilterFactory = new Lazy<IPermissionFilterFactory>(() =>
        {
            return new PermissionFilterFactory();
        });
        public virtual IPermissionFilterFactory PermissionFilterFactory
        {
            get { return _permissionFilterFactory.Value; }
            set { _permissionFilterFactory = new Lazy<IPermissionFilterFactory>(() => value); }
        }

        #endregion

        #region NodeObservers
        private Lazy<NodeObserver[]> _nodeObservers = new Lazy<NodeObserver[]>(() =>
        {
            var nodeObserverTypes = TypeResolver.GetTypesByBaseType(typeof(NodeObserver));
            var activeObservers = nodeObserverTypes.Where(t => !t.IsAbstract).Select(t => (NodeObserver)Activator.CreateInstance(t, true))
                .Where(n => !RepositoryEnvironment.DisabledNodeObservers.Contains(n.GetType().FullName)).ToArray();

            if (SnTrace.Repository.Enabled)
            {
                SnTrace.Repository.Write("NodeObservers (count: {0}):", nodeObserverTypes.Length);
                for (var i = 0; i < nodeObserverTypes.Length; i++)
                {
                    var observerType = nodeObserverTypes[i];
                    var fullName = observerType.FullName;
                    SnTrace.Repository.Write("  #{0} ({1}): {2}:{3} // {4}",
                        i + 1,
                        RepositoryEnvironment.DisabledNodeObservers.Contains(fullName) ? "disabled" : "active",
                        observerType.Name,
                        observerType.BaseType?.Name,
                        observerType.Assembly.GetName().Name);
                }
            }

            var activeObserverNames = activeObservers.Select(x => x.GetType().FullName).ToArray();
            SnLog.WriteInformation("NodeObservers are instantiated. ", EventId.RepositoryLifecycle,
                properties: new Dictionary<string, object> { { "Types", string.Join(", ", activeObserverNames) } });

            return activeObservers;
        });
        public NodeObserver[] NodeObservers
        {
            get { return _nodeObservers.Value; }
            set
            {
                _nodeObservers = new Lazy<NodeObserver[]>(() => value);

                SnLog.WriteInformation("NodeObservers have changed. ", EventId.RepositoryLifecycle,
                    properties: new Dictionary<string, object>
                        {{"Types", string.Join(", ", value?.Select(n => n.GetType().Name) ?? Array.Empty<string>())}});
            }
        }
        #endregion

        #region private Lazy<IIndexDocumentProvider> _indexDocumentProvider = new Lazy<IIndexDocumentProvider>
        private Lazy<IIndexDocumentProvider> _indexDocumentProvider = new Lazy<IIndexDocumentProvider>(() =>
            CreateProviderInstance<IIndexDocumentProvider>(IndexDocumentProviderClassName, "IndexDocumentProvider"));
        public virtual IIndexDocumentProvider IndexDocumentProvider
        {
            get { return _indexDocumentProvider.Value; }
            set { _indexDocumentProvider = new Lazy<IIndexDocumentProvider>(() => value); }
        }

        #endregion

        #region ContentProtector
        private Lazy<ContentProtector> _contentProtector =
            new Lazy<ContentProtector>(() => new ContentProtector());
        public virtual ContentProtector ContentProtector
        {
            get => _contentProtector.Value;
            set { _contentProtector = new Lazy<ContentProtector>(() => value); }
        }

        #endregion

        public StorageSchema StorageSchema { get; set; } = new StorageSchema();

        public ICompatibilitySupport CompatibilitySupport { get; set; } =
            new EmptyCompatibilitySupport();

        private IEventDistributor _eventDistributor = new DevNullEventDistributor();
        public IEventDistributor EventDistributor
        {
            get => _eventDistributor;
            set => _eventDistributor = value ?? new DevNullEventDistributor();
        }

        public IEventProcessor AuditLogEventProcessor { get; set; }
        public List<IEventProcessor> AsyncEventProcessors { get; } = new List<IEventProcessor>();

        //===================================================================================== General provider API

        private readonly Dictionary<string, object> _providersByName = new Dictionary<string, object>();
        private readonly Dictionary<Type, object> _providersByType = new Dictionary<Type, object>();

        public virtual T GetProvider<T>(string name) where T: class 
        {
            // Test cached instance if there is.
            if (!_providersByName.TryGetValue(name, out var provider))
            {
                // Try to resolve by configuration
                // 1 - read classname from configuration.
                var className = GetProvider(name);

                // 2 - resolve provider instance.
                provider = className == null ? null : TypeResolver.CreateInstance(className);

                // 3 - memorize even if null.
                SetProvider(name, provider);
            }

            return provider as T;
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

        private static T CreateProviderInstance<T>(string className, string providerName, bool skipLog = false)
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

            // in some cases the logger is not available yet
            if (skipLog)
                SnTrace.System.Write($"{providerName} created: {className}");
            else
                SnLog.WriteInformation($"{providerName} created: {className}");

            return provider;
        }

        /* =================================================================================== COMPONENTS FOR PATCHES */
        /* =================================================================================== Experimental feature   */

        public List<object> Components { get; } = new List<object>();
        public void AddComponent(object instance)
        {
            Components.Add(instance);
        }
    }
}
