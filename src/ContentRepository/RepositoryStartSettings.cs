using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Controls the startup sequence of the Repository. This class is used as a parameter of the Repository.Start(RepositoryStartSettings) method.
    /// The startup control information is also available (in read only way) in the RepositoryInstace which the Repository.Start method returns to.
    /// </summary>
    public class RepositoryStartSettings
    {
        /// <summary>
        /// Provides the control information of the startup sequence. 
        /// The instance of this class is the clone of the RepositoryStartSettings that was passed the Repository.Start(RepositoryStartSettings) method.
        /// </summary>
        public class ImmutableRepositoryStartSettings : RepositoryStartSettings
        {
            private new bool _isWebContext;
            private new bool _startLuceneManager;
            private new bool _startWorkflowEngine;
            private new string _pluginsPath;
            private new string _indexPath;
            private new System.IO.TextWriter _console;
            private new ReadOnlyDictionary<Type, Type[]> _providers;

            public new bool IsWebContext { get { return _isWebContext; } }
            /// <summary>
            /// Gets a value that is 'true' if your tool uses the Content search and any modification features (e.g. save, move etc.). 'True' is the default.
            /// </summary>
            public new bool StartLuceneManager { get { return _startLuceneManager; } }
            /// <summary>
            /// Gets or sets a value that is 'true' if your tool enables the running of workflow engine. 'True' is the default.
            /// </summary>
            public new bool StartWorkflowEngine { get { return _startWorkflowEngine; } }
            /// <summary>
            /// Gets a local directory path of plugins if it is different from your tool's path. Default is null that means the plugins are placed in the appdomain's working directory.
            /// </summary>
            public new string PluginsPath { get { return _pluginsPath; } }
            /// <summary>
            /// Gets a local directory path of index if it is different from configured path. Default is false that means the application uses the configured index path.
            /// </summary>
            public new string IndexPath { get { return _indexPath; } }
            /// <summary>
            /// Gets a System.IO.TextWriter instance. Can be null. If it is not null, the startup sequence will be traced to given writer.
            /// </summary>
            public new System.IO.TextWriter Console { get { return _console; } }
            /// <summary>
            /// Contains type matching configurations.
            /// Every item can contain one or more Type for the key Type.
            /// </summary>
            public new ReadOnlyDictionary<Type, Type[]> Providers
            {
                get { return _providers; }
                set { _providers = value; }
            }

            internal ImmutableRepositoryStartSettings(RepositoryStartSettings settings)
            {
                _isWebContext = settings._isWebContext;
                _startLuceneManager = settings.StartLuceneManager;
                _startWorkflowEngine = settings.StartWorkflowEngine;
                _console = settings.Console;
                _pluginsPath = settings.PluginsPath;
                _indexPath = settings.IndexPath;
                _providers = new ReadOnlyDictionary<Type, Type[]>(settings.Providers);
            }

            private const string NotSupportedMessage = "This method is not supported after calling Repository.Start method.";
            public override void AddProvider(Type key, params Type[] bricks)
            {
                throw new NotSupportedException(NotSupportedMessage);
            }
            public override void ConfigureProvider(Type key, params Type[] bricks)
            {
                throw new NotSupportedException(NotSupportedMessage);
            }
        }

        internal static readonly RepositoryStartSettings Default = new RepositoryStartSettings();

        private bool _isWebContext = false;
        private bool _startLuceneManager = true;
        private bool _startWorkflowEngine = true;
        private string _pluginsPath;
        private string _indexPath;
        private System.IO.TextWriter _console;
        private Dictionary<Type, Type[]> _providers = new Dictionary<Type, Type[]>();

        public virtual bool IsWebContext
        {
            get { return _isWebContext; }
            set { _isWebContext = value; }
        }
        /// <summary>
        /// Gets or sets a value that is 'true' if your tool uses the Content search and any modification features (e.g. save, move etc.). 'True' is the default.
        /// </summary>
        /// <remarks>
        /// If your tool needs to run Lucene and its running is postponed (StartLuceneManager = false), call the RepositoryInstance.StartLucene() method.
        /// </remarks>
        public virtual bool StartLuceneManager
        {
            get { return _startLuceneManager; }
            set { _startLuceneManager = value; }
        }
        /// <summary>
        /// Gets or sets a value that is 'true' if your tool enables the running of workflow engine. 'True' is the default.
        /// </summary>
        /// <remarks>
        /// If your tool needs to run the workflow engine and its running is postponed (StartWorkflowEngine = false), call the RepositoryInstance.StartWorkflowEngine() method.
        /// </remarks>
        public virtual bool StartWorkflowEngine
        {
            get { return _startWorkflowEngine; }
            set { _startWorkflowEngine = value; }
        }
        /// <summary>
        /// Gets or sets a local directory path of plugins if it is different from your tool's path. Default is null that means the plugins are placed in the appdomain's working directory.
        /// </summary>
        public virtual string PluginsPath
        {
            get { return _pluginsPath; }
            set { _pluginsPath = value; }
        }
        /// <summary>
        /// Gets or sets a local directory path of index if it is different from configured path. Default is false that means the application uses the configured index path.
        /// </summary>
        public virtual string IndexPath
        {
            get { return _indexPath; }
            set { _indexPath = value; }
        }
        /// <summary>
        /// Gets or set a System.IO.TextWriter instance. Can be null. If it is not null, the startup sequence will be traced to given writer.
        /// </summary>
        public virtual System.IO.TextWriter Console
        {
            get { return _console; }
            set { _console = value; }
        }
        /// <summary>
        /// Contains type matching configurations.
        /// Every item can contain one or more Type for the key Type.
        /// </summary>
        public virtual Dictionary<Type, Type[]> Providers
        {
            get { return _providers; }
            set { _providers = value; }
        }

        /// <summary>
        /// Upserts (inserts or updates if exists) a provider item.
        /// Provider item: one or more Type for the key Type.
        /// </summary>
        public virtual void ConfigureProvider(Type key, params Type[] bricks)
        {
            _providers[key] = bricks;
        }
        /// <summary>
        /// Adds a provider item.
        /// If the key exists, ArgumentException will be thrown.
        /// Provider item: one or more Type for the key Type.
        /// </summary>
        public virtual void AddProvider(Type key, params Type[] bricks)
        {
            Type[] value;
            if (!_providers.TryGetValue(key, out value))
            {
                _providers.Add(key, bricks);
                return;
            }
            ConfigureProvider(key, value.Union(bricks).ToArray());
        }
    }
}
