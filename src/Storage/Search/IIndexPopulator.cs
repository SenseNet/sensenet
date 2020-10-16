using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines constants for the level of index rebuilding.
    /// </summary>
    public enum IndexRebuildLevel
    {
        /// <summary>
        /// Rebuild the index using the prepared index documents stored in the database.
        /// </summary>
        IndexOnly,
        /// <summary>
        /// Re-create the index document, store it in the database and then
        /// rebuild the index using the new index documents.
        /// </summary>
        DatabaseAndIndex
    }

    /// <summary>
    /// Defines operations for indexing content items.
    /// </summary>
    public interface IIndexPopulator
    {
        /// <summary>
        /// Builds a brand new index.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="consoleWriter">TextWriter instance for writing progress.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ClearAndPopulateAllAsync(CancellationToken cancellationToken, TextWriter consoleWriter = null);

        /// <summary>
        /// Refreshes the index of the given subtree directly. Designed for offline usage e.g. a step in an SnAdmin package.
        /// It does not notify other web servers and does not register activities.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="level">Index rebuild level. Default is <see cref="IndexRebuildLevel.IndexOnly"/>.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task RebuildIndexDirectlyAsync(string path, CancellationToken cancellationToken, IndexRebuildLevel level = IndexRebuildLevel.IndexOnly);
        /// <summary>
        /// Adds a brand new subtree to the index.
        /// </summary>
        /// <param name="path">The Path of the root node of the subtree.</param>
        /// <param name="nodeId">The Id of the root node of the subtree.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task AddTreeAsync(string path, int nodeId, CancellationToken cancellationToken);
        /// <summary>
        /// Creates a snapshot object before saving the node.
        /// This snapshot helps performing the correct indexing operation.
        /// </summary>
        /// <param name="node">The node before save.</param>
        /// <param name="settings">The current saving algorithm.</param>
        /// <param name="originalPath">The path of the node before save. Required.</param>
        /// <param name="newPath">The path of the node after save. Required.</param>
        /// <returns>A custom snapshot object that will help the <see cref="CommitPopulateNodeAsync"/> method
        /// finalizing the operation.</returns>
        object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath);
        /// <summary>
        /// Writes the index document to the index using the state information in the snapshot object.
        /// </summary>
        /// <param name="data">The snapshot recorded by the <see cref="BeginPopulateNode"/> method.</param>
        /// <param name="indexDocument">The index document that will be written into the index.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CommitPopulateNodeAsync(object data, IndexDocumentData indexDocument, CancellationToken cancellationToken);
        /// <summary>
        /// Writes the index document to the index after text extraction using the state information in the snapshot object.
        /// </summary>
        /// <param name="data">The snapshot recorded by the <see cref="BeginPopulateNode"/> method.</param>
        /// <param name="indexDocument">The index document that will be written into the index.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task FinalizeTextExtractingAsync(object data, IndexDocumentData indexDocument, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a subtree from the index by path.
        /// </summary>
        /// <param name="path">Path of the deleted content.</param>
        /// <param name="nodeId">Id of the deleted content.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteTreeAsync(string path, int nodeId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes multiple subtrees by id.
        /// The idSet cannot be null.
        /// </summary>
        /// <param name="idSet">An array of subtree root ids to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteForestAsync(IEnumerable<int> idSet, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes multiple subtrees by path.
        /// The pathSet cannot be null.
        /// </summary>
        /// <param name="pathSet">An array of subtree root paths to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteForestAsync(IEnumerable<string> pathSet, CancellationToken cancellationToken);

        /// <summary>
        /// Rebuilds the index of a node or a subtree.
        /// </summary>
        /// <param name="node">The root node of the subtree.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="recursive">True if the intention is to reindex the whole subtree.</param>
        /// <param name="rebuildLevel">Index rebuild level. Default is <see cref="IndexRebuildLevel.IndexOnly"/>.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task RebuildIndexAsync(Node node, CancellationToken cancellationToken, bool recursive = false, 
            IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly);

        /// <summary>
        /// Defines an event that occurs when an index document is refreshed.
        /// </summary>
        event EventHandler<NodeIndexedEventArgs> IndexDocumentRefreshed;

        /// <summary>
        /// Defines an event that occurs when a node has just been indexed.
        /// </summary>
        event EventHandler<NodeIndexedEventArgs> NodeIndexed;

        /// <summary>
        /// Defines an event that occurs when a node indexing causes an error.
        /// </summary>
        event EventHandler<NodeIndexingErrorEventArgs> IndexingError;
    }
}
