using System;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage.Search
{
    public class TypeExpression : Expression
    {
        private NodeType _nodeType;
        private bool _exactMatch;

        public NodeType NodeType
        {
            get { return _nodeType; }
        }
        public bool ExactMatch
        {
            get { return _exactMatch; }
        }

        public TypeExpression(NodeType nodeType)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            _nodeType = nodeType;
        }
        public TypeExpression(NodeType nodeType, bool exactMatch)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");
            _nodeType = nodeType;
            _exactMatch = exactMatch;
        }

        internal override void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("Type", NodeQuery.XmlNamespace);
            writer.WriteAttributeString("nodeType", _nodeType.Name);
            if (_exactMatch)
                writer.WriteAttributeString("exactMatch", "yes");
            writer.WriteEndElement();
        }
    }

}