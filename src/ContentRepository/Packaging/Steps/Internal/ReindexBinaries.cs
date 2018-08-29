using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable CheckNamespace
namespace SenseNet.Packaging.Steps.Internal
{
    public partial class ReindexBinaries : Step
    {
        public override void Execute(ExecutionContext context)
        {
            DataHandler.InstallTables();
            ReindexMetadata();
            DataHandler.StartBackgroundTasks();
        }
        private void ReindexMetadata()
        {
            using (new SystemAccount())
            {
                Parallel.ForEach(new NodeList<Node>(DataHandler.GetAllNodeIds(0)),
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    n =>
                {
                    foreach (var node in n.LoadVersions())
                        ReindexNode(node);
                });
            }
        }
        private void ReindexNode(Node node)
        {
            var indx = DataBackingStore.SaveIndexDocument(node, true, false, out var hasBinary);
            if (hasBinary)
                CreateBinaryReindexTask(node.VersionId,
                    indx.IsLastPublic ? 1 : indx.IsLastDraft ? 2 : 3);
        }
        private void CreateBinaryReindexTask(int versionId, int rank)
        {
            DataHandler.CreateTempTask(versionId, rank);
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
                    taskCount > 0 ? taskCount : 42,
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
            throw new System.NotImplementedException();
        }

        public static void InactivateFeature()
        {
            throw new System.NotImplementedException();
        }
    }
}
