using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage.Search.Internal
{
    public class PropertyLiteral
    {
        private bool _isSlot;
        private PropertyType _propertySlot;
        private NodeAttribute _nodeAttribute;

        public bool IsSlot
        {
            get { return _isSlot; }
        }
        public PropertyType PropertySlot
        {
            get { return _propertySlot; }
        }
        public NodeAttribute NodeAttribute
        {
            get { return _nodeAttribute; }
        }

        public string Name
        {
            get
            {
                if (_isSlot)
                    return _propertySlot.Name;
                switch (_nodeAttribute)
                {
                    case NodeAttribute.Id:
                        return "NodeId";
                    default:
                        return _nodeAttribute.ToString();
                }
            }
        }
        public DataType DataType
        {
            get
            {
                if (_isSlot)
                    return _propertySlot.DataType;
                switch (_nodeAttribute)
                {
                    case NodeAttribute.Name:
                    case NodeAttribute.Path:
                    case NodeAttribute.ETag:
                    case NodeAttribute.LockToken:
                        return DataType.String;
                    case NodeAttribute.Id:
                    case NodeAttribute.IsDeleted:
                    case NodeAttribute.IsInherited:
                    case NodeAttribute.Index:
                    case NodeAttribute.Locked:
                    case NodeAttribute.LockTimeout:
                    case NodeAttribute.LockType:
                    case NodeAttribute.MajorVersion:
                    case NodeAttribute.MinorVersion:
                    case NodeAttribute.FullTextRank:
                    case NodeAttribute.ParentId:
                    case NodeAttribute.Parent:
                    case NodeAttribute.LockedById:
                    case NodeAttribute.CreatedById:
                    case NodeAttribute.ModifiedById:
                    case NodeAttribute.LastMinorVersionId:
                    case NodeAttribute.LastMajorVersionId:
                    case NodeAttribute.LockedBy:
                    case NodeAttribute.CreatedBy:
                    case NodeAttribute.ModifiedBy:
                    case NodeAttribute.IsSystem:
                    case NodeAttribute.OwnerId:
                    case NodeAttribute.SavingState:
                        return DataType.Int;
                    case NodeAttribute.LockDate:
                    case NodeAttribute.LastLockUpdate:
                    case NodeAttribute.CreationDate:
                    case NodeAttribute.ModificationDate:
                        return DataType.DateTime;
                    default:
                        throw new SnNotSupportedException();
                }
            }
        }

        public PropertyLiteral()
        {
        }
        public PropertyLiteral(NodeAttribute nodeAttribute)
        {
            _nodeAttribute = nodeAttribute;
        }
        public PropertyLiteral(PropertyType propertySlot)
        {
            _isSlot = true;
            _propertySlot = propertySlot;
        }

        internal virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            if (_isSlot)
                writer.WriteAttributeString("property", _propertySlot.Name);
            else
                writer.WriteAttributeString("property", _nodeAttribute.ToString());
        }
    }
}