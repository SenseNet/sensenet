using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

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
        public int WaitingSeconds { get; } = 120; // 2 minutes
        private bool _fileCleanupIsRunning;

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
                SnTrace.Database.Write(SnMaintenance.TracePrefix + "Cleanup files: setting the IsDeleted flag...");
                await BlobStorage.CleanupFilesSetFlagAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Error in file cleanup set flag background process. " + ex, EventId.RepositoryRuntime);
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
                SnTrace.Database.Write(SnMaintenance.TracePrefix + "Cleanup files: deleting rows...");

                // keep deleting orphaned binary rows while there are any
                while (await BlobStorage.CleanupFilesAsync(cancellationToken).ConfigureAwait(false))
                {
                    deleteCount++;
                }

                if (deleteCount > 0)
                    SnLog.WriteInformation($"{deleteCount} orphaned rows were deleted from the binary table during cleanup.", 
                        EventId.RepositoryRuntime);
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Error in file cleanup background process. " + ex, EventId.RepositoryRuntime);
            }
        }
    }
}
