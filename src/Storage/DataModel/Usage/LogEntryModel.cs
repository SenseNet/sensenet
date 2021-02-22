using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Storage.DataModel.Usage
{
    /// <summary>
    /// Represents a log entry in the database usage profile.
    /// </summary>
    public class LogEntriesTableModel
    {
        /// <summary>
        /// Size of the changed data properties in bytes.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Size of all metadata in bytes.
        /// </summary>
        public long Metadata { get; set; }
        /// <summary>
        /// Size of the formatted message in bytes.
        /// </summary>
        public long Text { get; set; }
    }
}
