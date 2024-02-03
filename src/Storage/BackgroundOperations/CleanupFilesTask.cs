using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    /// <summary>
    /// When a content with a binary (e.g. a document) is deleted, the file row itself is not
    /// deleted from the database, it is only detached from the binary property table for performance
    /// reasons. This maintenance task cleans up these orphaned rows.
    /// </summary>
    public class CleanupFilesTask : IMaintenanceTask
    {
        private readonly ILogger<CleanupFilesTask> _logger;
        public int WaitingSeconds { get; } = 120; // 2 minutes
        private bool _fileCleanupIsRunning;
        private IBlobStorage BlobStorage { get; }

        public CleanupFilesTask(IBlobStorage blobStorage, ILogger<CleanupFilesTask> logger)
        {
            _logger = logger;
            BlobStorage = blobStorage;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // skip cleanup, if it is already running
            if (_fileCleanupIsRunning)
                return;
            
            _fileCleanupIsRunning = true;

            try
            {
                // preparation: flag rows to delete
                await CleanupFilesSetFlagAsync(cancellationToken).ConfigureAwait(false);
                // delete rows one by one to lessen the load on the SQL server
                await CleanupFilesDeleteRowsAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _fileCleanupIsRunning = false;
            }
        }

        /// <summary>
        /// This method only flags orphaned rows for the CleanupFilesDeleteRowsAsync 
        /// method that will actually delete the rows from the database.
        /// </summary>
        private async Task CleanupFilesSetFlagAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogTrace(SnMaintenance.TracePrefix + "Cleanup files: setting the IsDeleted flag...");
                await BlobStorage.CleanupFilesSetFlagAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in file cleanup set flag background process.");
            }
        }
        /// <summary>
        /// This method deletes orphaned rows from the database physically.
        /// </summary>
        private async Task CleanupFilesDeleteRowsAsync(CancellationToken cancellationToken)
        {
            var deleteCount = 0;

            try
            {
                _logger.LogTrace(SnMaintenance.TracePrefix + "Cleanup files: deleting rows...");

                // keep deleting orphaned binary rows while there are any
                while (await BlobStorage.CleanupFilesAsync(cancellationToken).ConfigureAwait(false))
                {
                    deleteCount++;

                    // check if the task was cancelled and return silently if yes
                    if (cancellationToken.IsCancellationRequested)
                        return;
                }

                if (deleteCount > 0)
                    _logger.LogInformation(
                        "{DeleteRowCount} orphaned rows were deleted from the binary table during cleanup.",
                        deleteCount);
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("File cleanup was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in file cleanup background process.");
            }
        }
    }
}
