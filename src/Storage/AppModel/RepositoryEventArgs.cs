using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public class RepositoryEventArgs : EventArgs
    {
        public Node ContextNode { get; private set; }
        public bool Handled { get; set; }

        public RepositoryEventArgs(Node contextNode)
        {
            ContextNode = contextNode;
        }
    }
}
