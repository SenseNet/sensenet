using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository
{
	[ContentHandler]
    public class Folder : GenericContent, IFolder
	{
        public Folder(Node parent) : this(parent, null) { }
		public Folder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
		protected Folder(NodeToken nt) : base(nt) { }

        public override IEnumerable<ContentType> AllowedChildTypes
        {
            get { return base.AllowedChildTypes; }
            set
            {
                if (this.NodeType.Name == "Folder")
                    return;
                base.AllowedChildTypes = value;
            }
        }

        public virtual IEnumerable<Node> Children
        {
            get { return this.GetChildren(); }
        }
        public virtual int ChildCount
        {
            get { return this.GetChildCount(); }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case GenericContent.ALLOWEDCHILDTYPES:
                    return this.AllowedChildTypes;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case GenericContent.ALLOWEDCHILDTYPES:
                    this.AllowedChildTypes = (IEnumerable<ContentType>)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }


    }
}