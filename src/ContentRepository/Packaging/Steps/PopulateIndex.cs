using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Packaging.Steps
{
    public class PopulateIndex : Step
    {
        [DefaultProperty]
        [Annotation("Optional path of the subtree to populate. Default: /Root.")]
        public string Path { get; set; }

        private long _count;
        private long _versionCount;
        private long _factor;

        private ExecutionContext _context;

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var path = context.ResolveVariable(Path) as string;

            _context = context;
            _versionCount = DataProvider.GetVersionCount(path);
            _factor = Math.Max(_versionCount / 60, 1);

            var savedMode = RepositoryEnvironment.WorkingMode.Populating;
            RepositoryEnvironment.WorkingMode.Populating = true;

            var populator = StorageContext.Search.SearchEngine.GetPopulator();
            populator.NodeIndexed += Populator_NodeIndexed;

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    Logger.LogMessage($"Populating index of the whole Content Repository...");
                    populator.ClearAndPopulateAll(context.Console);
                }
                else
                {
                    Logger.LogMessage($"Populating index for {path}...");
                    populator.RepopulateTree(path);
                }
            }
            finally
            {
                populator.NodeIndexed -= Populator_NodeIndexed;
                RepositoryEnvironment.WorkingMode.Populating = savedMode;

                context.Console.WriteLine();
            }

            Logger.LogMessage("...finished: " + _count + " items indexed.");
        }
        private void Populator_NodeIndexed(object sender, NodeIndexedEvenArgs e)
        {
            _count++;

            if (_count % _factor == 0)
                _context.Console.Write("|");
        }
    }
}
