using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ApplicationCacheFile : File
    {
        public ApplicationCacheFile(Node parent) : this(parent, null) { }
        public ApplicationCacheFile(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ApplicationCacheFile(NodeToken nt) : base(nt) { }

        // ========================================================== Cached data

        private IEnumerable<string> cachedData = new List<string>();
        public IEnumerable<string> CachedData { get { return cachedData; } }
    }
}
