using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface IDataStore
    {
        /// <summary>
        /// Default size limit for preloading and caching long text values.
        /// </summary>
        public int TextAlternationSizeLimit { get; }

        /// <summary>
        /// Gets the current DataProvider instance.
        /// </summary>
        //public DataProvider DataProvider => Providers.Instance.DataProvider;
        public DataProvider DataProvider { get; }

        /// <summary>
        /// Gets the allowed length of the Path of a <see cref="Node"/>.
        /// </summary>
        public int PathMaxLength { get; }
        /// <summary>
        /// Gets the allowed minimum of a <see cref="DateTime"/> value.
        /// </summary>
        public DateTime DateTimeMinValue { get; }
        /// <summary>
        /// Gets the allowed maximum of a <see cref="DateTime"/> value.
        /// </summary>
        public DateTime DateTimeMaxValue { get; }
        /// <summary>
        /// Gets the allowed minimum of a <see cref="decimal"/> value.
        /// </summary>
        public decimal DecimalMinValue { get; }
        /// <summary>
        /// Gets the allowed maximum of a <see cref="decimal"/> value.
        /// </summary>
        public decimal DecimalMaxValue { get; }

        /// <summary>
        /// Restores the underlying dataprovider to the initial state after system start.
        /// </summary>
        public void Reset();

        /* =============================================================================================== Installation */

        /// <summary>
        /// Prepares the initial valid state of the underlying database using the provided storage-model structure.
        /// The database structure (tables, collections, indexes) should be prepared before calling this method 
        /// which happens during the installation process.
        /// </summary>
        /// <param name="data">A storage-model structure to install.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the Content tree representation for building the security model.
        /// Every node and leaf contains only the Id, ParentId and OwnerId of the node.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps an enumerable <see cref="EntityTreeNodeData"/>
        /// as the Content tree representation.</returns>
        public Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks if the database exists and is ready to accept new items.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a bool value that is
        /// true if the database already exists and contains the necessary schema.</returns>
        public Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Creates the database schema and fills it with the necessary initial data.
        /// </summary>
        /// <param name="initialData">Optional initial data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task InstallDatabaseAsync(InitialData initialData, CancellationToken cancellationToken);

        /* =============================================================================================== Nodes */

        /// <summary>
        /// Saves <see cref="Node"/> data to the underlying database. A new version may be created or
        /// an existing one updated based on the algorithm and values provided in the settings parameter.
        /// </summary>
        /// <remarks>This method is responsible for saving the data to the storage and managing the
        /// cache after the operation. It will also update the last version number and timestamp
        /// information stored in the settings and node data parameter objects.</remarks>
        /// <param name="nodeData">Data to save to the database.</param>
        /// <param name="settings">Defines the saving algorithm of the provided data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new node head
        /// containing the latest version and time identifiers.</returns>
        public Task<NodeHead> SaveNodeAsync(NodeData nodeData, NodeSaveSettings settings,
            CancellationToken cancellationToken);

        /// <summary>
        /// A loads a node from the database.
        /// </summary>
        /// <param name="head">A node head representing the node.</param>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node token
        /// containing the information for constructing the appropriate node object.</returns>
        public Task<NodeToken> LoadNodeAsync(NodeHead head, int versionId, CancellationToken cancellationToken);

        /// <summary>
        /// A loads an array of nodes from the database.
        /// </summary>
        /// <param name="headArray">A node head array representing the nodes to load.</param>
        /// <param name="versionIdArray">Version identifier array containing version ids of individual nodes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node token array
        /// containing the information for constructing the appropriate node objects.</returns>
        public Task<NodeToken[]> LoadNodesAsync(NodeHead[] headArray, int[] versionIdArray,
            CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a node from the database.
        /// </summary>
        /// <param name="nodeHead">A node data representing the node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteNodeAsync(NodeHead nodeHead, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a node from the database.
        /// </summary>
        /// <param name="nodeData">A node data representing the node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteNodeAsync(NodeData nodeData, CancellationToken cancellationToken);
        /// <summary>
        /// Moves a node to the provided target container.
        /// </summary>
        /// <param name="sourceNodeHead">A node data representing the node to move.</param>
        /// <param name="targetNodeId">Id of the container where the node will be moved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task MoveNodeAsync(NodeHead sourceNodeHead, int targetNodeId, CancellationToken cancellationToken);
        /// <summary>
        /// Moves a node to the provided target container.
        /// </summary>
        /// <param name="sourceNodeData">A node data representing the node to move.</param>
        /// <param name="targetNodeId">Id of the container where the node will be moved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task MoveNodeAsync(NodeData sourceNodeData, int targetNodeId, CancellationToken cancellationToken);

        /// <summary>
        /// Loads the provided text property values from the database.
        /// </summary>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="propertiesToLoad">A <see cref="PropertyType"/> id set to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a dictionary
        /// containing the loaded text property values.</returns>
        public Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] propertiesToLoad,
            CancellationToken cancellationToken);
        /// <summary>
        /// Loads the provided binary property value from the database.
        /// </summary>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="propertyTypeId">A <see cref="PropertyType"/> id to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a binary data
        /// containing the information to load the stream.</returns>
        public Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a node exists with the provided path.
        /// </summary>
        /// <param name="path">Path of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the database contains a node with the provided path.</returns>
        public Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken);

        public NodeData CreateNewNodeData(Node parent, NodeType nodeType, ContentListType listType, int listId);

        /* ----------------------------------------------------------------------------------------------- */

        public Stream GetBinaryStream(int nodeId, int versionId, int propertyTypeId);

        /* =============================================================================================== NodeHead */

        /// <summary>
        /// Loads a node head from the database.
        /// </summary>
        /// <param name="path">Path of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node head
        /// for the provided path or null if it does not exist.</returns>
        public Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken);
        /// <summary>
        /// Loads a node head from the database.
        /// </summary>
        /// <param name="nodeId">Id of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node head
        /// for the provided id or null if it does not exist.</returns>
        public Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken);
        /// <summary>
        /// Loads a node head from the database.
        /// </summary>
        /// <param name="versionId">Id of a specific node version.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node head
        /// for the provided version id or null if it does not exist.</returns>
        public Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken);
        /// <summary>
        /// Loads node heads from the database.
        /// </summary>
        /// <remarks>This method may return fewer node heads than requested in case not all of them exist.</remarks>
        /// <param name="nodeIds">Ids of the node heads to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of Node heads for the provided ids.</returns>
        public Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds, CancellationToken cancellationToken);

        /// <summary>
        /// Returns version numbers representing all versions of the requested <see cref="Node"/>.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of version numbers.</returns>
        public Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken);
        /// <summary>
        /// Returns version numbers representing all versions of the requested <see cref="Node"/>.
        /// </summary>
        /// <param name="path">Node path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of version numbers.</returns>
        public Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Load node heads in multiple subtrees.
        /// Used by internal APIs. Dot not use this method in your code.
        /// </summary>
        /// <param name="paths">Node path list.</param>
        /// <param name="resolveAll">Resolve all paths or only the first one that is found.</param>
        /// <param name="resolveChildren">Resolve child content or not.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of collected node heads.</returns>
        public Task<IEnumerable<NodeHead>> LoadNodeHeadsFromPredefinedSubTreesAsync(IEnumerable<string> paths, bool resolveAll,
            bool resolveChildren, CancellationToken cancellationToken);

        /* =============================================================================================== NodeQuery */

        /// <summary>
        /// Gets the count of nodes with the provided node types.
        /// </summary>
        /// <remarks>This methods expects a flattened list of node types. It does not return nodes of derived types.</remarks>
        /// <param name="nodeTypeIds">Array of node type ids. </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes of the requested types.</returns>
        public Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the ids of nodes that are children of the provided parent. Only direct children are collected, not the whole subtree.
        /// </summary>
        /// <param name="parentId">Parent node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps child node identifiers.</returns>
        public Task<IEnumerable<int>> GetChildrenIdentifiersAsync(int parentId, CancellationToken cancellationToken);
        /// <summary>
        /// Queries <see cref="Node"/>s by their path.
        /// </summary>
        /// <param name="pathStart">Case insensitive repository path of the required subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByPathAsync(string pathStart, bool orderByPath, CancellationToken cancellationToken);
        /// <summary>
        /// Queries <see cref="Node"/>s by their type.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByTypeAsync(int[] nodeTypeIds, CancellationToken cancellationToken);

        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository path of the relevant subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string pathStart,
            bool orderByPath, CancellationToken cancellationToken);

        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository paths of relevant subtrees or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string[] pathStart,
            bool orderByPath, CancellationToken cancellationToken);

        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository path of the relevant subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="name">Name of the relevant <see cref="Node"/>s or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string pathStart,
            bool orderByPath, string name, CancellationToken cancellationToken);

        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository paths of relevant subtrees or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="name">Name of the relevant <see cref="Node"/>s or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart,
            bool orderByPath, string name, CancellationToken cancellationToken);

        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository path of the relevant subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="properties">List of properties that need to be included in the query expression.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart,
            bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken);

        /// <summary>
        /// Queries <see cref="Node"/>s by a reference property.
        /// </summary>
        /// <remarks>For example: a list of books from a certain author. In this case the node type is Book,
        /// the reference property is Author and the referred node id is the author id.</remarks>
        /// <param name="referenceName">Name of the reference property to search for.</param>
        /// <param name="referredNodeId">Id of a referred node that the property value should contain.</param>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId,
            int[] nodeTypeIds, CancellationToken cancellationToken);

        /* =============================================================================================== Tree */

        /// <summary>
        /// Gets all the types that can be found in a subtree and are relevant in case of move or copy operations.
        /// </summary>
        /// <remarks>Not all types will be returned, only the ones that should be allowed on the target container.</remarks>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of node types in a subtree.</returns>
        public Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a list of content list types in a subtree.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps
        /// a list of content list types that are found in a subtree.</returns>
        public Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path,
            CancellationToken cancellationToken);

        /* =============================================================================================== TreeLock */

        /// <summary>
        /// Locks a subtree exclusively. The return value is 0 if the path is locked in the parent axis or in the subtree.
        /// </summary>
        /// <param name="path">Node path to lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the newly created
        /// tree lock Id for the requested path or 0.</returns>
        public Task<int> AcquireTreeLockAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether the provided path is locked.
        /// </summary>
        /// <param name="path">Node path to lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the path is already locked.</returns>
        public Task<bool> IsTreeLockedAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Releases the locks represented by the provided lock id array.
        /// </summary>
        /// <param name="lockIds">Array of lock identifiers.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all tree locks in the system.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a lock id, path dictionary.</returns>
        public Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken);

        /* =============================================================================================== IndexDocument */

        /// <summary>
        /// Generates the index document of a node and saves it to the database.
        /// </summary>
        /// <param name="node">The Node to index.</param>
        /// <param name="skipBinaries">True if binary properties should be skipped.</param>
        /// <param name="isNew">True if the node is new.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the result including
        /// index document data.</returns>
        public Task<SavingIndexDocumentDataResult> SaveIndexDocumentAsync(Node node, bool skipBinaries, bool isNew,
            CancellationToken cancellationToken);

        /// <summary>
        /// Completes the index document of a node and saves it to the database.
        /// </summary>
        /// <param name="node">The Node to index.</param>
        /// <param name="indexDocumentData">Index document data assembled by previous steps in the save operation.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the resulting index document data.</returns>
        public Task<IndexDocumentData> SaveIndexDocumentAsync(Node node, IndexDocumentData indexDocumentData,
            CancellationToken cancellationToken);

        public IndexDocumentData CreateIndexDocumentData(Node node, IndexDocument indexDocument,
            string serializedIndexDocument);

        /// <summary>
        /// Serializes the index document of a node and saves it to the database.
        /// </summary>
        /// <param name="versionId">The id of the target node version.</param>
        /// <param name="indexDoc">Index document assembled by previous steps in the save operation.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new version timestamp.</returns>
        public Task<long> SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc,
            CancellationToken cancellationToken);

        /* ----------------------------------------------------------------------------------------------- Load IndexDocument */

        /// <summary>
        /// Gets the index document for the provided version id.
        /// </summary>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the index document or null.</returns>
        public Task<IndexDocumentData> LoadIndexDocumentByVersionIdAsync(int versionId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets index documents for the provided version ids.
        /// </summary>
        /// <param name="versionIds">Version identifiers.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the list of loaded index documents.</returns>
        public Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets index document data for the provided subtree.
        /// </summary>
        /// <param name="path">Node path.</param>
        /// <param name="excludedNodeTypes">Array of node types that should be skipped during collecting index document data.</param>
        /// <returns>The list of loaded index documents.</returns>
        public IEnumerable<IndexDocumentData> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes);

        /// <summary>
        /// Gets ids of nodes that do not have an index document saved in the database.
        /// This method is used by the install process, do not use it from your code.
        /// </summary>
        /// <param name="fromId">Starting node id.</param>
        /// <param name="toId">Max node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the list of node ids.</returns>
        public Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId,
            CancellationToken cancellationToken);

        /* =============================================================================================== IndexingActivity */

        /// <summary>
        /// Deletes all restore points from the database.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteRestorePointsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the current indexing activity status. Contains the latest executed activity id and gaps.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the current indexing activity status.</returns>
        public Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Restores the indexing activity status.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="status">An <see cref="IndexingActivityStatus"/> instance that contains the latest executed activity id and gaps.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(
            IndexingActivityStatus status,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the latest IndexingActivityId or 0.
        /// This method is used in the distributed indexing scenario.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets indexing activities between the two provided ids.
        /// </summary>
        /// <remarks>All three boundary parameters are necessary for the algorithm to work because we have to make sure
        /// that no activity is loaded that have a bigger id than the one sealed at the beginning of the process.</remarks>
        /// <param name="fromId">Start activity id.</param>
        /// <param name="toId">Last accepted id.</param>
        /// <param name="count">Maximum number of loaded activities (page size).</param>
        /// <param name="executingUnprocessedActivities">True if the method is called during executing
        /// unprocessed indexing activities at system startup.</param>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the array of indexing activities.</returns>
        public Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count,
            bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets indexing activities defined by the provided id array.
        /// </summary>
        /// <param name="gaps">Array of activity ids to load.</param>
        /// <param name="executingUnprocessedActivities">True if the method is called during executing
        /// unprocessed indexing activities at system startup.</param>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the array of indexing activities.</returns>
        public Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken);

        /// <summary>
        /// Gets executable and finished indexing activities.
        /// </summary>
        /// <remarks>This method loads a limited number of executable activities and also the ones that
        /// were started long ago (beyond a timeout limit). It also checks whether any of the blocking
        /// activities (ones that we are waiting for) have been completed by other app domains.</remarks>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="maxCount">Maximum number of loaded activities.</param>
        /// <param name="runningTimeoutInSeconds">Timeout for running activities.</param>
        /// <param name="waitingActivityIds">An array of activities that this appdomain is waiting for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the executable activity result.</returns>
        public Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(
            IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds,
            int[] waitingActivityIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Registers an indexing activity in the database.
        /// </summary>
        /// <param name="activity">Indexing activity.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task RegisterIndexingActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken);

        /// <summary>
        /// Set the state of an indexing activity.
        /// </summary>
        /// <param name="indexingActivityId">Indexing activity id.</param>
        /// <param name="runningState">A state to set in the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId,
            IndexingActivityRunningState runningState,
            CancellationToken cancellationToken);

        /// <summary>
        /// Refresh the lock time of multiple indexing activities.
        /// </summary>
        /// <param name="waitingIds">Activity ids that we are still working on or waiting for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>internal static 
        public Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes finished activities. Called by a cleanup background process.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes all activities from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken);

        /* =============================================================================================== Schema */

        /// <summary>
        /// Gets the whole schema definition from the database, including property types, node types and content list types.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the schema definition.</returns>
        public Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a schema update operation and locks the schema exclusively.
        /// </summary>
        /// <param name="schemaTimestamp">The current known timestamp of the schema.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the generated schema lock token.</returns>
        public Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a schema writer supported by the current data provider.
        /// </summary>
        /// <returns>A schema writer object.</returns>
        public SchemaWriter CreateSchemaWriter();

        /// <summary>
        /// Completes a schema write operation and releases the lock.
        /// </summary>
        /// <param name="schemaLock">The lock token generated by the start operation before.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new schema timestamp.</returns>
        public Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken);

        /* =============================================================================================== Logging */

        /// <summary>
        /// Writes an audit event to the database.
        /// </summary>
        /// <param name="auditEvent">Audit event info.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the last several audit events from the database.
        /// Intended for internal use.
        /// </summary>
        /// <param name="count">Number of events to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of audit events.</returns>
        public Task<IEnumerable<AuditLogEntry>> LoadLastAuditEventsAsync(int count,
            CancellationToken cancellationToken);

        /* =============================================================================================== Tools */

        /// <summary>
        /// Returns the provided DateTime value with reduced precision.
        /// </summary>
        /// <param name="d">The DateTime value to round.</param>
        /// <returns>The rounded DateTime value.</returns>
        public DateTime RoundDateTime(DateTime d);

        /// <summary>
        /// Checks whether the text is short enough to be cached.
        /// By default it uses the <see cref="TextAlternationSizeLimit"/> value as a limit.
        /// </summary>
        /// <param name="value">Text value to test for.</param>
        /// <returns>True if the text can be added to the cache.</returns>
        public bool IsCacheableText(string value);

        /// <summary>
        /// Gets the name of a node with the provided name base and the biggest incremental name index.
        /// </summary>
        /// <remarks>For example if there are multiple files in the folder with names like MyDoc(1).docx, MyDoc(2).docx,
        /// this method will return the one with the biggest number.</remarks>
        /// <param name="parentId">Id of the container.</param>
        /// <param name="namebase">Name prefix.</param>
        /// <param name="extension">File extension.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the found file name.</returns>
        public Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the size of the requested node or the whole subtree.
        /// The size of a Node is the summary of all stream lengths in all committed BinaryProperties.
        /// The size does not contain staged streams (when upload is in progress) and orphaned (deleted) streams.
        /// </summary>
        /// <param name="path">Path of the requested node.</param>
        /// <param name="includeChildren">True if the algorithm should count in all nodes in the whole subtree.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the size of the requested node or subtree.</returns>
        public Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of nodes in the whole Content Repository.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes.</returns>
        public Task<int> GetNodeCountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of nodes in a subtree. The number will include the subtree root node.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes.</returns>
        public Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of node versions in the whole Content Repository.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of versions.</returns>
        public Task<int> GetVersionCountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of node versions in a subtree. The number will include the versions of the subtree root node.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of versions.</returns>
        public Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken);

        /* =============================================================================================== */

        public string CreateNodeDataVersionIdCacheKey(int versionId);

        public void CacheNodeData(NodeData nodeData, string cacheKey = null);

        /// <summary>
        /// Removes the node data from the cache by its version id.
        /// </summary>
        /// <param name="versionId">Version id.</param>
        public void RemoveNodeDataFromCacheByVersionId(int versionId);
    }
}
