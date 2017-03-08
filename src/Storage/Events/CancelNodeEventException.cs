using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Events
{
	[global::System.Serializable]
	public class CancelNodeEventException : ApplicationException
	{
		public CancellableNodeEvent Event { get; private set; }
		public Node Node { get; private set; }

        public CancelNodeEventException() : base() { }
        public CancelNodeEventException(string cancelMessage) : base(cancelMessage) { }
        public CancelNodeEventException(string cancelMessage, Exception innerException) : base(cancelMessage, innerException) { }
        public CancelNodeEventException(string cancelMessage, CancellableNodeEvent cancelEvent, Node node)
			: base(cancelMessage)
		{
			this.Event = cancelEvent;
			this.Node = node;
			this.Data.Add("CancelMessage", cancelMessage);
		}
		public CancelNodeEventException(string cancelMessage, CancellableNodeEvent cancelEvent, Node node, Exception innerException)
			: base(cancelMessage, innerException)
		{
			this.Event = cancelEvent;
			this.Node = node;
			this.Data.Add("CancelMessage", cancelMessage);
		}
		protected CancelNodeEventException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
        {
            var id = info.GetInt32("Node");
            Node = id == 0 ? null : Node.LoadNode(id);
            Event = (CancellableNodeEvent)info.GetValue("Event", typeof(CancellableNodeEvent));
        }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Node", Node == null ? 0 : Node.Id);
            info.AddValue("Event", Event);
        }

	}

}