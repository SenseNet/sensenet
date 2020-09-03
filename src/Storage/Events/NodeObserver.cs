using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Events
{
    public static class NodeObserverNames
    {
        public static readonly string NOTIFICATION = "SenseNet.Notification.NotificationObserver";
        public static readonly string WORKFLOWNOTIFICATION = "SenseNet.Workflow.WorkflowNotificationObserver";
        public static readonly string DOCUMENTPREVIEW = "SenseNet.Preview.DocumentPreviewObserver";
    }

    public abstract class NodeObserver
    {
        public static NodeObserver GetInstance(Type type)
        {
            return Providers.Instance.NodeObservers.FirstOrDefault(x => x.GetType() == type);
        }
        public static NodeObserver GetInstanceByGenericBaseType(Type baseType)
        {
            return Providers.Instance.NodeObservers.FirstOrDefault(x => x.GetType().IsSubclassOf(baseType));
        }
        public static Type[] GetObserverTypes()
        {
            return Providers.Instance.NodeObservers.Select(o => o.GetType()).ToArray();
        }

        // ================================================================================================ Safe caller methods

        internal static void InvokeEventHandlers<T>(EventHandler<T> handler, object sender, EventArgs e) where T : EventArgs
        {
            if (handler == null)
                return;

            Exception firstException = null;
            Delegate[] list = (Delegate[])handler.GetInvocationList().Clone();
            foreach (EventHandler<T> del in list)
            {
                try
                {
                    del(sender, e as T);
                }
                catch (Exception ex)
                {
                    if (firstException == null)
                        firstException = ex;
                }
            }

            if (firstException != null)
                throw firstException;
        }
        internal static void InvokeCancelEventHandlers(CancellableNodeEventHandler handler, object sender, CancellableNodeEventArgs e)
        {
            if (handler == null)
                return;

            Exception firstException = null;
            Delegate[] list = (Delegate[])handler.GetInvocationList().Clone();
            bool cancel = false;
            foreach (CancellableNodeEventHandler del in list)
            {
                try
                {
                    del(sender, e);
                    cancel = cancel | e.Cancel;
                    e.Cancel = cancel;
                }
                catch (Exception ex)
                {
                    if (firstException == null)
                        firstException = ex;
                }
            }

            if (firstException != null)
                throw firstException;
        }
        internal static void InvokeCancelOperationEventHandlers(CancellableNodeOperationEventHandler handler, object sender, CancellableNodeOperationEventArgs e)
        {
            if (handler == null)
                return;

            Exception firstException = null;
            Delegate[] list = (Delegate[])handler.GetInvocationList().Clone();
            bool cancel = false;
            foreach (CancellableNodeOperationEventHandler del in list)
            {
                try
                {
                    del(sender, e);
                    cancel = cancel | e.Cancel;
                    e.Cancel = cancel;
                }
                catch (Exception ex)
                {
                    if (firstException == null)
                        firstException = ex;
                }
            }

            if (firstException != null)
                throw firstException;
        }

        // ================================================================================================ Triggers

        internal static void FireOnStart(EventHandler<EventArgs> Start)
        {
            InvokeEventHandlers<EventArgs>(Start, null, EventArgs.Empty);
            var observers = Providers.Instance.NodeObservers;
            if (observers == null)
                return;
            foreach (NodeObserver observer in observers)
            {
                using (var op = SnTrace.Repository.StartOperation("NodeObserver OnStart " + observer.GetType().Name))
                {
                    observer.OnStart(null, EventArgs.Empty);
                    op.Successful = true;
                }
            }
        }

        internal static void FireOnReset(EventHandler<EventArgs> Reset)
        {
            InvokeEventHandlers<EventArgs>(Reset, null, EventArgs.Empty);
            var observers = Providers.Instance.NodeObservers;
            if (observers == null)
                return;
            foreach (NodeObserver observer in observers)
            {
                using (var op = SnTrace.Repository.StartOperation("NodeObserver OnReset " + observer.GetType().Name))
                {
                    observer.OnReset(null, EventArgs.Empty);
                    op.Successful = true;
                }
            }
        }
        internal static void FireOnNodeCreating(CancellableNodeEventHandler Creating, Node sender, CancellableNodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelEventHandlers(Creating, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeCreating(sender, e); });
        }
        internal static void FireOnNodeCreated(EventHandler<NodeEventArgs> Created, Node sender, NodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers<NodeEventArgs>(Created, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeCreated(sender, e); });
        }
        internal static void FireOnNodeModifying(CancellableNodeEventHandler Modifying, Node sender, CancellableNodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelEventHandlers(Modifying, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeModifying(sender, e); });
        }
        internal static void FireOnNodeModified(EventHandler<NodeEventArgs> Modified, Node sender, NodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers<NodeEventArgs>(Modified, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeModified(sender, e); });
        }
        internal static void FireOnNodeDeleting(CancellableNodeEventHandler Deleting, Node sender, CancellableNodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelEventHandlers(Deleting, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeDeleting(sender, e); });
        }
        internal static void FireOnNodeDeleted(EventHandler<NodeEventArgs> Deleted, Node sender, NodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers<NodeEventArgs>(Deleted, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeDeleted(sender, e); });
        }
        internal static void FireOnNodeDeletingPhysically(CancellableNodeEventHandler DeletingPhysically, Node sender, CancellableNodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelEventHandlers(DeletingPhysically, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeDeletingPhysically(sender, e); });
        }
        internal static void FireOnNodeDeletedPhysically(EventHandler<NodeEventArgs> DeletedPhysically, Node sender, NodeEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers<NodeEventArgs>(DeletedPhysically, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeDeletedPhysically(sender, e); });
        }
        internal static void FireOnNodeMoving(CancellableNodeOperationEventHandler Moving, Node sender, CancellableNodeOperationEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelOperationEventHandlers(Moving, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeMoving(sender, e); });
        }
        internal static void FireOnNodeMoved(EventHandler<NodeOperationEventArgs> Moved, Node sender, NodeOperationEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers<NodeOperationEventArgs>(Moved, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeMoved(sender, e); });
        }
        internal static void FireOnNodeCopying(CancellableNodeOperationEventHandler Copying, Node sender, CancellableNodeOperationEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelOperationEventHandlers(Copying, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeCopying(sender, e); });
        }
        internal static void FireOnNodeCopied(EventHandler<NodeOperationEventArgs> Copied, Node sender, NodeOperationEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers<NodeOperationEventArgs>(Copied, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnNodeCopied(sender, e); });
        }
        internal static void FireOnPermissionChanging(CancellableNodeEventHandler PermissionChanging, Node sender, CancellablePermissionChangingEventArgs e, List<Type> disabledObservers)
        {
            InvokeCancelEventHandlers(PermissionChanging, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnPermissionChanging(sender, e); });
        }
        internal static void FireOnPermissionChanged(EventHandler<PermissionChangedEventArgs> PermissionChanged, Node sender, PermissionChangedEventArgs e, List<Type> disabledObservers)
        {
            InvokeEventHandlers(PermissionChanged, sender, e);
            FireNodeEvent(disabledObservers, observer => { observer.OnPermissionChanged(sender, e); });
        }


        private static void FireNodeEvent(List<Type> disabledObservers, Action<NodeObserver> action)
        {
            var observers = Providers.Instance.NodeObservers;
            if (observers == null)
                return;

            foreach (NodeObserver observer in observers)
            {
                if (disabledObservers == null || !disabledObservers.Contains(observer.GetType()))
                {
                    //// Remove comment if you need to see the NodeObserver method calls in the detailed log.
                    //var methodName = action.Method.Name;
                    //var eventName = methodName.Substring(1, methodName.IndexOf('>') - 1);
                    //using (var op = SnTrace.Repository.StartOperation(
                    //    $"NodeObserver {eventName} {observer.GetType().Name}"))
                    //{
                        action(observer);
                    //    op.Successful = true;
                    //}
                }
            }
        }

        // ================================================================================================ Overridable observer methods

        protected virtual void OnReset(object sender, EventArgs e) { }
        protected virtual void OnStart(object sender, EventArgs e) { }
        protected virtual void OnNodeCreating(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnNodeCreated(object sender, NodeEventArgs e) { }
        protected virtual void OnNodeModifying(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnNodeModified(object sender, NodeEventArgs e) { }
        protected virtual void OnNodeDeleting(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnNodeDeleted(object sender, NodeEventArgs e) { }
        protected virtual void OnNodeDeletingPhysically(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnNodeDeletedPhysically(object sender, NodeEventArgs e) { }
        protected virtual void OnNodeMoving(object sender, CancellableNodeOperationEventArgs e) { }
        protected virtual void OnNodeMoved(object sender, NodeOperationEventArgs e) { }
        protected virtual void OnNodeCopying(object sender, CancellableNodeOperationEventArgs e) { }
        protected virtual void OnNodeCopied(object sender, NodeOperationEventArgs e) { }
        protected virtual void OnPermissionChanging(object sender, CancellablePermissionChangingEventArgs e) { }
        protected virtual void OnPermissionChanged(object sender, PermissionChangedEventArgs e) { }
    }
}