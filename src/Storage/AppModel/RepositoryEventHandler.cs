using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public abstract class RepositoryEventHandler : RepositoryEventHandlerBase
    {
        public RepositoryEventHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected RepositoryEventHandler(NodeToken nt) : base(nt) { }

        public abstract void HandleEvent(object sender, RepositoryEventArgs e);
    }
}
