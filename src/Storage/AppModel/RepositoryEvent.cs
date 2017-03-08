using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public class RepositoryEvent : RepositoryEventBase
    {
        public RepositoryEvent(string name) : base(name) { }
        public override bool Cancellable { get { return false; } }

        public void FireEvent(object sender, RepositoryEventArgs args)
        {
            var eventHandlers = FindEventHandlerNodes(args.ContextNode);
            base.Fire<RepositoryEventHandler, RepositoryEventArgs>(eventHandlers, sender, args);
        }
    }
}
