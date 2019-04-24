using System;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

namespace SenseNet.Packaging.Steps
{
    public class PopulateIndex : Step
    {
        [DefaultProperty]
        [Annotation("Optional path of the subtree to populate. Default: /Root.")]
        public string Path { get; set; }

        [Annotation("Optional level of rebuilding the index. Two values are accepted: 'IndexOnly' (default) and 'DatabaseAndIndex'.")]
        public string Level { get; set; }

        private long _count;
        private long _docCount;
        private long _versionCount;

        private ExecutionContext _context;

        public override void Execute(ExecutionContext context)
        {
            ExecuteInternal(context);
        }
        // Method for indexing tests.
        internal void ExecuteInternal(ExecutionContext context,
            EventHandler<NodeIndexedEventArgs> refreshed = null,
            EventHandler<NodeIndexedEventArgs> indexed = null,
            EventHandler<NodeIndexingErrorEventArgs> error = null)
        {
            context.AssertRepositoryStarted();

            var path = context.ResolveVariable(Path) as string;
            var level = context.ResolveVariable(Level) as string;
            IndexRebuildLevel rebuildLevel;
            try
            {
                rebuildLevel = string.IsNullOrEmpty(level)
                    ? IndexRebuildLevel.IndexOnly
                    : (IndexRebuildLevel)Enum.Parse(typeof(IndexRebuildLevel), level, true);
            }
            catch (Exception e)
            {
                throw new PackagingException(SR.Errors.InvalidParameter + ": Level", e, PackagingExceptionType.InvalidStepParameter);
            }

            _context = context;
            _versionCount = DataStore.Enabled ? DataStore.GetVersionCountAsync(path).Result : DataProvider.GetVersionCount(path); //DB:??

            var savedMode = RepositoryEnvironment.WorkingMode.Populating;
            RepositoryEnvironment.WorkingMode.Populating = true;

            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += Populator_NodeIndexed;
            populator.IndexDocumentRefreshed += Populator_IndexDocumentRefreshed;
            populator.IndexingError += Populator_IndexingError;
            if (refreshed != null)
                populator.IndexDocumentRefreshed += refreshed;
            if (indexed != null)
                populator.NodeIndexed += indexed;
            if (error != null)
                populator.IndexingError += error;

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

                context.Console.Write("                                                      \r");
                context.Console.WriteLine();
            }

            Logger.LogMessage("...finished: " + _count + " items indexed.");
        }
        private DateTime _lastWriteTime = DateTime.MinValue;

        private void Populator_IndexDocumentRefreshed(object sender, NodeIndexedEventArgs e)
        {
            Interlocked.Increment(ref _docCount);
            if (DateTime.Now.AddSeconds(-1) < _lastWriteTime)
                return;
            _context.Console.Write($"  Document refreshing progress: {_docCount}/{_versionCount} {_docCount * 100 / _versionCount}%  \r");
            _lastWriteTime = DateTime.Now;
        }
        private void Populator_NodeIndexed(object sender, NodeIndexedEventArgs e)
        {
            Interlocked.Increment(ref _count);
            if (DateTime.Now.AddSeconds(-1) < _lastWriteTime)
                return;
            _context.Console.Write($"  Indexing progress: {_count}/{_versionCount} {_count * 100 / _versionCount}%               \r");
            _lastWriteTime = DateTime.Now;
        }
        private void Populator_IndexingError(object sender, NodeIndexingErrorEventArgs e)
        {
            Logger.LogException(e.Exception, $"Indexing error: NodeId: {e.NodeId}, VersionId: {e.VersionId}, Path: {e.Path}");
        }
    }
}
