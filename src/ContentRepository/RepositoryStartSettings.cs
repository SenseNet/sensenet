﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;

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
            [Obsolete("Old-school configuration property. Do not use anymore.", true)]
            public new bool IsWebContext => false;
            public new bool ExecutingPatches { get; internal set; }

            public IServiceProvider Services { get; }

            /// <summary>
            /// Gets a value that is 'true' if your tool uses the Content search and any modification features (e.g. save, move etc.). 'True' is the default.
            /// </summary>
            public new bool StartIndexingEngine { get; }

            /// <summary>
            /// Gets or sets a value that is 'true' if your tool enables the running of workflow engine. 'True' is the default.
            /// </summary>
            [Obsolete("Old-school configuration property. Do not use anymore.", true)]
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

            internal ImmutableRepositoryStartSettings(RepositoryStartSettings settings)
            {
                ExecutingPatches = settings.ExecutingPatches;
                StartIndexingEngine = settings.StartIndexingEngine;
                Console = settings.Console;
                IndexPath = settings.IndexPath;

                TraceCategories = settings.TraceCategories;

                if (settings is RepositoryBuilder builder)
                    Services = builder.Services;
            }
        }

        [Obsolete("Old-school configuration property. Do not use anymore.", true)]
        public virtual bool IsWebContext => false;
        public virtual bool ExecutingPatches { get; set; } = false;

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
        [Obsolete("Old-school configuration property. Do not use anymore.", true)]
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
        public virtual string[] TraceCategories { get; protected internal set; }
    }
}
