using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Lucene29.Tests.Implementations
{
    internal class TestNode : Node, IIndexableDocument
    {
        public override bool IsContentType => false;

        public TestNode(Node parent) : this(parent, null) { }
        public TestNode(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected TestNode(NodeToken nt) : base(nt) { }
        public IEnumerable<IIndexableField> GetIndexableFields()
        {
            var content = Content.Create(this);
            var fields = content.Fields.Values;
            var indexableFields = fields.Where(f => f.IsInIndex).Cast<IIndexableField>().ToArray();
            return indexableFields;
        }
    }
}
