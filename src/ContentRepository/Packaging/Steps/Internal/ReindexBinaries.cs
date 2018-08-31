using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using Retrier = SenseNet.Tools.Retrier;

// ReSharper disable CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public partial class ReindexBinaries : Step
    {
        private static readonly string TraceCategory = "REINDEX";

        // ReSharper disable once InconsistentNaming
        private static SnTrace.SnTraceCategory __tracer;
        private static SnTrace.SnTraceCategory Tracer
        {
            get
            {
                if (__tracer == null)
                {
                    __tracer = SnTrace.Category(TraceCategory);
                    __tracer.Enabled = true;
                }
                return __tracer;
            }
        }

        private readonly object _consoleSync = new object();
        private volatile int _reindexMetadataProgress;
        private volatile int _nodeCount;
        private volatile int _taskCount;

        public override void Execute(ExecutionContext context)
        {
            Tracer.Write("Phase-0: Create background tasks.");
            DataHandler.InstallTables();

            using (var op = Tracer.StartOperation("Phase-1: Reindex metadata."))
            {
                ReindexMetadata();
                op.Successful = true;
            }

            using (var op = Tracer.StartOperation("Phase-2: Create background tasks."))
            {
                DataHandler.StartBackgroundTasks();
                op.Successful = true;
            }
            SnTrace.Flush();
            // ensure finishing the last operation of the SnTrace
            Thread.Sleep(1500);
        }
        private void ReindexMetadata()
        {
            using (new SystemAccount())
            {
                Tracer.Write("Phase-1: Discover node ids.");
                var nodeIds = DataHandler.GetAllNodeIds(0);
                _nodeCount = nodeIds.Count;

                Tracer.Write($"Phase-1: Start reindexing {_nodeCount} nodes. Create background tasks");
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
            Tracer.Write($"V#{node.VersionId} {node.Version} N#{node.Id} {node.Path}");
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
                    taskCount > 0 ? taskCount : 10, //UNDONE: finalize tesk count and timeout
                    timeoutInMinutes > 0 ? timeoutInMinutes : 10,
                    out var remainingTasks);
                if (remainingTasks == 0)
                {
                    _featureIsRequested = false;
                    _featureIsRunning = false;
                    return true;
                }

                Tracer.Write($"Assigned tasks: {versionIds.Length}, unfinished: {remainingTasks}");

                foreach (var versionId in versionIds)
                {
                    if (ReindexBinaryProperties(versionId))
                        DataHandler.FinishTask(versionId);
                }
            } while (_featureIsRequested);

            _featureIsRunning = false;
            return false;
        }
        private static bool ReindexBinaryProperties(int versionId)
        {
            var node = Node.LoadNodeByVersionId(versionId);
            if (node == null)
                return true;

            //UNDONE: Skip if the version is modified after the reindex feature is started.

            using (new SystemAccount())
            {
                try
                {
                    Retrier.Retry(3, 2000, typeof(Exception), () =>
                    {
                        var indx = SearchManager.LoadIndexDocumentByVersionId(versionId);
                        DataBackingStore.SaveIndexDocument(node, indx);
                    });
                    Tracer.Write($"V#{node.VersionId} {node.Version} N#{node.Id} {node.Path}");
                    return true;
                }
                catch (Exception e)
                {
                    Tracer.WriteError("Error after 3 attempt: {0}", e);
                    return false;
                }
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
            Tracer.Write("Feature is inactivated.");
        }
    }
}
