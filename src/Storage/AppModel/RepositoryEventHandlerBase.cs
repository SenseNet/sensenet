using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public abstract class RepositoryEventHandlerBase : Node
    {
        public RepositoryEventHandlerBase(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected RepositoryEventHandlerBase(NodeToken nt) : base(nt) { }

        public abstract bool StopEventBubbling { get; set; }
    }
}
