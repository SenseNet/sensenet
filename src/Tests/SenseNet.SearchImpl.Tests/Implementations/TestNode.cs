using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class TestNode : Node
    {
        public override bool IsContentType { get { return false; } }

        public TestNode(Node parent) : this(parent, null) { }
        public TestNode(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected TestNode(NodeToken nt) : base(nt) { }
    }
}
