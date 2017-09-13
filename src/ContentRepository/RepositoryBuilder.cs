using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;
using SenseNet.Security.Messaging;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Settings and provider builder class that controls the startup options and provider
    /// instances when a sensenet ECM repository starts.
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
            return this;
        }
        /// <summary>
        /// Sets the access provider responsible for user-related technical operations in the system.
        /// </summary>
        /// <param name="accessProvider">AccessProvider instance.</param>
        public RepositoryBuilder UseAccessProvider(AccessProvider accessProvider)
        {
            Configuration.Providers.Instance.AccessProvider = accessProvider;
            return this;
        }
        /// <summary>
        /// Sets the security data provider used for all security db operations in the system.
        /// </summary>
        /// <param name="securityDataProvider">ISecurityDataProvider instance.</param>
        public RepositoryBuilder UseSecurityDataProvider(ISecurityDataProvider securityDataProvider)
        {
            Configuration.Providers.Instance.SecurityDataProvider = securityDataProvider;
            return this;
        }
        /// <summary>
        /// Sets the security message provider used for security messaging operations.
        /// </summary>
        /// <param name="securityMessageProvider">IMessageProvider instance that will handle security-related messages.</param>
        public RepositoryBuilder UseSecurityMessageProvider(IMessageProvider securityMessageProvider)
        {
            Configuration.Providers.Instance.SecurityMessageProvider = securityMessageProvider;
            return this;
        }
        /// <summary>
        /// Sets the elevated modification visibility rule provider.
        /// </summary>
        public RepositoryBuilder UseElevatedModificationVisibilityRuleProvider(ElevatedModificationVisibilityRule modificationVisibilityRuleProvider)
        {
            Configuration.Providers.Instance.ElevatedModificationVisibilityRuleProvider = modificationVisibilityRuleProvider;
            return this;
        }
        /// <summary>
        /// Sets the search engine used for querying and indexing.
        /// </summary>
        /// <param name="searchEngine">SearchEngine instance.</param>
        public RepositoryBuilder UseSearchEngine(ISearchEngine searchEngine)
        {
            Configuration.Providers.Instance.SearchEngine = searchEngine;
            return this;
        }
        /// <summary>
        /// Sets the membership extender used for extending user membership on-the-fly.
        /// </summary>
        /// <param name="membershipExtender">MembershipExtender instance.</param>
        public RepositoryBuilder UseMembershipExtender(MembershipExtenderBase membershipExtender)
        {
            Configuration.Providers.Instance.MembershipExtender = membershipExtender;
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
            return this;
        }

        /// <summary>
        /// Set this value to false if your tool does not need Content search and modification features 
        /// (e.g. save, move etc.). Default is true.
        /// </summary>
        /// <remarks>
        /// If your tool needs to query for content and querying is switched off by this method, 
        /// you may call the RepositoryInstance.StartLucene() method later.
        /// </remarks>
        public new RepositoryBuilder StartLuceneManager(bool start = true)
        {
            base.StartLuceneManager = start;
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
    }
}
