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

	    [Obsolete("Use the GetCustomData method instead.")]
        public object CustomData => GetCustomData(CancellableNodeEventArgs.CustomDataKey);

	    private readonly IDictionary<string, object> _customData;
	    /// <summary>
	    /// Gets a previously stored custom data during node life cycle events.
	    /// </summary>
	    public object GetCustomData(string key)
	    {
	        return _customData.TryGetValue(key, out var value) ? value : null;
	    }

        protected internal NodeEventArgs(Node node, NodeEvent eventType, IDictionary<string, object> customData) : this(node, eventType, customData, node.Path) { }

        internal NodeEventArgs(Node node, NodeEvent eventType, IDictionary<string, object> customData, string originalSourcePath) : this(node, eventType, customData, originalSourcePath, null) { }

        internal NodeEventArgs(Node node, NodeEvent eventType, IDictionary<string, object> customData, string originalSourcePath, IEnumerable<ChangedData> changedData)
        {
            this.SourceNode = node;
            this.User = AccessProvider.Current.GetCurrentUser();
            this.Time = DateTime.UtcNow;
            this.EventType = eventType;
            this.OriginalSourcePath = originalSourcePath;
            this.ChangedData = changedData;

            _customData = customData ?? new Dictionary<string, object>();
        }
    }
}