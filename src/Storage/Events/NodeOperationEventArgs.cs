using System;

namespace SenseNet.ContentRepository.Storage.Events
{
	public class NodeOperationEventArgs : NodeEventArgs
	{
		private Node _targetNode;
		public Node TargetNode { get { return _targetNode; } }

		public NodeOperationEventArgs(Node sourceNode, Node targetNode, NodeEvent eventType, object customData)
            : base(sourceNode, eventType, customData)
		{
			_targetNode = targetNode;
		}
        public NodeOperationEventArgs(Node sourceNode, Node targetNode, NodeEvent eventType, object customData, string originalSourcePath)
            : base(sourceNode, eventType, customData, originalSourcePath)
        {
            _targetNode = targetNode;
        }
    }

}