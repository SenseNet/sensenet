using System.Collections.Generic;
using System;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using System.Linq;
using System.Configuration;
using SenseNet.Configuration;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Schema
{
	internal sealed class NodeTypeManager : SchemaRoot
    {
        #region Distributed Action child class
        [Serializable]
        internal class NodeTypeManagerRestartDistributedAction : SenseNet.Communication.Messaging.DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return;

                NodeTypeManager.RestartPrivate();
            }
        }
        #endregion

        private static NodeTypeManager _current;
        private static readonly object _lock = new object();

		internal static NodeTypeManager Current
		{
			get
			{
                if(_current == null)
                {
                    lock(_lock)
                    {
                        if (_current == null)
                        {
                            LoadPrivate();
                        }
                    }
                }
                return _current;
			}
		}

		private NodeTypeManager()
		{
		}

        /// <summary>
        /// Distributes a NodeTypeManager restart (calls the NodeTypeManager.RestartPrivate()).
        /// </summary>
        internal static void Restart()
        {
            SnLog.WriteInformation("NodeTypeManager.Restart called.", EventId.RepositoryRuntime,
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });
            new NodeTypeManagerRestartDistributedAction().Execute();
        }


        /// <summary>
        /// Restarts the NodeTypeManager without sending any distributed action.
        /// Do not call this method explicitly, the system will call it if neccessary (when the reset is triggered by an another instance).
        /// </summary>
        private static void RestartPrivate()
        {
            SnLog.WriteInformation("NodeTypeManager.Restart executed.", EventId.RepositoryRuntime, 
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });
            OnReset();

            lock (_lock)
            {
                DataProvider.Current.Reset();
                LoadPrivate();
            }
        }

        internal static void Reload()
        {
            RestartPrivate();
            var c = Current;
        }

	    private static void LoadPrivate()
	    {
            // this method must be called inside a lock block!
            var current = new NodeTypeManager();
            current.Load();
            current.StartEventSystem();

            _current = current;
            
            NodeObserver.FireOnStart(Start);
	        SnLog.WriteInformation("NodeTypeManager created: " + _current);
	    }

	    public static event EventHandler<EventArgs> Start;
		public static event EventHandler<EventArgs> Reset;
		private static void OnReset()
		{
			NodeObserver.FireOnReset(Reset);
		}

		private List<NodeObserver> _nodeObservers;
        internal List<NodeObserver> NodeObservers
        {
            get { return _nodeObservers; }
        }

        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.DisabledNodeObservers instead.")]
        public static List<string> DisabledNodeObservers => RepositoryEnvironment.DisabledNodeObservers;

		private void StartEventSystem()
		{
            var nodeObserverTypes = TypeResolver.GetTypesByBaseType(typeof(NodeObserver));
            var activeObservers = nodeObserverTypes.Where(t => !t.IsAbstract).Select(t => (NodeObserver)Activator.CreateInstance(t, true))
                .Where(n => !RepositoryEnvironment.DisabledNodeObservers.Contains(n.GetType().FullName)).ToList();

            if (SnTrace.Repository.Enabled)
                TraceNodeObservers(nodeObserverTypes, RepositoryEnvironment.DisabledNodeObservers);

            var activeObserverNames = activeObservers.Select(x => x.GetType().FullName).ToArray();
            SnLog.WriteInformation("NodeObservers are instantiated. ", EventId.RepositoryLifecycle, 
                properties: new Dictionary<string, object> { { "Types", string.Join(", ", activeObserverNames) } });

            _nodeObservers = activeObservers;
        }
        private static bool _TraceNodeObserversCalled;
        private void TraceNodeObservers(Type[] nodeObserverTypes, List<string> disabledObserverNames)
        {
            if (!_TraceNodeObserversCalled)
            {
                SnTrace.Repository.Write("NodeObservers (count: {0}):", nodeObserverTypes.Length);
                for (int i = 0; i < nodeObserverTypes.Length; i++)
                {
                    var observerType = nodeObserverTypes[i];
                    var fullName = observerType.FullName;
                    SnTrace.Repository.Write("  #{0} ({1}): {2}:{3} // {4}",
                        i + 1,
                        disabledObserverNames.Contains(fullName) ? "disabled" : "active",
                        observerType.Name,
                        observerType.BaseType.Name,
                        observerType.Assembly.GetName().Name);
                }
                _TraceNodeObserversCalled = true;
            }
        }

        public static TypeCollection<PropertyType> GetDynamicSignature(int nodeTypeId, int contentListTypeId)
        {
            System.Diagnostics.Debug.Assert(nodeTypeId > 0);

            var nodePropertyTypes = NodeTypeManager.Current.NodeTypes.GetItemById(nodeTypeId).PropertyTypes;
            var allPropertyTypes = new TypeCollection<PropertyType>(nodePropertyTypes);
            if (contentListTypeId > 0)
                allPropertyTypes.AddRange(NodeTypeManager.Current.ContentListTypes.GetItemById(contentListTypeId).PropertyTypes);

            return allPropertyTypes;
        }

    }
}
