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
    /// <summary>
    /// Defines a content handler that can show another content in the place of the link.
    /// </summary>
    [ContentHandler]
    public class ContentLink : GenericContent
    {
        private static readonly List<string> _notLinkedFields = new List<string>(new[] { "Id", "ParentId", "VersionId", "Name", "Path", "Index", "InTree", "InFolder", "Depth", "Type", "TypeIs", "Version" });

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentLink"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public ContentLink(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentLink"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public ContentLink(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentLink"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected ContentLink(NodeToken tk) : base(tk) { }

        /// <summary>
        /// Gets a list of field names that are not transferred.
        /// </summary>
        public virtual List<string> NotLinkedFields
        {
            get
            {
                return _notLinkedFields;
            }
        }

        private bool? _isAlive;
        /// <summary>
        /// Gets true if the linked content exists and current <see cref="User"/> has enough permission to open it (<see cref="PermissionType.Open"/>).
        /// </summary>
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

        /// <summary>
        /// Gets or sets the persistent linked <see cref="Node"/> instance.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// This property is not used if the linked content is resolved dinamically.
        /// For business purposes use the LinkedContent property instead.
        /// </summary>
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

        /// <inheritdoc />
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
        /// <inheritdoc />
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
        /// <summary>
        /// Gets the linked <see cref="GenericContent"/> instance.
        /// </summary>
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

        /// <summary>
        /// Customizable method that resolves the linked <see cref="GenericContent"/> instance.
        /// </summary>
        protected virtual GenericContent ResolveLinkedContent()
        {
            return Link as GenericContent;
        }

        /// <summary>
        /// Gets value of the linked content'"s Icon property.
        /// </summary>
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
