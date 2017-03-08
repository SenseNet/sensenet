using System;

namespace SenseNet.ContentRepository.Storage.Events
{
	public class CancellableNodeOperationEventArgs : CancellableNodeEventArgs
	{
		private Node _targetNode;

		public Node TargetNode { get { return _targetNode; } }

		public CancellableNodeOperationEventArgs(Node sourceNode, Node targetNode, CancellableNodeEvent eventType)
			: base(sourceNode, eventType)
		{
			_targetNode = targetNode;
		}
	}
}