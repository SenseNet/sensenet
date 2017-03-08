using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using System.Xml;
using SenseNet.ContentRepository.Storage.Search;

using  SenseNet.ContentRepository.Schema;
using System.Collections;

namespace SenseNet.ContentRepository.Fields
{
    [Obsolete("Do not use this field. Directly accessing the fields containing the user and date information is preferred. (eg. CreatedBy and CreationDate)")]
	[ShortName("WhoAndWhen")]
	[DataSlot(0, RepositoryDataType.Reference, typeof(Node), typeof(User), typeof(IEnumerable))]
	[DataSlot(1, RepositoryDataType.DateTime, typeof(DateTime))]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.WhoAndWhen")]
    [FieldDataType(typeof(WhoAndWhenData))]
	public class WhoAndWhenField : Field
	{
		public class WhoAndWhenData
		{
			public WhoAndWhenData() { }
			public WhoAndWhenData(User who, DateTime when)
			{
				_who = who;
				_when = when;
			}
			private User _who;
			private DateTime _when;

			public User Who
			{
				get { return _who; }
				set { _who = value; }
			}
			public DateTime When
			{
				get { return _when; }
				set { _when = value; }
			}
		}
		protected override bool HasExportData { get { return false; } }
        protected override void WriteXmlData(XmlWriter writer)
        {
            var data = (WhoAndWhenData)GetData();
            writer.WriteStartElement("Who");
            writer.WriteString(data.Who == null ? string.Empty : data.Who.Path);
            writer.WriteEndElement();
            writer.WriteStartElement("When");
            writer.WriteString(XmlConvert.ToString(data.When, XmlDateTimeSerializationMode.Unspecified));
            writer.WriteEndElement();
        }
		protected override void ImportData(XmlNode fieldNode, ImportContext context)
		{
            throw new SnNotSupportedException("The ImportData operation is not supported on WhoAndWhenData.");
		}
		protected override object[] ConvertFrom(object value)
		{
			WhoAndWhenData data = value as WhoAndWhenData;
            if (data == null)
                throw new NotSupportedException("Field value is null or not a WhoAndWhenData. FieldName: " + this.Name);
            object[] result = new object[2];

            var nodeValue = data.Who as Node;
            var enumerableValue = data.Who as IEnumerable;

            Type propertyType = this.GetHandlerSlot(0);
			if (typeof(Node).IsAssignableFrom(propertyType))
			{
                if (enumerableValue != null)
                {
                    foreach (Node node in enumerableValue)
                    {
                        result[0] = node;
                        break;
                    }
                }
                else
                {
				    result[0] = data.Who;
                }
			}
            else if (typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                var refs = new List<Node>();
                if (enumerableValue != null)
                {
                    foreach (Node node in enumerableValue)
                    {
                        if (node != null)
                        {
                            refs.Add(node);
                            break;
                        }
                    }
                }
                else
                {
                    if(nodeValue != null)
                        refs.Add(nodeValue);
                }
                result[0] = refs;
            }
            else
            {
                throw new NotSupportedException(String.Concat(data.Who.GetType().FullName ," is not assignable to Who part of WhoAndWhen field. FieldName: ", this.Name));
            }

			result[1] = data.When;

			return result;
		}
		protected override object ConvertTo(object[] handlerValues)
		{
			WhoAndWhenData result = new WhoAndWhenData();
			Type propertyType = this.GetHandlerSlot(0);
			if (propertyType.IsAssignableFrom(typeof(User)))
			{
				result.Who = (User)handlerValues[0];
			}
			else if (typeof(IEnumerable).IsAssignableFrom(propertyType))
			{
                var enumerableValue = handlerValues[0] as IEnumerable;
                foreach (User item in enumerableValue)
                {
                    result.Who = item;
                    break;
                }
			}

			if (handlerValues[1] != null)
				result.When = (DateTime)handlerValues[1];
			else
				result.When = ActiveSchema.DateTimeMinValue;

			return result;
		}
	}
}