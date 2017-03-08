using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public abstract class RepositoryCancelEventHandler : RepositoryEventHandlerBase
    {
        public RepositoryCancelEventHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected RepositoryCancelEventHandler(NodeToken nt) : base(nt) { }

        public abstract void HandleEvent(object sender, RepositoryCancelEventArgs e);
    }
}
