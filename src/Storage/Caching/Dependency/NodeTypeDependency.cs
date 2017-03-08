using System;
using System.Web.Caching;
using SenseNet.Communication.Messaging;
using Cache = SenseNet.Configuration.Cache;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class NodeTypeDependency : CacheDependency
    {
        #region private class FireChangedDistributedAction
        [Serializable]
        private class FireChangedDistributedAction : DistributedAction
        {
            private int _nodeTypeId;

            private FireChangedDistributedAction(int nodeTypeId)
            {
                _nodeTypeId = nodeTypeId;
            }

            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                FireChangedPrivate(_nodeTypeId);
            }

            internal static void Trigger(int nodeTypeId)
            {
                new FireChangedDistributedAction(nodeTypeId).Execute();
            }
        }
        // -----------------------------------------------------------------------------------------
        #endregion

        private int _nodeTypeId;
        private static readonly EventServer<int> Changed = new EventServer<int>(Cache.NodeTypeDependencyEventPartitions);

        public NodeTypeDependency(int nodeTypeId)
        {
            _nodeTypeId = nodeTypeId;
            try
            {
                lock (PortletDependency._eventSync)
                {
                    Changed.TheEvent += NodeTypeDependency_NodeTypeChanged;
                }
            }
            finally
            {
                this.FinishInit();
            }
        }

        private void NodeTypeDependency_NodeTypeChanged(object sender, EventArgs<int> e)
        {
            if (e.Data == _nodeTypeId)
                NotifyDependencyChanged(this, e);
        }

        protected override void DependencyDispose()
        {
            lock (PortletDependency._eventSync)
            {
                Changed.TheEvent -= NodeTypeDependency_NodeTypeChanged;
            }
        }

        public static void FireChanged(int nodeTypeId)
        {
            FireChangedDistributedAction.Trigger(nodeTypeId);
        }
        private static void FireChangedPrivate(int nodeTypeId)
        {
            lock (PortletDependency._eventSync)
            {
                Changed.Fire(null, nodeTypeId);
            }
        }
    }
}
