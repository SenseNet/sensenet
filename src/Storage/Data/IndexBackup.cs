using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class IndexBackup
    {
        private static string[] _tableNames = new string[] { "[IndexBackup]", "[IndexBackup2]" };

        public int IndexBackupId { get; internal set; }
        public int BackupNumber { get; internal set; }
        public int TableIndex { get { return (BackupNumber + 1) % 2; } }
        public string TableName { get { return _tableNames[TableIndex]; } }
        public DateTime BackupDate { get; set; }
        public string ComputerName { get; set; }
        public string AppDomainName { get; set; }
        public long BackupFileLength { get; internal set; }
        public Guid RowGuid { get; internal set; }
        public long Timestamp { get; internal set; }
    }
}
