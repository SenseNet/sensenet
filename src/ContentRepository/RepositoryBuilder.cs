﻿using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Security;
using SenseNet.Security.Messaging;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Settings and provider builder class that controls the startup options and provider
    /// instances when a sensenet repository starts.
    /// </summary>
    public class RepositoryBuilder : RepositoryStartSettings
    {
        /// <summary>
        /// Sets the data provider used for all db operations in the system.
        /// </summary>
        /// <param name="dataProvider">DataProvider instance.</param>
        public RepositoryBuilder UseDataProvider(DataProvider dataProvider)
        {
            Configuration.Providers.Instance.DataProvider = dataProvider;
            WriteLog("DataProvider", dataProvider);

            if (dataProvider is ITransactionFactory)
            {
                CommonComponents.TransactionFactory = dataProvider;
                WriteLog("TransactionFactory", dataProvider);
            }

            return this;
        }
        /// <summary>
        /// Sets the blob metadata provider.
        /// </summary>
        /// <param name="metaDataProvider">IBlobStorageMetaDataProvider instance.</param>
        public RepositoryBuilder UseBlobMetaDataProvider(IBlobStorageMetaDataProvider metaDataProvider)
        {
            Configuration.Providers.Instance.BlobMetaDataProvider = metaDataProvider;
            WriteLog("BlobMetaDataProvider", metaDataProvider);

            return this;
        }
        /// <summary>
        /// Sets the blob provider selector.
        /// </summary>
        /// <param name="selector">IBlobProviderSelector instance.</param>
        public RepositoryBuilder UseBlobProviderSelector(IBlobProviderSelector selector)
        {
            Configuration.Providers.Instance.BlobProviderSelector = selector;
            WriteLog("BlobProviderSelector", selector);

            return this;
        }
        /// <summary>
        /// Sets the access provider responsible for user-related technical operations in the system.
        /// </summary>
        /// <param name="accessProvider">AccessProvider instance.</param>
        public RepositoryBuilder UseAccessProvider(AccessProvider accessProvider)
        {
            Configuration.Providers.Instance.AccessProvider = accessProvider;
            WriteLog("AccessProvider", accessProvider);

            return this;
        }
        /// <summary>
        /// Sets the permission filter factory responsible for creating a filter for every query execution.
        /// </summary>
        /// <param name="permissionFilterFactory">IPermissionFilterFactory implementation instance.</param>
        public RepositoryBuilder UsePermissionFilterFactory(IPermissionFilterFactory permissionFilterFactory)
        {
            Configuration.Providers.Instance.PermissionFilterFactory = permissionFilterFactory;
            WriteLog("PermissionFilterFactory", permissionFilterFactory);

            return this;
        }
        /// <summary>
        /// Sets the security data provider used for all security db operations in the system.
        /// </summary>
        /// <param name="securityDataProvider">ISecurityDataProvider instance.</param>
        public RepositoryBuilder UseSecurityDataProvider(ISecurityDataProvider securityDataProvider)
        {
            Configuration.Providers.Instance.SecurityDataProvider = securityDataProvider;
            WriteLog("SecurityDataProvider", securityDataProvider);

            return this;
        }
        /// <summary>
        /// Sets the security message provider used for security messaging operations.
        /// </summary>
        /// <param name="securityMessageProvider">IMessageProvider instance that will handle security-related messages.</param>
        public RepositoryBuilder UseSecurityMessageProvider(IMessageProvider securityMessageProvider)
        {
            Configuration.Providers.Instance.SecurityMessageProvider = securityMessageProvider;
            WriteLog("SecurityMessageProvider", securityMessageProvider);

            return this;
        }
        /// <summary>
        /// Sets the cache provider.
        /// </summary>
        /// <param name="cacheProvider">ICache instance.</param>
        public RepositoryBuilder UseCacheProvider(ICache cacheProvider)
        {
            Configuration.Providers.Instance.CacheProvider = cacheProvider;
            WriteLog("CacheProvider", cacheProvider);

            return this;
        }
        /// <summary>
        /// Sets the application cache provider.
        /// </summary>
        /// <param name="applicationCacheProvider">IApplicationCache instance.</param>
        public RepositoryBuilder UseApplicationCacheProvider(IApplicationCache applicationCacheProvider)
        {
            Configuration.Providers.Instance.ApplicationCacheProvider = applicationCacheProvider;
            WriteLog("ApplicationCacheProvider", applicationCacheProvider);

            return this;
        }
        /// <summary>
        /// Sets the cluster channel provider.
        /// </summary>
        /// <param name="clusterChannelProvider">IClusterChannel instance.</param>
        public RepositoryBuilder UseClusterChannelProvider(IClusterChannel clusterChannelProvider)
        {
            Configuration.Providers.Instance.ClusterChannelProvider = clusterChannelProvider;
            WriteLog("ClusterChannelProvider", clusterChannelProvider);

            return this;
        }
        /// <summary>
        /// Sets the elevated modification visibility rule provider.
        /// </summary>
        public RepositoryBuilder UseElevatedModificationVisibilityRuleProvider(ElevatedModificationVisibilityRule modificationVisibilityRuleProvider)
        {
            Configuration.Providers.Instance.ElevatedModificationVisibilityRuleProvider = modificationVisibilityRuleProvider;
            WriteLog("ElevatedModificationVisibilityRuleProvider", modificationVisibilityRuleProvider);

            return this;
        }
        /// <summary>
        /// Sets the search engine used for querying and indexing.
        /// </summary>
        /// <param name="searchEngine">SearchEngine instance.</param>
        public RepositoryBuilder UseSearchEngine(ISearchEngine searchEngine)
        {
            Configuration.Providers.Instance.SearchEngine = searchEngine;
            WriteLog("SearchEngine", searchEngine);

            return this;
        }
        /// <summary>
        /// Sets the membership extender used for extending user membership on-the-fly.
        /// </summary>
        /// <param name="membershipExtender">MembershipExtender instance.</param>
        public RepositoryBuilder UseMembershipExtender(MembershipExtenderBase membershipExtender)
        {
            Configuration.Providers.Instance.MembershipExtender = membershipExtender;
            WriteLog("MembershipExtender", membershipExtender);

            return this;
        }
        /// <summary>
        /// Sets trace categories that should be enabled when the repository starts. This will
        /// override both startup and runtime categories, but will not switch any category off.
        /// </summary> 
        public RepositoryBuilder UseTraceCategories(params string[] categoryNames)
        {
            this.TraceCategories = categoryNames;
            return this;
        }
        /// <summary>
        /// Sets logger instances.
        /// </summary>
        public RepositoryBuilder UseLogger(params IEventLogger[] logger)
        {
            // store loggers in the provider collection temporarily
            Configuration.Providers.Instance.SetProvider(typeof(IEventLogger[]), logger);
            return this;
        }
        /// <summary>
        /// Sets tracer instances.
        /// </summary>
        public RepositoryBuilder UseTracer(params ISnTracer[] tracer)
        {
            // store tracers in the provider collection temporarily
            Configuration.Providers.Instance.SetProvider(typeof(ISnTracer[]), tracer);
            return this;
        }

        /// <summary>
        /// General API for defining a provider instance that will be injected into and can be loaded
        /// from the Providers.Instance store.
        /// </summary>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="provider">Provider instance.</param>
        public RepositoryBuilder UseProvider(string providerName, object provider)
        {
            Configuration.Providers.Instance.SetProvider(providerName, provider);
            WriteLog(providerName, provider);

            return this;
        }
        /// <summary>
        /// General API for defining a provider instance that will be injected into and can be loaded
        /// from the Providers.Instance store.
        /// </summary>
        /// <param name="providerType">Type of the provider.</param>
        /// <param name="provider">Provider instance.</param>
        public RepositoryBuilder UseProvider(Type providerType, object provider)
        {
            Configuration.Providers.Instance.SetProvider(providerType, provider);
            WriteLog(providerType?.Name ?? "Null provider", provider);

            return this;
        }

        /// <summary>
        /// Set this value to false if your tool does not need Content search and modification features 
        /// (e.g. save, move etc.). Default is true.
        /// </summary>
        /// <remarks>
        /// If your tool needs to query for content and querying is switched off by this method, 
        /// you may call the RepositoryInstance.StartIndexingEngine() method later.
        /// </remarks>
        public new RepositoryBuilder StartIndexingEngine(bool start = true)
        {
            base.StartIndexingEngine = start;
            return this;
        }
        public new RepositoryBuilder IsWebContext(bool webContext = false)
        {
            base.IsWebContext = webContext;
            return this;
        }
        /// <summary>
        /// Instructs the system to start the workflow engine during startup.
        /// </summary>
        /// <remarks>
        /// If your tool needs to run the workflow engine and its running is postponed (StartWorkflowEngine = false), 
        /// call the RepositoryInstance.StartWorkflowEngine() method.
        /// </remarks>
        public new RepositoryBuilder StartWorkflowEngine(bool start = true)
        {
            base.StartWorkflowEngine = start;
            return this;
        }
        /// <summary>
        /// Sets a local directory path of plugins if it is different from your tool's path. 
        /// Default is null that means the plugins are placed in the appdomain's working directory.
        /// </summary>
        public RepositoryBuilder SetPluginsPath(string path)
        {
            PluginsPath = path;
            return this;
        }
        /// <summary>
        /// Sets a local directory path of index if it is different from configured path. 
        /// Default is empty that means the application uses the configured index path.
        /// </summary>
        public RepositoryBuilder SetIndexPath(string path)
        {
            IndexPath = path;
            return this;
        }
        /// <summary>
        /// Sets a TextWriter instance. Can be null. If it is not null, the startup sequence 
        /// will be traced to the provided textwriter.
        /// </summary>
        public RepositoryBuilder SetConsole(System.IO.TextWriter console)
        {
            Console = console;
            return this;
        }

        /// <summary>
        /// Disables one or more node observers in the system. If you call it without parameters, 
        /// it will disable all available node observers.
        /// </summary>
        public RepositoryBuilder DisableNodeObservers(params Type[] nodeObserverTypes)
        {
            if (nodeObserverTypes == null || nodeObserverTypes.Length == 0)
            {
                Configuration.Providers.Instance.NodeObservers = new NodeObserver[0];
            }
            else
            {
                var observers = Configuration.Providers.Instance.NodeObservers;

                // remove only the provided types
                Configuration.Providers.Instance.NodeObservers =
                    observers.Where(o => !nodeObserverTypes.Contains(o.GetType())).ToArray();
            }
            return this;
        }
        /// <summary>
        /// Enables one or more node observers.
        /// </summary>
        public RepositoryBuilder EnableNodeObservers(params Type[] nodeObserverTypes)
        {
            if (nodeObserverTypes != null && nodeObserverTypes.Any())
            {
                var observers = new List<NodeObserver>(Configuration.Providers.Instance.NodeObservers);

                // add missing observer instances
                foreach (var nodeObserverType in nodeObserverTypes)
                {
                    if (observers.All(no => no.GetType() != nodeObserverType))
                    {
                        observers.Add((NodeObserver)Activator.CreateInstance(nodeObserverType, true));
                    }
                }

                Configuration.Providers.Instance.NodeObservers = observers.ToArray();
            }

            return this;
        }

        private static void WriteLog(string name, object provider)
        {
            var message = $"{name} configured: {provider?.GetType().FullName ?? "null"}";

            SnTrace.Repository.Write(message);
            SnLog.WriteInformation(message);
        }
    }
}
