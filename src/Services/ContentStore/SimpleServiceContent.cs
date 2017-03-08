using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using sn = SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Services.ContentStore
{
    [DataContract]
    [KnownType(typeof(EntityReference))]
    [XmlInclude(typeof(EntityReference))]
    [KnownType(typeof(ContentProperty))]
    [XmlInclude(typeof(ContentProperty))]
    public class SimpleServiceContent
    {
        #region Members

        // Non-serialized stuff /////////////////////////////////////////////
        
        private Node ContentNode { get; set; }

        private sn.Content _contentFromNode;
        private sn.Content ContentFromNode
        {
            get { return _contentFromNode ?? (_contentFromNode = sn.Content.Create(ContentNode)); }
        }

        private sn.Schema.ContentType _contentAsType;
        private sn.Schema.ContentType ContentAsType
        {
            get { return _contentAsType ?? (_contentAsType = ContentNode as sn.Schema.ContentType); }
        }

        // Serialized interface /////////////////////////////////////////////

        private int? _id;
        [DataMember]
        public int Id
        {
            get { return _id ?? ContentNode.Id; }
            set { _id = value; }
        }

        // hack for JS case sensitivity
        [DataMember]
        public int id
        {
            get { return Id; }
            set { Id = value; }
        }
        // endhack

        private string _name;
        [DataMember]
        public string Name
        {
            get { return _name ?? ContentNode.Name; }
            set { _name = value; }
        }

        private string _path;
        [DataMember]
        public string Path
        {
            get { return _path ?? ContentNode.Path; }
            set { _path = value; } 
        }

        private string _displayName;
        [DataMember]
        public string DisplayName
        {
            get
            {
                if (_displayName != null)
                    return _displayName;

                if (ContentAsType != null)
                    return sn.Content.Create(ContentAsType).DisplayName;

                // Needs revision! This shouldn't be what it is.
                return ContentFromNode.DisplayName;
            }
            set { _displayName = value; }
        }

        [DataMember]
        public string Description
        {
            get { return ContentNode.IsHeadOnly ? string.Empty : ContentFromNode.Description; }
            set { /* do nothing */ }
        }
        
        private string _nodeTypeName;
        [DataMember]
        public string NodeTypeName
        {
            get { return _nodeTypeName ?? ContentNode.NodeType.Name; }
            set { _nodeTypeName = value; }
        }

        private string _nodeTypeTitle;
        [DataMember]
        public string NodeTypeTitle
        {
            get 
            {
                if (_nodeTypeTitle != null)
                    return _nodeTypeTitle;

                var cType = ContentRepository.Schema.ContentType.GetByName(ContentNode.NodeType.Name);
                if (cType != null)
                {
                    if (!string.IsNullOrEmpty(cType.DisplayName))
                        return ContentRepository.Content.Create(cType).DisplayName;
                    return cType.Name;
                }
                return string.Empty;
            }
            set { _nodeTypeTitle = value; }
        }

        private object _nodeType;
        [DataMember]
        public object NodeType
        {
            get { return _nodeType ?? new EntityReference() { Uri = ContentNode.NodeType.NodeTypePath }; }
            set { _nodeType = value; }
        }

        [DataMember]
        public string ContentTypeName
        {
            get { return NodeTypeName; }
            set { NodeTypeName = value; }
        }

        private string _icon;
        [DataMember]
        public String Icon
        {
            get { return _icon ?? ContentFromNode.Icon; }
            set { _icon = value; }
        }

        private string _iconPath;
        [DataMember]
        public String IconPath
        {
            get { return _iconPath ?? (_iconPath = string.Concat("/Root/Global/images/icons/16/", ContentFromNode.Icon, ".png")); }
            set { _iconPath = value; }
        }

        private int? _index;
        [DataMember]
        public int Index
        {
            get { return _index ?? ContentNode.Index; }
            set { _index = value; }
        }

        private bool? _leaf;
        [DataMember]
        public bool Leaf
        {
            get { return _leaf ?? !(ContentNode is sn.IFolder); }
            set { _leaf = value; }
        }

        [DataMember]
        public bool IsSystemContent
        {
            get
            {
                return ContentNode.NodeType.IsInstaceOfOrDerivedFrom("SystemFolder") ||
                       ContentNode.NodeType.IsInstaceOfOrDerivedFrom("SystemFile");
            }
            set { }
        }

        private int? _parentId;
        [DataMember]
        public int ParentId
        {
            get
            {
                if (_parentId == null)
                    _parentId = ContentNode.ParentId;
                return _parentId.Value;
            }
            set { _parentId = value; }
        }

        #endregion

        public SimpleServiceContent() { }
        public SimpleServiceContent(Node contentNode)
        {
            ContentNode = contentNode;
        }
    }
}
