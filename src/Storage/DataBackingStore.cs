using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using System.IO;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using System.Globalization;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using SenseNet.Tools;
using BlobStorage = SenseNet.ContentRepository.Storage.Data.BlobStorage;

namespace SenseNet.ContentRepository.Storage
{
    public static class DataBackingStore
    {
        // ====================================================================== Save Nodedata

        private const int maxDeadlockIterations = 3;
        private const int sleepIfDeadlock = 1000;

        internal static void SaveNodeData(Node node, NodeSaveSettings settings, IIndexPopulator populator, string originalPath, string newPath)
        {
            var isNewNode = node.Id == 0;
            var isOwnerChanged = node.Data.IsPropertyChanged("OwnerId");
            if (!isNewNode && isOwnerChanged)
                node.Security.Assert(PermissionType.TakeOwnership);

            var data = node.Data;
            var attempt = 0;

            using (var op = SnTrace.Database.StartOperation("SaveNodeData"))
            {
                while (true)
                {
                    attempt++;

                    var deadlockException = SaveNodeDataTransactional(node, settings, populator, originalPath, newPath);
                    if (deadlockException == null)
                        break;

                    SnTrace.Database.Write("DEADLOCK detected. Attempt: {0}/{1}, NodeId:{2}, Version:{3}, Path:{4}",
                        attempt, maxDeadlockIterations, node.Id, node.Version, node.Path);

                    if (attempt >= maxDeadlockIterations)
                        throw new Exception(string.Format("Error saving node. Id: {0}, Path: {1}", node.Id, node.Path), deadlockException);

                    SnLog.WriteWarning("Deadlock detected in SaveNodeData", properties:
                        new Dictionary<string, object>
                        {
                            {"Id: ", node.Id},
                            {"Path: ", node.Path},
                            {"Version: ", node.Version},
                            {"Attempt: ", attempt}
                        });

                    System.Threading.Thread.Sleep(sleepIfDeadlock);
                }
                op.Successful = true;
            }

            try
            {
                if (isNewNode)
                {
                    SecurityHandler.CreateSecurityEntity(node.Id, node.ParentId, node.OwnerId);
                }
                else if (isOwnerChanged)
                {
                    SecurityHandler.ModifyEntityOwner(node.Id, node.OwnerId);
                }
            }
            catch (EntityNotFoundException e)
            {
                SnLog.WriteException(e, $"Error during creating or modifying security entity: {node.Id}. Original message: {e}",
                    EventId.Security);
            }
            catch (SecurityStructureException) // suppressed
            {
                // no need to log this: somebody else already created or modified this security entity
            }

            if (isNewNode)
                SnTrace.ContentOperation.Write("Node created. Id:{0}, Path:{1}", data.Id, data.Path);
            else
                SnTrace.ContentOperation.Write("Node updated. Id:{0}, Path:{1}", data.Id, data.Path);
        }
        private static Exception SaveNodeDataTransactional(Node node, NodeSaveSettings settings, IIndexPopulator populator, string originalPath, string newPath)
        {
            IndexDocumentData indexDocument = null;
            bool hasBinary = false;

            var data = node.Data;
            var isNewNode = data.Id == 0;

            var msg = "Saving Node#" + node.Id + ", " + node.ParentPath + "/" + node.Name;

            using (var op = SnTrace.Database.StartOperation(msg))
            {
                try
                {
                    // collect data for populator
                    var populatorData = populator.BeginPopulateNode(node, settings, originalPath, newPath);

                    if (settings.NodeHead != null)
                    {
                        settings.LastMajorVersionIdBefore = settings.NodeHead.LastMajorVersionId;
                        settings.LastMinorVersionIdBefore = settings.NodeHead.LastMinorVersionId;
                    }

                    // Finalize path
                    string path;

                    if (data.Id != Identifiers.PortalRootId)
                    {
                        var parent = NodeHead.Get(data.ParentId);
                        if (parent == null)
                            throw new ContentNotFoundException(data.ParentId.ToString());
                        path = RepositoryPath.Combine(parent.Path, data.Name);
                    }
                    else
                    {
                        path = Identifiers.RootPath;
                    }
                    Node.AssertPath(path);
                    data.Path = path;

                    // Store in the database
                    int lastMajorVersionId, lastMinorVersionId;

                    var head = DataStore.SaveNodeAsync(data, settings, CancellationToken.None).Result;
                    lastMajorVersionId = settings.LastMajorVersionIdAfter;
                    lastMinorVersionId = settings.LastMinorVersionIdAfter;
                    node.RefreshVersionInfo(head);

                    // here we re-create the node head to insert it into the cache and refresh the version info);
                    if (lastMajorVersionId > 0 || lastMinorVersionId > 0)
                    {
                        if (!settings.DeletableVersionIds.Contains(node.VersionId))
                        {
                            // Elevation: we need to create the index document with full
                            // control to avoid field access errors (indexing must be independent
                            // from the current users permissions).
                            using (new SystemAccount())
                            {
                                indexDocument = DataStore.SaveIndexDocument(node, true, isNewNode, out hasBinary);
                            }
                        }
                    }

                    // populate index only if it is enabled on this content (e.g. preview images will be skipped)
                    using (var op2 = SnTrace.Index.StartOperation("Indexing node"))
                    {
                        if (node.IsIndexingEnabled)
                        {
                            using (new SystemAccount())
                                populator.CommitPopulateNode(populatorData, indexDocument);
                        }

                        if (indexDocument != null && hasBinary)
                        {
                            using (new SystemAccount())
                            {
                                indexDocument = DataStore.SaveIndexDocument(node, indexDocument);
                                populator.FinalizeTextExtracting(populatorData, indexDocument);
                            }
                        }
                        op2.Successful = true;
                    }
                }
                catch (System.Data.Common.DbException dbe)
                {
                    if (IsDeadlockException(dbe))
                        return dbe;
                    throw SavingExceptionHelper(data, dbe);
                }
                catch (Exception e)
                {
                    var ee = SavingExceptionHelper(data, e);
                    if (ee == e)
                        throw;
                    else
                        throw ee;
                }
                op.Successful = true;
            }
            return null;
        }

