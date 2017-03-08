using System;
using SenseNet.ContentRepository.Storage.Security;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Events
{
	public class NodeEventArgs : EventArgs, INodeEventArgs
	{
		public Node SourceNode { get; private set; }
		public IUser User { get; private set; }
		public DateTime Time { get; private set; }
		public NodeEvent EventType { get; private set; }
        public string OriginalSourcePath { get; private set; }
        public IEnumerable<ChangedData> ChangedData { get; private set; }
        public object CustomData { get; private set; }


		internal NodeEventArgs(Node node, NodeEvent eventType, object customData) : this(node, eventType, customData, node.Path) { }

        internal NodeEventArgs(Node node, NodeEvent eventType, object customData, string originalSourcePath) : this(node, eventType, customData, originalSourcePath, null) { }

        internal NodeEventArgs(Node node, NodeEvent eventType, object customData, string originalSourcePath, IEnumerable<ChangedData> changedData)
        {
            this.SourceNode = node;
            this.User = AccessProvider.Current.GetCurrentUser();
            this.Time = DateTime.UtcNow;
            this.EventType = eventType;
            this.OriginalSourcePath = originalSourcePath;
            this.ChangedData = changedData;
            this.CustomData = customData;
        }
    }
}