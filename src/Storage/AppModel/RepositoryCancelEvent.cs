using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public class RepositoryCancelEvent : RepositoryEventBase
    {
        public RepositoryCancelEvent(string name) : base(name) { }
        public override bool Cancellable { get { return true; } }

        public void FireEvent(object sender, RepositoryCancelEventArgs args)
        {
            var eventHandlers = FindEventHandlerNodes(args.ContextNode);
            base.Fire<RepositoryCancelEventHandler, RepositoryCancelEventArgs>(eventHandlers, sender, args);
        }
    }
}
