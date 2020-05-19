using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Represents a backup operation.
    /// </summary>
    public class BackupInfo
    {
        /// <summary>
        /// Gets or sets the UTC time of the start.
        /// </summary>
        public DateTime StartedAt { get; set; }
        /// <summary>
        /// Gets or sets the UTC time of the finish.
        /// The value is DateTime.MinValue if the operation is unfinished.
        /// </summary>
        public DateTime FinishedAt { get; set; }
        /// <summary>
        /// Gets or sets the total length of the files to be copied.
        /// </summary>
        public long TotalBytes { get; set; }
        /// <summary>
        /// Gets or sets the total length of the copied files.
        /// </summary>
        public long CopiedBytes { get; set; }
        /// <summary>
        /// Gets or sets the count of the files to be copied.
        /// </summary>
        public int CountOfFiles { get; set; }
        /// <summary>
        /// Gets or sets the count of the copied files.
        /// </summary>
        public int CopiedFiles { get; set; }
        /// <summary>
        /// Gets or sets the name of the currently copied file.
        /// </summary>
        public string CurrentlyCopiedFile { get; set; }
        /// <summary>
        /// Gets or sets the error or cancellation message.
        /// In case of currently executing or successfully finished operations the value is null.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Creates a copy.
        /// </summary>
        public BackupInfo Clone()
        {
            return new BackupInfo
            {
                StartedAt = StartedAt,
                FinishedAt = FinishedAt,
                TotalBytes = TotalBytes,
                CopiedBytes = CopiedBytes,
                CountOfFiles = CountOfFiles,
                CopiedFiles = CopiedFiles,
                CurrentlyCopiedFile = CurrentlyCopiedFile,
                Message = Message
            };
        }
    }
}
