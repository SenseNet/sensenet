using System.Collections.Generic;
using System;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage.Schema
{
	internal sealed class NodeTypeManager : SchemaRoot
    {
        #region Distributed Action child class
        [Serializable]
        internal class NodeTypeManagerRestartDistributedAction : SenseNet.Communication.Messaging.DistributedAction
        {
            public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return Task.CompletedTask;

                NodeTypeManager.RestartPrivate();

                return Task.CompletedTask;
            }
        }
        #endregion

        //private static NodeTypeManager _current;
        private static readonly object _lock = new object();

		internal static NodeTypeManager Current
		{
			get
			{
                if(Providers.Instance.NodeTypeManeger == null)
                {
                    lock(_lock)
                    {
                        if (Providers.Instance.NodeTypeManeger == null)
                        {
                            LoadPrivate();
                        }
                    }
                }
                return Providers.Instance.NodeTypeManeger;
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
            new NodeTypeManagerRestartDistributedAction().ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
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
                DataStore.Reset();
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

            Providers.Instance.NodeTypeManeger = current;
            
            NodeObserver.FireOnStart(Start);
	        SnLog.WriteInformation("NodeTypeManager created: " + Providers.Instance.NodeTypeManeger);
	    }

        internal static NodeTypeManager CreateForTests()
        {
            return new NodeTypeManager();
        }


        public static event EventHandler<EventArgs> Start;
		public static event EventHandler<EventArgs> Reset;
		private static void OnReset()
		{
			NodeObserver.FireOnReset(Reset);
		}

        [Obsolete("After V6.5 PATCH 9: Use RepositoryEnvironment.DisabledNodeObservers instead.")]
        public static List<string> DisabledNodeObservers => RepositoryEnvironment.DisabledNodeObservers;

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
