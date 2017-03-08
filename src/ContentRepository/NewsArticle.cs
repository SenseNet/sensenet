using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    internal class NewsArticle : GenericContent, IFolder
    {

        public NewsArticle(Node parent) : this(parent, null) { }
		public NewsArticle(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected NewsArticle(NodeToken nt) : base(nt) { }

        #region IFolder Members

        public IEnumerable<SenseNet.ContentRepository.Storage.Node> Children
        {
            get { return this.GetChildren(); }
        }

        public int ChildCount
        {
            get { return this.GetChildCount(); }
        }

        #endregion
    }
}
