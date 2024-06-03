using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the IRepositoryBuilder interface to let developers
    /// configure providers during the repository start process.
    /// </summary>
    public static class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Sets the <see cref="InitialData"/> that will be installed to the database in the repository start sequence.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="initialData">Data file instance.</param>
        /// <returns></returns>
        public static IRepositoryBuilder UseInitialData(this IRepositoryBuilder repositoryBuilder, InitialData initialData)
        {
            if (!(repositoryBuilder is RepositoryBuilder repoBuilder))
                throw new ApplicationException(
                    $"The repositoryBuilder is not an instance of {typeof(RepositoryBuilder).FullName}.");

            repoBuilder.InitialData = initialData;
            return repositoryBuilder;
        }

        /// <summary>
        /// Sets the blob metadata provider.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="metaDataProvider">IBlobStorageMetaDataProvider instance.</param>
        public static IRepositoryBuilder UseBlobMetaDataProvider(this IRepositoryBuilder repositoryBuilder, IBlobStorageMetaDataProvider metaDataProvider)
        {
            Configuration.Providers.Instance.BlobMetaDataProvider = metaDataProvider;

            WriteLog("BlobMetaDataProvider", metaDataProvider);

            return repositoryBuilder;
        }
        
        /// <summary>
        /// Sets the blob provider selector.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="selector">IBlobProviderSelector instance.</param>
        public static IRepositoryBuilder UseBlobProviderSelector(this IRepositoryBuilder repositoryBuilder, IBlobProviderSelector selector)
        {
            Configuration.Providers.Instance.BlobProviderSelector = selector;
            WriteLog("BlobProviderSelector", selector);

            return repositoryBuilder;
        }

        /// <summary>
        /// Sets the blob provider store.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="store">IBlobProviderStore instance.</param>
        public static IRepositoryBuilder UseBlobProviderStore(this IRepositoryBuilder repositoryBuilder, IBlobProviderStore store)
        {
            Configuration.Providers.Instance.BlobProviders = store;
            WriteLog("IBlobProviderStore", store);

            return repositoryBuilder;
        }

        /// <summary>
        /// Legacy API for tests and tools. In production use the AddSenseNetBlobProvider method
        /// instead to register a blob provider service in DI.
        /// </summary>
        public static IRepositoryBuilder AddBlobProvider(this IRepositoryBuilder repositoryBuilder,
            IBlobProvider provider)
        {
            Configuration.Providers.Instance.BlobProviders.AddProvider(provider);
            return repositoryBuilder;
        }

        /// <summary>
        /// Sets the access provider responsible for user-related technical operations in the system.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="accessProvider">AccessProvider instance.</param>
        public static IRepositoryBuilder UseAccessProvider(this IRepositoryBuilder repositoryBuilder, AccessProvider accessProvider)
        {
            Configuration.Providers.Instance.AccessProvider = accessProvider;
            WriteLog("AccessProvider", accessProvider);

            return repositoryBuilder;
        }

        /// <summary>
        /// Sets the cluster channel provider.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="clusterChannelProvider">IClusterChannel instance.</param>
        public static IRepositoryBuilder UseClusterChannelProvider(this IRepositoryBuilder repositoryBuilder, IClusterChannel clusterChannelProvider)
        {
            Configuration.Providers.Instance.ClusterChannelProvider = clusterChannelProvider;
            WriteLog("ClusterChannelProvider", clusterChannelProvider);

            return repositoryBuilder;
        }

        /// <summary>
        /// Sets the search engine used for querying and indexing.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="searchEngine">SearchEngine instance.</param>
        public static IRepositoryBuilder UseSearchEngine(this IRepositoryBuilder repositoryBuilder, ISearchEngine searchEngine)
        {
            Configuration.Providers.Instance.SearchEngine = searchEngine;
            WriteLog("SearchEngine", searchEngine);

            return repositoryBuilder;
        }

        /// <summary>
        /// Sets trace categories that should be enabled when the repository starts. This will
        /// override both startup and runtime categories, but will not switch any category off.
        /// </summary> 
        public static IRepositoryBuilder UseTraceCategories(this IRepositoryBuilder repositoryBuilder, params string[] categoryNames)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
            {
                repoBuilder.TraceCategories = repoBuilder.TraceCategories == null
                    ? categoryNames
                    : repoBuilder.TraceCategories.Union(categoryNames).Distinct().ToArray();
            }
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }

        /// <summary>
        /// Sets the logger instance.
        /// </summary>
        public static IRepositoryBuilder UseLogger(this IRepositoryBuilder repositoryBuilder, IEventLogger logger)
        {
            Configuration.Providers.Instance.EventLogger = logger;

            return repositoryBuilder;
        }

        /// <summary>
        /// Adds the registered IEventLogger instance to the repository builder.
        /// </summary>
        public static IRepositoryBuilder UseLogger(this IRepositoryBuilder repositoryBuilder, IServiceProvider provider)
        {
            var logger = provider.GetService<IEventLogger>();
            if (logger != null)
                repositoryBuilder.UseLogger(logger);

            // stores a logger instance for later use
            var genericLogger = provider.GetService<ILogger<SnILogger>>();
            if (genericLogger != null)
                repositoryBuilder.SetProvider<ILogger<SnILogger>>(genericLogger);

            return repositoryBuilder;
        }

        /// <summary>
        /// General API for defining a provider instance that will be injected into and can be loaded
        /// from the Providers.Instance store.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="provider">Provider instance.</param>
        public static IRepositoryBuilder UseProvider(this IRepositoryBuilder repositoryBuilder, string providerName, object provider)
        {
            repositoryBuilder.SetProvider(providerName, provider);

            return repositoryBuilder;
        }

        /// <summary>
        /// General API for defining a provider instance that will be injected into and can be loaded
        /// from the Providers.Instance store.
        /// </summary>
        /// <param name="repositoryBuilder"></param>
        /// <param name="provider">Provider instance.</param>
        public static IRepositoryBuilder UseProvider(this IRepositoryBuilder repositoryBuilder, object provider)
        {
            repositoryBuilder.SetProvider(provider);

            return repositoryBuilder;
        }
        
        /// <summary>
        /// Registers one or more features in the system, represented by component instances.
        /// Do not use this method directly in your code, it is intended to be used by the system.
        /// Use the AddComponent extension method for the IServiceCollection api instead.
        /// </summary>
        public static IRepositoryBuilder UseComponent(this IRepositoryBuilder repositoryBuilder, params ISnComponent[] components)
        {
            foreach (var component in components)
            {
                Configuration.Providers.Instance.AddComponent(component);
            }

            return repositoryBuilder;
        }

        /// <summary>
        /// Set this value to false if your tool does not need Content search and modification features 
        /// (e.g. save, move etc.). Default is true.
        /// </summary>
        /// <remarks>
        /// If your tool needs to query for content and querying is switched off by this method, 
        /// you may call the RepositoryInstance.StartIndexingEngine() method later.
        /// </remarks>
        public static IRepositoryBuilder StartIndexingEngine(this IRepositoryBuilder repositoryBuilder, bool start = true)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
                repoBuilder.StartIndexingEngine = start;
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }
        public static IRepositoryBuilder IsWebContext(this IRepositoryBuilder repositoryBuilder, bool webContext = false)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
                repoBuilder.IsWebContext = webContext;
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }
        /// <summary>
        /// Instructs the system to start the workflow engine during startup.
        /// </summary>
        /// <remarks>
        /// If your tool needs to run the workflow engine and its running is postponed (StartWorkflowEngine = false), 
        /// call the RepositoryInstance.StartWorkflowEngine() method.
        /// </remarks>
        public static IRepositoryBuilder StartWorkflowEngine(this IRepositoryBuilder repositoryBuilder, bool start = true)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
                repoBuilder.StartWorkflowEngine = start;
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }
        /// <summary>
        /// Sets a local directory path of plugins if it is different from your tool's path. 
        /// Default is null that means the plugins are placed in the appdomain's working directory.
        /// </summary>
        public static IRepositoryBuilder SetPluginsPath(this IRepositoryBuilder repositoryBuilder, string path)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
                repoBuilder.PluginsPath = path;
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }
        /// <summary>
        /// Sets a local directory path of index if it is different from configured path. 
        /// Default is empty that means the application uses the configured index path.
        /// </summary>
        public static IRepositoryBuilder SetIndexPath(this IRepositoryBuilder repositoryBuilder, string path)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
                repoBuilder.IndexPath = path;
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }
        /// <summary>
        /// Sets a TextWriter instance. Can be null. If it is not null, the startup sequence 
        /// will be traced to the provided textwriter.
        /// </summary>
        public static IRepositoryBuilder SetConsole(this IRepositoryBuilder repositoryBuilder, System.IO.TextWriter console)
        {
            // Old behavior: set a property on the instance that will be used
            // by the repo start process later.
            if (repositoryBuilder is RepositoryBuilder repoBuilder)
                repoBuilder.Console = console;
            else
                throw new NotImplementedException();

            return repositoryBuilder;
        }

        /// <summary>
        /// Disables one or more node observers in the system. If you call it without parameters, 
        /// it will disable all available node observers.
        /// </summary>
        [Obsolete("Use IServiceCollection.RemoveNodeObserver or RemoveAllNodeObservers method instead.", true)]
        public static IRepositoryBuilder DisableNodeObservers(this IRepositoryBuilder repositoryBuilder, params Type[] nodeObserverTypes)
        {
            return repositoryBuilder;
        }
        /// <summary>
        /// Enables one or more node observers.
        /// </summary>
        [Obsolete("Use IServiceCollection.AddNodeObserver method instead.", true)]
        public static IRepositoryBuilder EnableNodeObservers(this IRepositoryBuilder repositoryBuilder, params Type[] nodeObserverTypes)
        {
            return repositoryBuilder;
        }

        public static IRepositoryBuilder SetProvider<T>(this IRepositoryBuilder repositoryBuilder, T provider)
        {
            var providerType = typeof(T);
            Configuration.Providers.Instance.SetProvider(providerType, provider);
            WriteLog(providerType.Name, provider);

            return repositoryBuilder;
        }

        private static void WriteLog(string name, object provider)
        {
            RepositoryBuilder.WriteLog(name, provider);
        }
    }
}
