using System;
using System.Collections;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class IndexIntegrityCheckerItem
    {
        public int VersionId { get; set; }
        public long VersionTimestamp { get; set; }

        public int NodeId { get; set; }
        public long NodeTimestamp { get; set; }
        public int LastMajorVersionId { get; set; }
        public int LastMinorVersionId { get; set; }
    }
}
