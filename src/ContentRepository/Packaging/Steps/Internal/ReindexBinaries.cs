using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
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
        internal static SnTrace.SnTraceCategory Tracer
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

        private static CancellationToken cancel = CancellationToken.None; //UNDONE:DB: Cancel: Get token from somewhere

        public override void Execute(ExecutionContext context)
        {
            Tracer.Write("Phase-0: Initializing.");
            DataHandler.InstallTables(cancel);

            using (var op = Tracer.StartOperation("Phase-1: Reindex metadata."))
            {
                ReindexMetadata();
                op.Successful = true;
            }

            using (var op = Tracer.StartOperation("Phase-2: Create background tasks."))
            {
                DataHandler.StartBackgroundTasks(cancel);
                op.Successful = true;
            }

            // commit all buffered lines
            SnTrace.Flush();
        }
        private void ReindexMetadata()
        {
            using (new SystemAccount())
            {
                Tracer.Write("Phase-1: Discover node ids.");
                var nodeIds = DataHandler.GetAllNodeIds(cancel);
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
            var result = DataStore.SaveIndexDocumentAsync(node, true, false).Result;
            var indx = result.IndexDocumentData;
            if (result.HasBinary)
                CreateBinaryReindexTask(node,
                    indx.IsLastPublic ? 1 : indx.IsLastDraft ? 2 : 3);
            _reindexMetadataProgress++;
        }
        private void CreateBinaryReindexTask(Node node, int rank)
        {
            DataHandler.CreateTempTask(node.VersionId, rank, cancel);
            _taskCount++;
            Tracer.Write($"V#{node.VersionId} {node.Version} N#{node.Id} {node.Path}");
        }

        /* =============================================================== */

        private static bool _featureIsRunning;
        private static bool _featureIsRequested;

        /// <summary>
        /// Gets some background task from the database and executes them.
        /// Task execution is skipped if the version is modified after this time.
        /// Returns true if there are no more tasks.
        /// Triggered by the SnMaintenance via the ReindexBinariesTask.
        /// </summary>
        /// <param name="timeLimit">Execution is skipped if the version is modified after this time.</param>
        /// <param name="taskCount">Default 10.</param>
        /// <param name="timeoutInMinutes">Default 5.</param>
        /// <returns>True if there are no more tasks.</returns>
        internal static bool GetBackgroundTasksAndExecute(DateTime timeLimit, int taskCount = 0, int timeoutInMinutes = 0)
        {
            //UNDONE:DB: TEST: not tested (packaging)
            if (_featureIsRunning)
            {
                _featureIsRequested = true;
                Tracer.Write("Maintenance call skipped.");
                return false;
            }
            Tracer.Write("Maintenance call.");
            _featureIsRunning = true;

            do
            {
                _featureIsRequested = false;
                var assignedTasks = DataHandler.AssignTasks(
                    taskCount > 0 ? taskCount : 10,
                    timeoutInMinutes > 0 ? timeoutInMinutes : 5, cancel);

                var versionIds = assignedTasks.VersionIds;
                if (assignedTasks.RemainingTaskCount == 0)
                {
                    _featureIsRequested = false;
                    _featureIsRunning = false;
                    return true;
                }

                Tracer.Write($"Assigned tasks: {versionIds.Length}, unfinished: {assignedTasks.RemainingTaskCount}");

                foreach (var versionId in versionIds)
                {
                    if (ReindexBinaryProperties(versionId, timeLimit))
                        DataHandler.FinishTask(versionId, cancel);
                }
            } while (_featureIsRequested); // repeat if the maintenance called in the previous loop. 

            _featureIsRunning = false;
            return false;
        }
        private static bool ReindexBinaryProperties(int versionId, DateTime timeLimit)
        {
            using (new SystemAccount())
            {
                var node = Node.LoadNodeByVersionId(versionId);
                if (node == null)
                    return true;

                if (node.VersionModificationDate > timeLimit)
                {
                    Tracer.Write($"SKIP V#{node.VersionId} {node.Version} N#{node.Id} {node.Path}");
                    return true;
                }

                try
                {
                    Retrier.Retry(3, 2000, typeof(Exception), () =>
                    {
                        var indx = SearchManager.LoadIndexDocumentByVersionId(versionId);
                        DataStore.SaveIndexDocumentAsync(node, indx).Wait();
                    });
                    Tracer.Write($"Save V#{node.VersionId} {node.Version} N#{node.Id} {node.Path}");
                    return true;
                }
                catch (Exception e)
                {
                    Tracer.WriteError("Error after 3 attempts: {0}", e);
                    return false;
                }
            }
        }

        internal static DateTime GetTimeLimit()
        {
            return DataHandler.LoadTimeLimit(cancel);
        }
        internal static bool IsFeatureActive()
        {
            if (Debugger.IsAttached)
                return false;
            if (Process.GetCurrentProcess().ProcessName.Equals("SnAdminRuntime", StringComparison.InvariantCultureIgnoreCase))
                return false;

            return DataHandler.CheckFeature(cancel);
        }
        internal static void InactivateFeature()
        {
            DataHandler.DropTables(cancel);
            Tracer.Write("Feature is inactivated.");
        }
    }
}
