using System;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
    public class RichTextFieldValue
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("editor")]
        public string Editor { get; set; }
    }

    [ShortName("RichText")]
    [DataSlot(0, RepositoryDataType.Text, typeof(RichTextFieldValue), typeof(string))]
    [DefaultFieldSetting(typeof(RichTextFieldSetting))]
    [FieldDataType(typeof(RichTextFieldValue))]
    public class RichTextField : Field
    {
        protected override bool HasExportData => GetData() != null;
        protected override void ExportData(XmlWriter writer, ExportContext context)
        {
            var value = (RichTextFieldValue) GetData(false);

            if (value.Text == null && value.Editor == null)
            {
                //writer.WriteStartElement("Text");
                //writer.WriteCData(value.Text);
                //writer.WriteEndElement();
                //writer.WriteStartElement("Editor");
                //writer.WriteCData(value.Editor);
                //writer.WriteEndElement();
            }
            else if (value.Text == null && value.Editor != null)
            {
                writer.WriteStartElement("Editor");
                writer.WriteCData(value.Editor);
                writer.WriteEndElement();
            }
            else if (value.Text != null && value.Editor == null)
            {
                writer.WriteCData(value.Text);
            }
            else
            {
                writer.WriteStartElement("Text");
                writer.WriteCData(value.Text);
                writer.WriteEndElement();
                writer.WriteStartElement("Editor");
                writer.WriteCData(value.Editor);
                writer.WriteEndElement();
            }
        }
        protected override void WriteXmlData(XmlWriter writer)
        {
            ExportData(writer, null);
        }

        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            if (string.IsNullOrEmpty(fieldNode.InnerXml))
            {
                this.SetData(null);
                return;
            }

            var data = new RichTextFieldValue();
            var childElements = fieldNode.SelectNodes("*");
            if (childElements != null)
            {
                // Backward compatibility: if there is no any child element, the node's text is the Text of the RichText.
                // Note: whitespace only or empty element interpreted as null.
                if (childElements.Count == 0)
                {
                    data.Text = fieldNode.InnerText;
                }
                else
                {
                    foreach (XmlElement childElement in childElements)
                    {
                        switch (childElement.LocalName)
                        {
                            case "Text": data.Text = childElement.InnerText; break;
                            case "Editor": data.Editor = childElement.InnerText; break;
                        }
                    }
                }
            }

            this.SetData(data);
        }

        protected override object ConvertTo(object[] handlerValues)
        {
            RichTextFieldValue data = null;

            Type propertyType = this.GetHandlerSlot(0);
            if (propertyType == typeof(string))
            {
                var src = (string)handlerValues[0];
                if (string.IsNullOrEmpty(src))
                    return null;
                try
                {
                    data = JsonConvert.DeserializeObject<RichTextFieldValue>(src);
                }
                catch (Exception e) // rethrow
                {
                    data = new RichTextFieldValue {Text = src};
                }
            }
            else if (propertyType == typeof(RichTextFieldValue))
            {
                data = (RichTextFieldValue)handlerValues[0];
            }

            return data;
        }
        protected override object[] ConvertFrom(object value)
        {
            // Accepts: string, JObject, RichTextFieldValue
            object converted = null;
            Type propertyType = this.GetHandlerSlot(0);
            if (propertyType == typeof(string))
            {
                if (value is string stringValue)
                {
                    converted = EncodeTransferData(new RichTextFieldValue { Text = stringValue });
                }
                if (value is JObject jObject)
                {
                    var src = jObject.ToString();
                    // check
                    var data = JsonConvert.DeserializeObject<RichTextFieldValue>(src);
                    // convert
                    converted = src;
                }
                else if (value is RichTextFieldValue rtf)
                {
                    converted = EncodeTransferData(rtf);
                }
            }
            else if (propertyType == typeof(RichTextFieldValue))
            {
                if (value is string stringValue)
                {
                    converted = new RichTextFieldValue { Text = stringValue };
                }
                if (value is JObject jObject)
                {
                    // check
                    converted = JsonConvert.DeserializeObject<RichTextFieldValue>(jObject.ToString());
                }
                else if (value is RichTextFieldValue rtf)
                {
                    converted = rtf;
                }
            }

            return new object[] { converted };
        }
        private string EncodeTransferData(RichTextFieldValue data)
        {
            return JsonConvert.SerializeObject(data);
        }

        protected override bool ParseValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            try
            {
                var hld = this.ConvertTo(new object[] { value });
                this.SetData(hld);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                return false;
            }

            return true;
        }


    }
}
