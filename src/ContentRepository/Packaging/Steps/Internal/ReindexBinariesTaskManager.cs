using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Steps.Internal;

namespace SenseNet.ContentRepository.Packaging.Steps.Internal
{
    internal class ReindexBinariesTaskManager
    {
        private readonly ReindexBinariesDataHandler _dataHandler;
        private readonly CancellationToken _cancel = CancellationToken.None;

        // ReSharper disable once InconsistentNaming
        private SnTrace.SnTraceCategory __tracer;
        internal SnTrace.SnTraceCategory Tracer
        {
            get
            {
                if (__tracer == null)
                {
                    __tracer = SnTrace.Category(ReindexBinaries.TraceCategory);
                    __tracer.Enabled = true;
                }
                return __tracer;
            }
        }

        private static bool _featureIsRunning;
        private static bool _featureIsRequested;

        public ReindexBinariesTaskManager(ReindexBinariesDataHandler dataHandler)
        {
            _dataHandler = dataHandler;
        }

        public bool GetBackgroundTasksAndExecute(DateTime timeLimit, int taskCount = 0, int timeoutInMinutes = 0)
        {
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
                var assignedTasks = _dataHandler.AssignTasks(
                    taskCount > 0 ? taskCount : 10,
                    timeoutInMinutes > 0 ? timeoutInMinutes : 5, _cancel);

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
                        _dataHandler.FinishTask(versionId, _cancel);
                }
            } while (_featureIsRequested); // repeat if the maintenance called in the previous loop. 

            _featureIsRunning = false;
            return false;
        }
        private bool ReindexBinaryProperties(int versionId, DateTime timeLimit)
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
                        var indx = Providers.Instance.DataStore.LoadIndexDocumentsAsync(new[] { versionId },
                                CancellationToken.None).GetAwaiter().GetResult().FirstOrDefault();
                        var _ = Providers.Instance.DataStore
                            .SaveIndexDocumentAsync(node, indx, CancellationToken.None)
                            .GetAwaiter().GetResult();
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

        internal DateTime GetTimeLimit()
        {
            return _dataHandler.LoadTimeLimit(_cancel);
        }
        internal bool IsFeatureActive()
        {
            if (Debugger.IsAttached)
                return false;
            if (Process.GetCurrentProcess().ProcessName.Equals("SnAdminRuntime", StringComparison.InvariantCultureIgnoreCase))
                return false;

            return _dataHandler.CheckFeature(_cancel);
        }
        internal void InactivateFeature()
        {
            _dataHandler.DropTables(_cancel);
            Tracer.Write("Feature is inactivated.");
        }
    }
}
