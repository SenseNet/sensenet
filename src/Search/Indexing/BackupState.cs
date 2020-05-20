using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Represents a state of the backup operation.
    /// </summary>
    public enum BackupState
    {
        /// <summary>
        /// Default value: there is no running backup and history is empty.
        /// </summary>
        Initial,
        /// <summary>
        /// Indicates to the caller that the backup has started successfully.
        /// </summary>
        Started,
        /// <summary>
        /// Indicates to the caller that the backup is running.
        /// </summary>
        Executing,
        /// <summary>
        /// Indicates that the last backup is finished immediately.
        /// </summary>
        Finished,
        /// <summary>
        /// Indicates that the break of the running backup has been requested.
        /// </summary>
        CancelRequested,
        /// <summary>
        ///  Indicates to the caller that the backup operation is broken without any error.
        /// </summary>
        Canceled,
        /// <summary>
        /// Indicates that an error occured.
        /// </summary>
        Faulted,
    }
}
