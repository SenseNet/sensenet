using System;
using System.ComponentModel;
using SenseNet.ContentRepository.Storage.Security;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Events
{
	public class CancellableNodeEventArgs : CancelEventArgs, INodeEventArgs
	{
		private Node _sourceNode;
		private IUser _user;
		private DateTime _time;
		private CancellableNodeEvent _eventType;
		private string _cancelMessage;

		public Node SourceNode { get { return _sourceNode; } }
		public IUser User { get { return _user; } }
		public DateTime Time { get { return _time; } }
		public CancellableNodeEvent EventType { get { return _eventType; } }
        public IEnumerable<ChangedData> ChangedData { get; private set; }
        public object CustomData { get; set; }

		public string CancelMessage
		{
			get { return _cancelMessage; }
			set { _cancelMessage = value; }
		}

        internal CancellableNodeEventArgs(Node node, CancellableNodeEvent eventType) : this(node, eventType, null) { }

		internal CancellableNodeEventArgs(Node node, CancellableNodeEvent eventType, IEnumerable<ChangedData> changedData)
		{
			AccessProvider.Current.GetCurrentUser();
			_sourceNode = node;
			_user = AccessProvider.Current.GetCurrentUser();
			_time = DateTime.UtcNow;
			_eventType = eventType;
            this.ChangedData = changedData;
		}
	}

}