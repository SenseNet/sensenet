using System;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Events
{
	public class NodeOperationEventArgs : NodeEventArgs
	{
		private Node _targetNode;
		public Node TargetNode { get { return _targetNode; } }

		internal NodeOperationEventArgs(Node sourceNode, Node targetNode, NodeEvent eventType, IDictionary<string, object> customData)
            : base(sourceNode, eventType, customData)
		{
			_targetNode = targetNode;
		}
	    internal NodeOperationEventArgs(Node sourceNode, Node targetNode, NodeEvent eventType, IDictionary<string, object> customData, string originalSourcePath)
            : base(sourceNode, eventType, customData, originalSourcePath)
        {
            _targetNode = targetNode;
        }
    }

}