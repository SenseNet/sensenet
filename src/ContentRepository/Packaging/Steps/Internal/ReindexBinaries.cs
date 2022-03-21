using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

// ReSharper disable CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public class ReindexBinaries : Step
    {
        internal static readonly string TraceCategory = "REINDEX";

        // ReSharper disable once InconsistentNaming
        private SnTrace.SnTraceCategory __tracer;
        internal SnTrace.SnTraceCategory Tracer
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

        private readonly CancellationToken _cancel = CancellationToken.None;

        private ReindexBinariesDataHandler _dataHandler;

        public override void Execute(ExecutionContext context)
        {
            Tracer.Write("Phase-0: Initializing.");

            _dataHandler = new ReindexBinariesDataHandler(DataOptions.GetLegacyConfiguration(), context.ConnectionStrings);
            _dataHandler.InstallTables(_cancel);

            using (var op = Tracer.StartOperation("Phase-1: Reindex metadata."))
            {
                ReindexMetadata();
                op.Successful = true;
            }

            using (var op = Tracer.StartOperation("Phase-2: Create background tasks."))
            {
                _dataHandler.StartBackgroundTasks(_cancel);
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
                var nodeIds = _dataHandler.GetAllNodeIds(_cancel);
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
            var result = Providers.Instance.DataStore
                .SaveIndexDocumentAsync(node, true, false, CancellationToken.None).GetAwaiter().GetResult();
            var index = result.IndexDocumentData;
            if (result.HasBinary)
                CreateBinaryReindexTask(node,
                    index.IsLastPublic ? 1 : index.IsLastDraft ? 2 : 3);
            Interlocked.Increment(ref _reindexMetadataProgress);
        }
        private void CreateBinaryReindexTask(Node node, int rank)
        {
            _dataHandler.CreateTempTask(node.VersionId, rank, _cancel);
            Interlocked.Increment(ref _taskCount);
            Tracer.Write($"V#{node.VersionId} {node.Version} N#{node.Id} {node.Path}");
        }
    }
}
