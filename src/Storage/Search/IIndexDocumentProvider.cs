using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines operations for creating index document from a node.
    /// </summary>
    public interface IIndexDocumentProvider
    {
        /// <summary>
        /// Creates index document from the given node.
        /// </summary>
        /// <param name="node">The node that will be indexed.</param>
        /// <param name="skipBinaries">True if the BinaryProperty are skipped.</param>
        /// <param name="isNew">True if the node is new. Note that the passed node already saved to the database so its Id is never 0.</param>
        /// <param name="hasBinary">Output parameter that is true if the node has any BinaryProperty.</param>
        /// <returns></returns>
        IndexDocument GetIndexDocument(Node node, bool skipBinaries, bool isNew, out bool hasBinary);
        /// <summary>
        /// Returns with an updated base document with the BinaryProperties of the given node.
        /// </summary>
        /// <param name="node">The node that will be indexed.</param>
        /// <param name="baseDocument">The index document that will be updated.</param>
        /// <returns>The updated index document.</returns>
        IndexDocument CompleteIndexDocument(Node node, IndexDocument baseDocument);
    }
}
