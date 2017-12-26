using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A Content handler class for representing a container for child Content items.
    /// </summary>
	[ContentHandler]
    public class Folder : GenericContent, IFolder
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Folder(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Folder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected Folder(NodeToken nt) : base(nt) { }

	    /// <inheritdoc select="summary" />
        /// <remarks>This value cannot be modified in case of simple Folders,
        /// they inherit this list from their parent.</remarks>
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

	    /// <inheritdoc />
        public virtual IEnumerable<Node> Children
        {
            get { return this.GetChildren(); }
        }
	    /// <inheritdoc />
        public virtual int ChildCount
        {
            get { return this.GetChildCount();}
        }

        /// <inheritdoc />
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
	    /// <inheritdoc />
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