using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using System.Configuration;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Steps
{
    public class StartRepository : Step
    {
        private bool _startLuceneManagerChanged;
        private bool _startLuceneManager;
        public bool StartLuceneManager
        {
            get { return _startLuceneManager; }
            set
            {
                _startLuceneManager = value;
                _startLuceneManagerChanged = true;
            }
        }

        public string PluginsPath { get; set; }
        public string IndexPath { get; set; }
        public bool StartWorkflowEngine { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.Console.Write("Starting ... ");

            var indexPath = IndexPath;
            if (IndexPath == null)
            {
                indexPath = Configuration.Indexing.IndexDirectoryPath;
                if (string.IsNullOrEmpty(indexPath))
                {
                    indexPath = System.IO.Path.Combine(context.TargetPath, "App_Data\\LuceneIndex");
                }
                else
                {
                    if (!System.IO.Path.IsPathRooted(indexPath))
                        indexPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(context.TargetPath, indexPath));
                }
            }
            var startLuceneManager = _startLuceneManagerChanged ? StartLuceneManager : StorageContext.Search.IsOuterEngineEnabled;

            context.Console.WriteLine("startLuceneManager: " + startLuceneManager);
            context.Console.WriteLine("indexPath: " + indexPath);

            Repository.Start(new RepositoryStartSettings
            {
                StartLuceneManager = startLuceneManager,
                PluginsPath = PluginsPath ?? context.SandboxPath,
                IndexPath = indexPath,
                StartWorkflowEngine = StartWorkflowEngine
            });

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
