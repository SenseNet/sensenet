using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

// ReSharper disable CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public partial class ReindexBinaries : Step
    {
        private static readonly string TracePrefix = "ReindexBinaries:";

        private readonly object _consoleSync = new object();
        private volatile int _reindexMetadataProgress;
        private volatile int _nodeCount;
        private volatile int _taskCount;

        public override void Execute(ExecutionContext context)
        {
            SnTrace.Index.Enabled = true;

            DataHandler.InstallTables();
            ReindexMetadata();
            DataHandler.StartBackgroundTasks();
        }
        private void ReindexMetadata()
        {
            using (new SystemAccount())
            {
                var nodeIds = DataHandler.GetAllNodeIds(0);
                _nodeCount = nodeIds.Count;

                Parallel.ForEach(new NodeList<Node>(nodeIds),
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    n =>
                {
                    foreach (var node in n.LoadVersions())
                        ReindexNode(node);
                    lock(_consoleSync)
                        Console.Write($"\r Tasks: {_taskCount} ({_reindexMetadataProgress} / {_nodeCount})  ");
                });
                Console.WriteLine();
            }
        }
        private void ReindexNode(Node node)
        {
            var indx = DataBackingStore.SaveIndexDocument(node, true, false, out var hasBinary);
            if (hasBinary)
                CreateBinaryReindexTask(node,
                    indx.IsLastPublic ? 1 : indx.IsLastDraft ? 2 : 3);
            _reindexMetadataProgress++;
        }
        private void CreateBinaryReindexTask(Node node, int rank)
        {
            DataHandler.CreateTempTask(node.VersionId, rank);
            _taskCount++;
            SnTrace.Index.Write($"{TracePrefix} Task created for version #{node.VersionId} {node.Version} of node #{node.Id}: {node.Path}");
        }

        /* =============================================================== */
        private static bool _featureIsRunning;
        private static bool _featureIsRequested;
        /// <summary>
        /// Gets some background task from the database and executes them.
        /// Returns true if there are no more tasks.
        /// Triggered by the SnMaintenance via the ReindexBinariesTask.
        /// </summary>
        public static bool GetBackgroundTasksAndExecute(int taskCount = 0, int timeoutInMinutes = 0)
        {
            if (_featureIsRunning)
            {
                _featureIsRequested = true;
                return false;
            }
            _featureIsRunning = true;
            _featureIsRequested = false;

            do
            {
                var versionIds = DataHandler.AssignTasks(
                    taskCount > 0 ? taskCount : 5, //UNDONE: finalize tesk count and timeout
                    timeoutInMinutes > 0 ? timeoutInMinutes : 10,
                    out var remainingTasks);
                if (remainingTasks == 0)
                {
                    _featureIsRequested = false;
                    _featureIsRunning = false;
                    return true;
                }
                foreach (var versionId in versionIds)
                {
                    ReindexBinaryProperties(versionId);
                    DataHandler.FinishTask(versionId);
                }
            } while (_featureIsRequested);

            _featureIsRunning = false;
            return false;
        }
        private static void ReindexBinaryProperties(int versionId)
        {
            using (new SystemAccount())
            {
                var node = Node.LoadNodeByVersionId(versionId);
                var indx = SearchManager.LoadIndexDocumentByVersionId(versionId);
                DataBackingStore.SaveIndexDocument(node, indx);
            }
        }

        public static bool IsFeatureActive()
        {
            if (Process.GetCurrentProcess().ProcessName.Equals("SnAdminRuntime", StringComparison.InvariantCultureIgnoreCase))
                return false;

            return DataHandler.CheckFeature();
        }

        public static void InactivateFeature()
        {
            DataHandler.DropTables();
            SnTrace.Index.Write($"{TracePrefix} Feature is inactivated.");
        }
    }
}
