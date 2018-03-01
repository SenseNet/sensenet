using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Tests.ContentHandlers
{
    [ContentHandler]
    public class EnumTestNode : Node
    {
        public enum TestEnum { Value0, Value1, Value2, Value3, Value4 }

        public EnumTestNode(Node parent) : this(parent, "EnumTestNode") { }
        public EnumTestNode(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected EnumTestNode(NodeToken nt) : base(nt) { }

        public override bool IsContentType { get { return false; } }

        public TestEnum TestProperty { get; set; }
    }
}
