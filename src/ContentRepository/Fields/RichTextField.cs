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
    [DataSlot(0, RepositoryDataType.Text, typeof(string))]
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
            foreach (XmlElement childElement in fieldNode.SelectNodes("*"))
            {
                switch (childElement.LocalName)
                {
                    case "Text": data.Text = childElement.InnerText; break;
                    case "Editor": data.Editor = childElement.InnerText; break;
                }
            }

            this.SetData(data);
        }

        protected override object ConvertTo(object[] handlerValues)
        {
            var src = (string)handlerValues[0];
            if (string.IsNullOrEmpty(src))
                return null;

            RichTextFieldValue data;
            try
            {
                data = JsonConvert.DeserializeObject<RichTextFieldValue>(src);
            }
            catch (Exception e) // rethrow
            {
                throw new ApplicationException(string.Concat("Invalid RichText data: ", src, ". Content: ", this.Content.Path, ", Field: ", this.Name), e);
            }

            return data;
        }
        protected override object[] ConvertFrom(object value)
        {
            string serialized = null;
            if (value is JObject jObject)
            {
                // check
                var data = JsonConvert.DeserializeObject<RichTextFieldValue>(jObject.ToString());
                // convert
                serialized = jObject.ToString();
            }
            else if (value is RichTextFieldValue rtf)
            {
                serialized = EncodeTransferData(rtf);
            }
            return new object[] { serialized };
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
