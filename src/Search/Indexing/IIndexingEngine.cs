using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    public enum IndexBackupResult { Finished, AlreadyExecuting }

    /// <summary>
    /// Describes a class that executes the indexing operations. Only one instance is used.
    /// </summary>
    public interface IIndexingEngine
    {
        /// <summary>
        /// Gets a value that indicates whether the IIndexingEngine is running.
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Gets a value that is true if the index is shared or false if the index is replicated.
        /// </summary>
        bool IndexIsCentralized { get; }

        /// <summary>
        /// Initializes the IIndexingEngine instance.
        /// ConsoleOut can be used for writing interactive messages if the system is running under an administrative tool.
        /// </summary>
        /// <param name="consoleOut">Console to write messages to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken);

        /// <summary>
        /// Stops the indexing and releases all inner and outer resources.  This is not a destructor.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ShutDownAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Takes a snapshot of the index and copies it to the configured or well known place.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task<IndexBackupResult> BackupAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Takes a snapshot of the index and copies it to the given target.
        /// Target is typically a directory in the filesystem.
        /// </summary>
        /// <param name="target">Path of the target directory or any other target definition.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task<IndexBackupResult> BackupAsync(string target, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the current index and creates a brand new empty one.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ClearIndexAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns an IndexingActivityStatus instance that was associated to the index state.
        /// Called once in the system startup sequence and periodically in the index health check.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the indexing activity status.</returns>
        Task<IndexingActivityStatus> ReadActivityStatusFromIndexAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Associate the given indexing state to the index. This method is called after index writing.
        /// In heavy load the status writing is not as dense than the index writing.
        /// </summary>
        /// <param name="state">The indexing activity state to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task WriteActivityStatusToIndexAsync(IndexingActivityStatus state, CancellationToken cancellationToken);

        /// <summary>
        /// Executes an atomic indexing operation. Deletes all index documents by "deletions" parameter,
        ///   updates all documents by "updates" parameter, and adds all documents from the "addition" parameter.
        /// Parameter data are not overlapped (e.g. addition is not deleted in one operation)
        ///   so the order of execution is not important (the inner implementation can be parallelized).
        /// The method need to be synchronous operation so it returns after full execution.
        /// Throws an exception if the operation is unsuccessful.
        /// Note that if the indexing operates uncertainly (means index document existence is not sure),
        ///   it is strongly recommended to delete the document before addition.
        ///   In this case the document can be deleted by the VersionId term.
        /// </summary>
        /// <param name="deletions">Contains terms that define the documents to delete. Can be null or empty.</param>
        /// <param name="updates">Contains term-document pairs that define the refreshed items. Can be null or empty.</param>
        /// <param name="additions">Contains documents to add to index.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates,
            IEnumerable<IndexDocument> additions, CancellationToken cancellationToken);
    }
}
