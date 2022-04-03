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
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Events;
using SenseNet.Search.Querying;
using SenseNet.Tools.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SenseNet.ContentRepository.Search;
using SenseNet.Storage;
using SenseNet.TaskManagement.Core;
using EventId = SenseNet.Diagnostics.EventId;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Providers : SnConfig
    {
        private const string SectionName = "sensenet/providers";

        public static string AccessProviderClassName => "SenseNet.ContentRepository.Security.DesktopAccessProvider";
        public static string DirectoryProviderClassName => null;
        public static string MembershipExtenderClassName => "SenseNet.ContentRepository.Storage.Security.DefaultMembershipExtender";
        public static bool RepositoryPathProviderEnabled { get;  } = GetValue<bool>(SectionName, "RepositoryPathProviderEnabled", true);

        private static string GetProvider(string key, string defaultValue = null)
        {
            return GetString(SectionName, key, defaultValue);
        }

        //===================================================================================== Instance

        public IServiceProvider Services { get; }

        public Providers(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            CacheProvider = services.GetService<ISnCache>();
            ApplicationCacheProvider = services.GetService<IApplicationCache>();
            IndexDocumentProvider = services.GetService<IIndexDocumentProvider>();
            AuditEventWriter = services.GetService<IAuditEventWriter>();
            PreviewProvider = services.GetService<IPreviewProvider>();
            PropertyCollector = services.GetService<IEventPropertyCollector>();
            SecurityHandler = services.GetService<SecurityHandler>();
            SecurityMessageProvider = services.GetService<IMessageProvider>();
            PasswordHashProvider = services.GetService<IPasswordHashProvider>();
            PasswordHashProviderForMigration = services.GetService<IPasswordHashProviderForMigration>();
            ContentNamingProvider = services.GetService<IContentNamingProvider>();
            TaskManager = services.GetService<ITaskManager>();
            ElevatedModificationVisibilityRuleProvider = services.GetService<ElevatedModificationVisibilityRule>();

            SearchManager = services.GetService<ISearchManager>();
            IndexManager = services.GetService<IIndexManager>();
            IndexPopulator = services.GetService<IIndexPopulator>();

        }

        /// <summary>
        /// Lets you access the replaceable providers in the system. This instance may be replaced 
        /// by a derived special implementation that stores instances on a thread context 
        /// for testing purposes.
        /// </summary>
        public static Providers Instance { get; set; }

        //===================================================================================== Named providers

        public IEventLogger EventLogger { get; set; }

        public IEventPropertyCollector PropertyCollector { get; [Obsolete]set; }

        public IAuditEventWriter AuditEventWriter { get; }

        #region DataProvider & DataStore

        public virtual DataProvider DataProvider { get; set; }

        public virtual IDataStore DataStore { get; set; }

        /// <summary>
        /// Internal method for initializing the data provider and data store instances from the service container.
        /// DO NOT USE THIS METHOD IN YOUR CODE
        /// </summary>
        public void InitializeDataProvider(IServiceProvider provider)
        {
            DataProvider ??= provider?.GetRequiredService<DataProvider>();

            if (DataStore == null)
                InitializeDataStore(provider);

            InitializeTreeLock(provider);
        }

        /// <summary>
        /// Internal method for initializing the data store instance and any other service instance that depends on it.
        /// DO NOT USE THIS METHOD IN YOUR CODE
        /// </summary>
        public void InitializeDataStore(IServiceProvider provider = null)
        {
            // This method is a temporary solution for initializing the datastore instance and other dependent service
            // instance without starting the whole repository.

            DataStore = new DataStore(DataProvider, 
                provider?.GetService<ILogger<DataStore>>() ?? NullLoggerFactory.Instance.CreateLogger<DataStore>());
            
            InitializeTreeLock(provider);
        }

        private void InitializeTreeLock(IServiceProvider provider = null)
        {
            TreeLock ??= new TreeLockController(DataStore,
                provider?.GetService<ILogger<TreeLock>>() ?? NullLoggerFactory.Instance.CreateLogger<TreeLock>());
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

        public void ResetBlobProviders(ConnectionStringOptions connectionStrings)
        {
            BlobStorage = null;
            BlobProviderSelector = null;
            BlobMetaDataProvider = null;
            BlobProviders.Clear();

            // add default internal blob provider
            BlobProviders.AddProvider(new BuiltInBlobProvider(Options.Create(DataOptions.GetLegacyConfiguration()),
                Options.Create(connectionStrings)));
        }

        public void InitializeBlobProviders(ConnectionStringOptions connectionStrings)
        {
            // add built-in provider manually if necessary
            if (!BlobProviders.Values.Any(bp => bp is IBuiltInBlobProvider))
            {
                BlobProviders.AddProvider(new BuiltInBlobProvider(Options.Create(DataOptions.GetLegacyConfiguration()),
                    Options.Create(connectionStrings)));
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
                    Options.Create(connectionStrings));

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

        private IBlobProviderStore _blobProviders;
        public IBlobProviderStore BlobProviders
        {
            get
            {
                if (_blobProviders == null)
                    //UNDONE: Delete initialization and use something like this everywhere: repositoryBuilder.UseBlobProviderStore(services.GetRequiredService<IBlobProviderStore>())
                    _blobProviders = new BlobProviderStore(Array.Empty<IBlobProvider>());
                return _blobProviders;
            }
            set => _blobProviders = value;
        }

        public ISearchEngine SearchEngine { get; set; }
        public ISearchManager SearchManager { get; [Obsolete]set; }
        public IIndexManager IndexManager { get; [Obsolete]set; }
        public IIndexPopulator IndexPopulator { get; [Obsolete]set; }

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

        public ISecurityDataProvider SecurityDataProvider { get; set; }
        public IMessageProvider SecurityMessageProvider { get; set; }

        public SecurityHandler SecurityHandler { get; }
        public IPasswordHashProvider PasswordHashProvider { get; }
        public IPasswordHashProviderForMigration PasswordHashProviderForMigration { get; }

        public IContentNamingProvider ContentNamingProvider { get; }

        public IPreviewProvider PreviewProvider { get; set; }

        public ElevatedModificationVisibilityRule ElevatedModificationVisibilityRuleProvider { get; }

        #region private Lazy<MembershipExtenderBase> _membershipExtender = new Lazy<MembershipExtenderBase>
        private Lazy<MembershipExtenderBase> _membershipExtender = new Lazy<MembershipExtenderBase>(() => CreateProviderInstance<MembershipExtenderBase>(MembershipExtenderClassName, "MembershipExtender"));
        public virtual MembershipExtenderBase MembershipExtender
        {
            get { return _membershipExtender.Value; }
            set { _membershipExtender = new Lazy<MembershipExtenderBase>(() => value); }
        }
        #endregion

        public ISnCache CacheProvider { get; }

        public IApplicationCache ApplicationCacheProvider { get; }

        public virtual IClusterChannel ClusterChannelProvider { get; set; } =
            new VoidChannel(new BinaryMessageFormatter(), ClusterMemberInfo.Current);

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

        public IIndexDocumentProvider IndexDocumentProvider { get; }

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

        public ITreeLockController TreeLock { get; set; } // Initialized by InitializeDataStore method in this instance.

        public ITaskManager TaskManager { get; set; }

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
