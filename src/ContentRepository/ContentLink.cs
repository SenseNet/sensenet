using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ContentLink : GenericContent
    {
        private static readonly List<string> _notLinkedFields = new List<string>(new[] { "Id", "ParentId", "VersionId", "Name", "Path", "Index", "InTree", "InFolder", "Depth", "Type", "TypeIs", "Version" });

        public ContentLink(Node parent) : this(parent, null) { }
        public ContentLink(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ContentLink(NodeToken tk) : base(tk) { }

        public virtual List<string> NotLinkedFields
        {
            get
            {
                return _notLinkedFields;
            }
        }

        private bool? _isAlive;
        public bool IsAlive
        {
            get
            {
                // check contentlink only once for performance reasons
                if (!_isAlive.HasValue)
                {
                    using (new SystemAccount())
                    {
                        var l = LinkedContent;
                        _isAlive = l != null && l.Security.HasPermission(User.LoggedInUser, PermissionType.See, PermissionType.Open);
                    }
                }

                return _isAlive.Value;
            }
        }

        [RepositoryProperty("Link", RepositoryDataType.Reference)]
        public Node Link
        {
            get { return base.GetReference<Node>("Link"); }
            set
            {
                base.SetReference("Link", value);
                _isAlive = null;
                _resolved = false;
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "Link":
                    return this.Link;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "Link":
                    this.Link = (Node)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        private bool _resolved;
        private GenericContent _linkedContent;
        public GenericContent LinkedContent
        {
            get
            {
                if (!_resolved)
                {
                    _linkedContent = ResolveLinkedContent();
                    _resolved = true;
                    _isAlive = null;
                }
                return _linkedContent;
            }
        }

        protected virtual GenericContent ResolveLinkedContent()
        {
            return Link as GenericContent;
        }

        public override string Icon
        {
            get
            {
                if (IsAlive)
                    return LinkedContent.Icon;
                return base.Icon;
            }
        }

        private  bool HasField(string name)
        {
            if (LinkedContent.HasProperty(name))
                return true;
            var ct = ContentType.GetByName(LinkedContent.NodeType.Name);
            return ct.FieldSettings.Exists(delegate(FieldSetting fs) { return fs.Name == name; });
        }
    }
}
