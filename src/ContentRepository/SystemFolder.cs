using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class SystemFolder : Folder
    {
        public SystemFolder(Node parent) : this(parent, null) { Initialize(); }
        public SystemFolder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { Initialize(); }
        protected SystemFolder(NodeToken nt) : base(nt) { }

        protected override void Initialize()
        {
            base.Initialize();
            this.IsSystem = true;
        }

        public static GenericContent GetSystemContext(Node child)
        {
            SystemFolder ancestor = null;

            while ((child != null) && ((ancestor = child as SystemFolder) == null))
                child = child.Parent;

            return (ancestor != null) ? ancestor.Parent as GenericContent : null;
        }

    }
}
