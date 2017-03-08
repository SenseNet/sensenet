using System;
using System.Globalization;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage
{
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("NodeId: {NodeId}, VersionId: {}, Version: {VersionNumber.ToString()}, Type: {NodeType.ClassName}, Path:{NodeData.Path}")]
    public class NodeToken
    {
        [NonSerialized]
        private TypeCollection<PropertyType> _propertyTypes;
        [NonSerialized]
        private TypeCollection<PropertyType> _contentListPropertyTypes;
        [NonSerialized]
        private TypeCollection<PropertyType> _allPropertyTypes;


        internal NodeToken(int nodeId, int nodeTypeId, int contentListId, int contentListTypeId, int versionId, VersionNumber version)
        {
            this.NodeId = nodeId;
            this.NodeTypeId = nodeTypeId;
            this.ContentListId = contentListId;
            this.ContentListTypeId = contentListTypeId;
            this.VersionId = versionId;
            this.VersionNumber = version;
        }


        public int NodeId { get; private set; }
        internal int VersionId { get; private set; }
        public VersionNumber VersionNumber { get; private set; }
        public int NodeTypeId { get; private set; }
        public int ContentListTypeId { get; private set; }
        public int ContentListId { get; private set; }

        internal NodeHead NodeHead { get; set; }
        internal NodeData NodeData { get; set; }

        public NodeType NodeType
        {
            get { return NodeTypeManager.Current.NodeTypes.GetItemById(NodeTypeId); }
        }
        public ContentListType ContentListType
        {
            get { return this.ContentListTypeId == 0 ? (ContentListType)null : NodeTypeManager.Current.ContentListTypes.GetItemById(ContentListTypeId); }
        }
        /// <summary>
        /// Gets the property types.
        /// </summary>
        /// <value>The property types.</value>
        public TypeCollection<PropertyType> PropertyTypes
        {
            get
            {
                if (_propertyTypes == null)
                    _propertyTypes = this.NodeType.PropertyTypes;
                return _propertyTypes;
            }
        }
        /// <summary>
        /// Gets the ContentList property types if this node is a ContentListItem.
        /// </summary>
        /// <value>The property types.</value>
        public TypeCollection<PropertyType> ContentListPropertyTypes
        {
            get
            {
                if (_contentListPropertyTypes == null)
                {
                    ContentListType listType = null;
                    if ((listType = this.ContentListType) == null)
                        _contentListPropertyTypes = new TypeCollection<PropertyType>(NodeTypeManager.Current);
                    else
                        _contentListPropertyTypes = this.ContentListType.PropertyTypes;
                }
                return _contentListPropertyTypes;
            }
        }
        /// <summary>
        /// Gets the all PropertyTypes of node (union of PropertyTypes and ContentListPropertyTypes)
        /// </summary>
        public TypeCollection<PropertyType> AllPropertyTypes
        {
            get
            {
                if (_allPropertyTypes == null)
                {
                    _allPropertyTypes = new TypeCollection<PropertyType>(this.PropertyTypes);
                    _allPropertyTypes.AddRange(this.ContentListPropertyTypes);
                }
                return _allPropertyTypes;
            }
        }
    }
}