using SenseNet.ContentRepository.Storage.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{
    /// <summary>
    /// Read only audit log entry for diagnostic purposes
    /// </summary>
    public class AuditLogEntry
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Title { get; set; }
        public int ContentId { get; set; }
        public string ContentPath { get; set; }
        public string UserName { get; set; }
        public DateTime LogDate { get; set; }
        public string Message { get; set; }
        public string FormattedMessage { get; set; }

        public static AuditLogEntry[] LoadLastEntries(int count)
        {
            return DataProvider.Current.LoadLastAuditLogEntries(count);
        }
    }
}
