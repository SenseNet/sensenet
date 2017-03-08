using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public abstract class RepositoryEventBase
    {
        private static readonly string EventsAppFolderName = "Events";
        protected static readonly ApplicationQuery EventsQuery = new ApplicationQuery(EventsAppFolderName, true, true, HierarchyOption.TypeAndPath, true);

        public static RepositoryEvent Copied { get; private set; }
        public static RepositoryEvent Created { get; private set; }
        public static RepositoryEvent Deleted { get; private set; }
        public static RepositoryEvent DeletedPhysically { get; private set; }
        public static RepositoryEvent Modified { get; private set; }
        public static RepositoryEvent Moved { get; private set; }

        public static RepositoryCancelEvent Copying { get; private set; }
        public static RepositoryCancelEvent Creating { get; private set; }
        public static RepositoryCancelEvent Deleting { get; private set; }
        public static RepositoryCancelEvent DeletingPhysically { get; private set; }
        public static RepositoryCancelEvent Modifying { get; private set; }
        public static RepositoryCancelEvent Moving { get; private set; }

        private static List<RepositoryEventBase> _allEvents = new List<RepositoryEventBase>(new RepositoryEventBase[]
        {
            Copied = new RepositoryEvent("Copied"),
            Created = new RepositoryEvent("Created"),
            Deleted = new RepositoryEvent("Deleted"),
            DeletedPhysically = new RepositoryEvent("DeletedPhysically"),
            Modified = new RepositoryEvent("Modified"),
            Moved = new RepositoryEvent("Moved"),

            Copying = new RepositoryCancelEvent("Copying"),
            Creating = new RepositoryCancelEvent("Creating"),
            Deleting = new RepositoryCancelEvent("Deleting"),
            DeletingPhysically = new RepositoryCancelEvent("DeletingPhysically"),
            Modifying = new RepositoryCancelEvent("Modifying"),
            Moving = new RepositoryCancelEvent("Moving")
        });

        public string EventName { get; private set; }
        public abstract bool Cancellable { get; }

        protected RepositoryEventBase(string eventName)
        {
            this.EventName = eventName;
        }

        public IEnumerable<RepositoryEventBase> GetAllEvents()
        {
            return _allEvents.AsReadOnly();
        }

        protected IEnumerable<Node> FindEventHandlerNodes(Node contextNode)
        {
            var nodeHeads = EventsQuery.ResolveApplications(this.EventName, contextNode);
            var eventHandlers = new List<Node>();
            foreach (var nodeHead in nodeHeads)
            {
                var eventHandlerNode = Node.LoadNode(nodeHead.Id);
                if (eventHandlerNode != null)
                    eventHandlers.Add(eventHandlerNode);
            }
            return eventHandlers;
        }

        protected bool Fire<THandler, TArgs>(IEnumerable<Node> eventHandlers, object sender, TArgs args) 
            where THandler : RepositoryEventHandlerBase 
            where TArgs : RepositoryEventArgs
        {
            var cancel = false;
            var exceptions = new List<Exception>();
            string finishLevel = null;
            foreach (var eventHandlerNode in eventHandlers)
            {
                var eventHandler = eventHandlerNode as THandler;
                var realSender = GetSender((RepositoryEventHandlerBase)eventHandlerNode);
                if (eventHandler == null)
                    throw new InvalidCastException(String.Concat("Type of ", this.EventName, " event handler must be ", typeof(THandler), ". Path: ", eventHandlerNode.Path));
                try
                {
                    if (finishLevel != null)
                    {
                        if (!eventHandler.Path.StartsWith(finishLevel))
                            break;
                    }

                    if(Cancellable)
                        InvokeEventHandler(eventHandler as RepositoryCancelEventHandler, realSender, args as RepositoryCancelEventArgs, out cancel);
                    else
                        InvokeEventHandler(eventHandler as RepositoryEventHandler, realSender, args as RepositoryEventArgs, out cancel);

                    if (cancel)
                        break;
                    if (args.Handled)
                        break;
                    if (eventHandler.StopEventBubbling)
                        finishLevel = RepositoryPath.GetParentPath(eventHandler.Path) + "/";
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            if (exceptions.Count > 0)
                throw new RepositoryEventException(args.ContextNode.Path, this, exceptions);
            return cancel;
        }
        private Node GetSender(RepositoryEventHandlerBase handler)
        {
            var p = handler.Path.IndexOf(String.Concat("/", EventsAppFolderName, "/"));
            if (p < 2)
                return null;
            var path = handler.Path.Substring(0, p);
            return Node.LoadNode(path);
        }

        private void InvokeEventHandler(RepositoryEventHandler eventHandler, object sender, RepositoryEventArgs args, out bool cancel)
        {
            eventHandler.HandleEvent(sender, args);
            cancel = false;
        }
        private void InvokeEventHandler(RepositoryCancelEventHandler eventHandler, object sender, RepositoryCancelEventArgs args, out bool cancel)
        {
            eventHandler.HandleEvent(sender, args);
            cancel = args.Cancel;
        }

    }
}
