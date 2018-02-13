using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
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

        [Annotation("Optional configuration. Two values are accepted: 'IndexOnly' (default) and 'DatabaseAndIndex'.")]
        public string Level { get; set; }

        private long _count;
        private long _versionCount;
        private long _factor;

        private ExecutionContext _context;

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var path = context.ResolveVariable(Path) as string;
            var level = context.ResolveVariable(Level) as string;
            IndexRebuildLevel rebuildLevel;
            try
            {
                rebuildLevel = string.IsNullOrEmpty(level)
                    ? IndexRebuildLevel.IndexOnly
                    : (IndexRebuildLevel) Enum.Parse(typeof(IndexRebuildLevel), level, true);
            }
            catch(Exception e)
            {
                throw new PackagingException(SR.Errors.InvalidParameter + ": Level", e, PackagingExceptionType.InvalidStepParameter);
            }

            _context = context;
            _versionCount = DataProvider.GetVersionCount(path);
            _factor = Math.Max(_versionCount / 60, 1);

            var savedMode = RepositoryEnvironment.WorkingMode.Populating;
            RepositoryEnvironment.WorkingMode.Populating = true;

            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += Populator_NodeIndexed;

            try
            {
                if (string.IsNullOrEmpty(path) && rebuildLevel == IndexRebuildLevel.IndexOnly)
                {
                    Logger.LogMessage($"Populating new index of the whole Content Repository...");
                    populator.ClearAndPopulateAll(context.Console);
                }
                else
                {
                    if (string.IsNullOrEmpty(path))
                        path = Identifiers.RootPath;
                    Logger.LogMessage($"Populating index for {path}, level={rebuildLevel} ...");
                    populator.RebuildIndexDirectly(path, rebuildLevel);
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
        private void Populator_NodeIndexed(object sender, NodeIndexedEventArgs e)
        {
            _count++;

            if (_count % _factor == 0)
                _context.Console.Write("|");
        }
    }
}
