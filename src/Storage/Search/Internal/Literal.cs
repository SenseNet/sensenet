using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using System.Xml;

namespace SenseNet.ContentRepository.Storage.Search.Internal
{
	public class Literal : PropertyLiteral
	{
		private bool _isValue;
		private object _value;

		public bool IsValue
		{
			get { return _isValue; }
		}
		public object Value
		{
			get { return _value; }
		}

		public Literal(object value) : base()
		{
			_isValue = true;
			_value = value;
		}
		public Literal(NodeAttribute nodeAttribute) : base(nodeAttribute) { }
		public Literal(PropertyType propertySlot) : base(propertySlot) { }

		internal override void WriteXml(XmlWriter writer)
		{
			if (base.IsSlot)
			{
                writer.WriteStartElement("Property", NodeQuery.XmlNamespace);
				writer.WriteAttributeString("name", base.PropertySlot.Name);
				writer.WriteEndElement();
			}
			else if (!IsValue)
			{
                writer.WriteStartElement("Property", NodeQuery.XmlNamespace);
				writer.WriteAttributeString("name", base.NodeAttribute.ToString());
				writer.WriteEndElement();
			}
			else
			{
				if (_value == null)
                    writer.WriteElementString("NullValue", NodeQuery.XmlNamespace, null);
				else if (_value is string)
					writer.WriteString((string)_value);
				else if (_value is int)
					writer.WriteString(XmlConvert.ToString((int)_value));
				else if (_value is decimal)
					writer.WriteString(XmlConvert.ToString((decimal)_value));
				if (_value is DateTime)
					writer.WriteString(XmlConvert.ToString((DateTime)_value, XmlDateTimeSerializationMode.Unspecified));
			}

		}
	}
}