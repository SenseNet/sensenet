using System;
using System.Collections.Generic;
using System.IO;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines constants for level definition of the index rebuilding.
    /// </summary>
    public enum IndexRebuildLevel
    {
        /// <summary>
        /// Rebuild the index with using the prepared index documents stored in the database.
        /// </summary>
        IndexOnly,
        /// <summary>
        /// Prepare the index document in the database by the modified content data and then
        /// rebuild the index with using the new index documents.
        /// </summary>
        DatabaseAndIndex
    }

    /// <summary>
    /// Defines operations for building indexes of the content.
    /// </summary>
    public interface IIndexPopulator
    {
        /// <summary>
        /// Build a brand new index.
        /// </summary>
        /// <param name="consoleWriter">TextWriter instance for writing progress.</param>
        void ClearAndPopulateAll(TextWriter consoleWriter = null);

        /// <summary>
        /// Refreshes the index of the given subtree directly. Designed for offline usage e.g. any step of SnAdmin package.
        /// Does not notify any other webservers and does not register any activity.
        /// Note: 
        /// </summary>
        void RebuildIndexDirectly(string path);
        /// <summary>
        /// Adds a brand new subtree to the index.
        /// </summary>
        /// <param name="path">The Path of the root node of the subtree.</param>
        /// <param name="nodeId">The Id of the root node of the subtree.</param>
        void AddTree(string path, int nodeId);
        /// <summary>
        /// Creates a snapshot object before saving the node.
        /// This snapshot helps to perform the correct indexing operation.
        /// </summary>
        /// <param name="node">The node before save.</param>
        /// <param name="settings">The current saving algorithm.</param>
        /// <param name="originalPath">The path of the node before save. Required.</param>
        /// <param name="newPath">The path of the node after save. Required.</param>
        /// <returns></returns>
        object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath);
        /// <summary>
        /// Writes index document to the index by the given snapshot.
        /// </summary>
        /// <param name="data">The snapshot that recorded by the BeginPopulateNode method.</param>
        /// <param name="indexDocument">The index document that will be written into the index.</param>
        void CommitPopulateNode(object data, IndexDocumentData indexDocument);
        /// <summary>
        /// Writes index document to the index after text extracting by the given snapshot.
        /// </summary>
        /// <param name="data">The snapshot that recorded by the BeginPopulateNode method.</param>
        /// <param name="indexDocument">The index document that will be written into the index.</param>
        void FinalizeTextExtracting(object data, IndexDocumentData indexDocument);
        /// <summary>
        /// Deletes a subtree from the index by path.
        /// </summary>
        /// <param name="path">Path of the deleted content.</param>
        /// <param name="nodeId">Id os the deleted content.</param>
        void DeleteTree(string path, int nodeId);
        /// <summary>
        /// Deletes more subtrees by id.
        /// The idSet cannot be null.
        /// </summary>
        void DeleteForest(IEnumerable<int> idSet);
        /// <summary>
        /// Deletes more subtrees by path.
        /// The pathSet cannot be null.
        /// </summary>
        void DeleteForest(IEnumerable<string> pathSet);

        /// <summary>
        /// Rebuilds the index of a node or a subtree.
        /// </summary>
        /// <param name="node">The root node of the subtree.</param>
        /// <param name="recursive">True if the intention is to reindex the whole subtree.</param>
        /// <param name="rebuildLevel">IndexRebuildLevel option.</param>
        void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly);

        /// <summary>
        /// Defines an event that occurs when a node has just been indexed.
        /// </summary>
        event EventHandler<NodeIndexedEventArgs> NodeIndexed;
    }
}
