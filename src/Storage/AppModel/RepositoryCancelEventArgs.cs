using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public class RepositoryCancelEventArgs : RepositoryEventArgs
    {
        public bool Cancel { get; set; }
        public string CancelMessage { get; set; }
        public RepositoryCancelEventArgs(Node contextNode) : base(contextNode) { }
    }
}
