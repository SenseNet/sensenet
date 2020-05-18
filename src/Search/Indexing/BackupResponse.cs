using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Contains return information for the backup actions and status queries.
    /// </summary>
    public class BackupResponse
    {
        /// <summary>
        /// Gets or sets the backup state of the current request.
        /// </summary>
        public BackupState State { get; set; }
        /// <summary>
        /// Gets or sets the progress of the currently running backup operation or null.
        /// </summary>
        public BackupInfo Current { get; set; }
        /// <summary>
        /// Gets or sets the finished backup operations since the last start.
        /// </summary>
        public BackupInfo[] History { get; set; }
    }
}
