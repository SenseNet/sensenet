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
                Parallel.ForEach(new NodeList<Node>(DataHandler.GetAllNodeIds(0)), n =>
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
        /// Called by the SnMaintenance.
        /// </summary>
        public static void GetBackgroundTasksAndExecute()
        {
            if (_featureIsRunning)
            {
                _featureIsRequested = true;
                return;
            }
            _featureIsRunning = true;
            _featureIsRequested = false;
            do
            {
                foreach (var versionId in DataHandler.AssignTasks(100, 1))
                {
                    ReindexBinaryProperties(versionId);
                    DataHandler.FinishTask(versionId);
                }
            } while (_featureIsRequested);
            _featureIsRunning = false;
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
    }
}
