using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.ContentRepository
{
    public class RepositoryBuilder : RepositoryStartSettings
    {
        //======================================================================== Internal properties

        internal DataProvider DataProvider { get; private set; }
        internal AccessProvider AccessProvider { get; private set; }
        internal ISecurityDataProvider SecurityDataProvider { get; private set; }
        internal ElevatedModificationVisibilityRule ElevatedModificationVisibilityRuleProvider { get; private set; }

        internal Dictionary<string, object> ProvidersByName { get; } = new Dictionary<string, object>();
        internal Dictionary<Type, object> ProvidersByType { get; } = new Dictionary<Type, object>();

        //======================================================================== Public API

        /// <summary>
        /// Sets the data provider used for all db operations in the system.
        /// </summary>
        /// <param name="dataProvider">DataProvider instance.</param>
        public RepositoryBuilder UseDataProvider(DataProvider dataProvider)
        {
            this.DataProvider = dataProvider;
            return this;
        }
        /// <summary>
        /// Sets the access provider responsible for user-related technical operations in the system.
        /// </summary>
        /// <param name="accessProvider">AccessProvider instance.</param>
        public RepositoryBuilder UseAccessProvider(AccessProvider accessProvider)
        {
            this.AccessProvider = accessProvider;
            return this;
        }
        /// <summary>
        /// Sets the security data provider used for all security db operations in the system.
        /// </summary>
        /// <param name="securityDataProvider">ISecurityDataProvider instance.</param>
        public RepositoryBuilder UseSecurityDataProvider(ISecurityDataProvider securityDataProvider)
        {
            this.SecurityDataProvider = securityDataProvider;
            return this;
        }

        public RepositoryBuilder UseElevatedModificationVisibilityRuleProvider(ElevatedModificationVisibilityRule modificationVisibilityRuleProvider)
        {
            this.ElevatedModificationVisibilityRuleProvider = modificationVisibilityRuleProvider;
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
            ProvidersByName[providerName] = provider;
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
            ProvidersByType[providerType] = provider;
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
        public new RepositoryBuilder RestoreIndex(bool restore = true)
        {
            base.RestoreIndex = restore;
            return this;
        }
        public new RepositoryBuilder IsWebContext(bool webContext = false)
        {
            base.IsWebContext = webContext;
            return this;
        }
        /// <summary>
        /// Gets or sets a value that is 'true' if the Lucene index will be backed up before your tool exits. Default: false
        /// </summary>
        public new RepositoryBuilder BackupIndexAtTheEnd(bool backup = false)
        {
            base.BackupIndexAtTheEnd = backup;
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
