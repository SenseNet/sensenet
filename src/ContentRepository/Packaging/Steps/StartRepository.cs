using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;

namespace SenseNet.Packaging.Steps
{
    public class StartRepository : Step
    {
        private bool _startIndexingEngineChanged;
        private bool _startIndexingEngine;

        [Obsolete("Use the StartIndexingEngine property instead.")]
        public bool StartLuceneManager
        {
            get => StartIndexingEngine;
            set => StartIndexingEngine = value;
        }
        public bool StartIndexingEngine
        {
            get => _startIndexingEngine;
            set
            {
                _startIndexingEngine = value;
                _startIndexingEngineChanged = true;
            }
        }

        public string PluginsPath { get; set; }
        public string IndexPath { get; set; }
        public bool StartWorkflowEngine { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.Console.WriteLine("Starting ... ");

            var indexPath = IndexPath;
            if (IndexPath == null)
            {
                indexPath = Configuration.Indexing.IndexDirectoryPath;
                if (string.IsNullOrEmpty(indexPath))
                {
                    indexPath = System.IO.Path.Combine(context.TargetPath, Configuration.Indexing.DefaultLocalIndexDirectory);
                }
                else
                {
                    if (!System.IO.Path.IsPathRooted(indexPath))
                        indexPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(context.TargetPath, indexPath));
                }
            }
            var startIndexingEngine = _startIndexingEngineChanged ? StartIndexingEngine : SearchManager.IsOuterEngineEnabled;

            Logger.LogMessage("startIndexingEngine: " + startIndexingEngine);
            Logger.LogMessage("indexPath: " + indexPath);

            var startSettings = context.RepositoryStartSettings ?? new RepositoryStartSettings
            {
                StartIndexingEngine = startIndexingEngine,
                PluginsPath = PluginsPath ?? context.SandboxPath,
                IndexPath = indexPath,
                StartWorkflowEngine = StartWorkflowEngine,
                Console = context.Console
            };

            Repository.Start(startSettings);

            context.Console.WriteLine("Ok.");

            var trace = RepositoryInstance.Instance.StartupTrace;

            context.Console.WriteLine("Assemblies:");
            context.Console.WriteLine("  References: {0}.", trace.ReferencedAssemblies.Length);
            context.Console.WriteLine("  Loaded before start: {0}.", trace.AssembliesBeforeStart.Length);
            context.Console.WriteLine("  Plugins: {0}.", trace.Plugins.Length);

            context.RepositoryStarted = true;
        }
    }
}
