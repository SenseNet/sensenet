using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class TestNode : Node, IIndexableDocument
    {
        public override bool IsContentType { get { return false; } }

        public TestNode(Node parent) : this(parent, null) { }
        public TestNode(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected TestNode(NodeToken nt) : base(nt) { }
        public IEnumerable<IIndexableField> GetIndexableFields()
        {
            var content = Content.Create(this);
            var fields = content.Fields.Values;
            var indexableFields = fields.Where(f => f.IsInIndex).Cast<IIndexableField>().ToArray();
            var names = fields.Select(f => f.Name).ToArray();
            var indexableNames = indexableFields.Select(f => f.Name).ToArray();
            return indexableFields;
        }
    }
}