        private static bool IsDeadlockException(System.Data.Common.DbException e)
        {
            // Avoid [SqlException (0x80131904): Transaction (Process ID ??) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
            // CAUTION: Using e.ErrorCode and testing for HRESULT 0x80131904 will not work! you should use e.Number not e.ErrorCode
            var sqlEx = e as System.Data.SqlClient.SqlException; //UNDONE:DB@@@ SqlException cannot be processed in a general algorithm
            if (sqlEx == null)
                return false;
            var sqlExNumber = sqlEx.Number;
            var sqlExErrorCode = sqlEx.ErrorCode;
            var isDeadLock = sqlExNumber == 1205;
            // assert
            var messageParts = new[]
                                   {
                                       "was deadlocked on lock",
                                       "resources with another process and has been chosen as the deadlock victim. rerun the transaction"
                                   };
            var currentMessage = e.Message.ToLower();
            var isMessageDeadlock = !messageParts.Where(msgPart => !currentMessage.Contains(msgPart)).Any();

            if (sqlEx != null && isMessageDeadlock != isDeadLock)
                throw new Exception(String.Concat("Incorrect deadlock analysis",
                    ". Number: ", sqlExNumber,
                    ". ErrorCode: ", sqlExErrorCode,
                    ". Errors.Count: ", sqlEx.Errors.Count,
                    ". Original message: ", e.Message), e);
            return isDeadLock;
        }
        private static Exception SavingExceptionHelper(NodeData data, Exception catchedEx)
        {
            var message = "The content cannot be saved.";
            if (catchedEx.Message.StartsWith("Cannot insert duplicate key"))
            {
                message += " A content with the name you specified already exists.";

                var appExc = new NodeAlreadyExistsException(message, catchedEx); // new ApplicationException(message, catchedEx);
                appExc.Data.Add("NodeId", data.Id);
                appExc.Data.Add("Path", data.Path);
                appExc.Data.Add("OriginalPath", data.OriginalPath);

                appExc.Data.Add("ErrorCode", "ExistingNode");
                return appExc;
            }
            return catchedEx;
        }
    }
}
