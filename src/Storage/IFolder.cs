using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines an interface for Content handler classes that can contain child Contents.
    /// </summary>
    public interface IFolder
    {
        /// <summary>
        /// Gets the collection of child <see cref="Node"/>s.
        /// </summary>
        IEnumerable<Node> Children { get; }
        /// <summary>
        /// Gets the count of the Children collection.
        /// </summary>
        int ChildCount { get; }

        /// <summary>
        /// Returns a query result of this Content's children.
        /// </summary>
        /// <param name="settings">A <see cref="QuerySettings"/> that extends the base query.</param>
        /// <returns>The <see cref="QueryResult"/> instance containing child items.</returns>
        QueryResult GetChildren(QuerySettings settings);
        /// <summary>
        /// Returns a query result of this Content's children.
        /// </summary>
        /// <param name="text">An additional filter clause.</param>
        /// <param name="settings">A <see cref="QuerySettings"/> that extends the base query.</param>
        /// <returns>The <see cref="QueryResult"/> instance containing child items.</returns>
        QueryResult GetChildren(string text, QuerySettings settings);
    }
}