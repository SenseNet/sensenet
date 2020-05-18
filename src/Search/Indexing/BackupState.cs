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
        /// Default value: there is no running backup.
        /// </summary>
        Stopped,
        /// <summary>
        /// Indicates to the caller that the backup has started successfully.
        /// </summary>
        Started,
        /// <summary>
        /// Indicates to the caller that the backup is running.
        /// </summary>
        AlreadyStarted,
        /// <summary>
        /// Indicates to the caller that the backup is finished immediately and
        /// no more action required.
        /// </summary>
        Finished,
        /// <summary>
        /// Indicates that an error occured.
        /// </summary>
        Faulted,
        /// <summary>
        ///  Indicates to the caller that the backup operation is broken without any error.
        /// </summary>
        Canceled
    }
}
