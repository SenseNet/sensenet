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
            public new bool IsWebContext { get; }

            /// <summary>
            /// Gets a value that is 'true' if your tool uses the Content search and any modification features (e.g. save, move etc.). 'True' is the default.
            /// </summary>
            public new bool StartIndexingEngine { get; }

            /// <summary>
            /// Gets or sets a value that is 'true' if your tool enables the running of workflow engine. 'True' is the default.
            /// </summary>
            public new bool StartWorkflowEngine { get; }

            /// <summary>
            /// Gets a local directory path of plugins if it is different from your tool's path. Default is null that means the plugins are placed in the appdomain's working directory.
            /// </summary>
            public new string PluginsPath { get; }

            /// <summary>
            /// Gets a local directory path of index if it is different from configured path. Default is false that means the application uses the configured index path.
            /// </summary>
            public new string IndexPath { get; }

            /// <summary>
            /// Gets a System.IO.TextWriter instance. Can be null. If it is not null, the startup sequence will be traced to given writer.
            /// </summary>
            public new System.IO.TextWriter Console { get; }

            /// <summary>
            /// Determines trace categories that should be enabled when the repository starts. This will
            /// override both startup and runtime categories.
            /// </summary>
            public new string[] TraceCategories { get; }
            /// <summary>
            /// Contains type matching configurations.
            /// Every item can contain one or more Type for the key Type.
            /// </summary>
            public new ReadOnlyDictionary<Type, Type[]> Providers { get; }

            internal ImmutableRepositoryStartSettings(RepositoryStartSettings settings)
            {
                IsWebContext = settings.IsWebContext;
                StartIndexingEngine = settings.StartIndexingEngine;
                StartWorkflowEngine = settings.StartWorkflowEngine;
                Console = settings.Console;
                PluginsPath = settings.PluginsPath;
                IndexPath = settings.IndexPath;
                Providers = new ReadOnlyDictionary<Type, Type[]>(settings.Providers);

                TraceCategories = settings.TraceCategories;
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

        public virtual bool IsWebContext { get; set; } = false;

        /// <summary>
        /// Gets or sets a value that is 'true' if your tool uses the Content search and any modification features (e.g. save, move etc.). 'True' is the default.
        /// </summary>
        /// <remarks>
        /// If your tool needs to run indexing and its running is postponed (StartIndexingEngine = false), call the RepositoryInstance.StartIndexingEngine() method.
        /// </remarks>
        public virtual bool StartIndexingEngine { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that is 'true' if your tool enables the running of workflow engine. 'True' is the default.
        /// </summary>
        /// <remarks>
        /// If your tool needs to run the workflow engine and its running is postponed (StartWorkflowEngine = false), call the RepositoryInstance.StartWorkflowEngine() method.
        /// </remarks>
        public virtual bool StartWorkflowEngine { get; set; } = true;

        /// <summary>
        /// Gets or sets a local directory path of plugins if it is different from your tool's path. Default is null that means the plugins are placed in the appdomain's working directory.
        /// </summary>
        public virtual string PluginsPath { get; set; }

        /// <summary>
        /// Gets or sets a local directory path of index if it is different from configured path. Default is false that means the application uses the configured index path.
        /// </summary>
        public virtual string IndexPath { get; set; }

        /// <summary>
        /// Gets or set a System.IO.TextWriter instance. Can be null. If it is not null, the startup sequence will be traced to given writer.
        /// </summary>
        public virtual System.IO.TextWriter Console { get; set; }

        /// <summary>
        /// Determines trace categories that should be enabled when the repository starts. This will
        /// override both startup and runtime categories.
        /// </summary>
        public virtual string[] TraceCategories { get; protected set; }

        /// <summary>
        /// Contains type matching configurations.
        /// Every item can contain one or more Type for the key Type.
        /// </summary>
        public virtual Dictionary<Type, Type[]> Providers { get; set; } = new Dictionary<Type, Type[]>();

        /// <summary>
        /// Upserts (inserts or updates if exists) a provider item.
        /// Provider item: one or more Type for the key Type.
        /// </summary>
        public virtual void ConfigureProvider(Type key, params Type[] bricks)
        {
            Providers[key] = bricks;
        }
        /// <summary>
        /// Adds a provider item.
        /// If the key exists, ArgumentException will be thrown.
        /// Provider item: one or more Type for the key Type.
        /// </summary>
        public virtual void AddProvider(Type key, params Type[] bricks)
        {
            Type[] value;
            if (!Providers.TryGetValue(key, out value))
            {
                Providers.Add(key, bricks);
                return;
            }
            ConfigureProvider(key, value.Union(bricks).ToArray());
        }
    }
}
