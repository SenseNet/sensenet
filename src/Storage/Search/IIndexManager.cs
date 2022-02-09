using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search
{
    public interface IIndexManager
    {
        /* ==================================================================== Managing index */

        /// <summary>
        /// Gets the current <see cref="IIndexingEngine"/> implementation.
        /// </summary>
        IIndexingEngine IndexingEngine { get; }

        /// <summary>
        /// Gets a value that is true if the current indexing engine is running.
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Gets the ids of not indexed <see cref="NodeType"/>s.
        /// </summary>
        int[] GetNotIndexedNodeTypes();

        /// <summary>
        /// Initializes the indexing feature: starts the IndexingEngine, CommitManager and indexing activity organizer.
        /// If "consoleOut" is not null, writes progress and debug messages into it.
        /// </summary>
        /// <param name="consoleOut">A <see cref="TextWriter"/> instance or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken);


        /// <summary>
        /// Shuts down the indexing feature: stops CommitManager, indexing activity organizer and IndexingEngine.
        /// </summary>
        void ShutDown();

        /// <summary>
        /// Deletes the existing index. Called before making a brand new index.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ClearIndexAsync(CancellationToken cancellationToken);

        /* ========================================================================================== Activity */

        /// <summary>
        /// Registers an indexing activity in the database.
        /// </summary>
        /// <param name="activity">The activity to register.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task RegisterActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken);

        /// <summary>
        /// Executes an indexing activity taking dependencies into account and waits for its completion asynchronously.
        /// Dependent activities are executed in the order of registration.
        /// Dependent activity execution starts after the previously blocker activity is completed.
        /// </summary>
        /// <param name="activity">The activity to execute.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ExecuteActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the Id of the last registered indexing activity.
        /// </summary>
        int GetLastStoredIndexingActivityId();

        /// <summary>
        /// Gets the current <see cref="IndexingActivityStatus"/> instance
        /// containing the last executed indexing activity id and ids of missing indexing activities.
        /// This method is used in the distributed indexing scenario.
        /// The indexing activity status comes from the index.
        /// </summary>
        /// <returns>The current <see cref="IndexingActivityStatus"/> instance.</returns>
        IndexingActivityStatus GetCurrentIndexingActivityStatus();

        /// <summary>
        /// Deletes all restore points from the database.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteRestorePointsAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Gets the current <see cref="IndexingActivityStatus"/> instance
        /// containing the last executed indexing activity id and ids of missing indexing activities.
        /// This method is used in the centralized indexing scenario.
        /// The indexing activity status comes from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the current
        /// <see cref="IndexingActivityStatus"/> instance.</returns>
        Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Restores the indexing activity status.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <remarks>
        /// To ensure index and database integrity, this method marks indexing activities
        /// that were executed after the backup status was queried as executables. In the
        /// CentralizedIndexingActivityQueue's startup sequence these activities will be
        /// executed before new indexing activities that were added later.
        /// </remarks>
        /// <param name="status">An <see cref="IndexingActivityStatus"/> instance that contains the latest executed activity id and gaps.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(
            IndexingActivityStatus status, CancellationToken cancellationToken);

        /* ==================================================================== IndexDocument management */

        /// <summary>
        /// Returns finalized IndexDocument extracted from passed <paramref name="docData"/>.
        /// Finalization means adding place-in-tree information to the IndexDocument e.g. Name, Path, ParentId, IsSystem etc.
        /// </summary>
        IndexDocument CompleteIndexDocument(IndexDocumentData docData);

        /// <summary>
        /// Loads the index document of an explicit Content version, extends it with text extract and update in database and index.
        /// </summary>
        void AddTextExtract(int versionId, string textExtract);
    }
}
