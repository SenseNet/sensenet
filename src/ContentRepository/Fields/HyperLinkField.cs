using System;
using System.Collections.Generic;
using System.Text;
using  SenseNet.ContentRepository.Schema;

using System.Xml;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("HyperLink")]
	[DataSlot(0, RepositoryDataType.String, typeof(string))]
	[DefaultFieldSetting(typeof(HyperLinkFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.HyperLink")]
    [FieldDataType(typeof(HyperlinkData))]
    public class HyperLinkField : Field, SenseNet.ContentRepository.Xpath.IRawXmlContainer
	{
		public class HyperlinkData
		{
			private string _href;
			private string _text;
			private string _title;
			private string _target;

			public string Href
			{
				get { return _href; }
				set { _href = value; }
			}
			public string Text
			{
				get { return _text; }
				set { _text = value; }
			}
			public string Title
			{
				get { return _title; }
				set { _title = value; }
			}
			public string Target
			{
				get { return _target; }
				set { _target = value; }
			}

			public HyperlinkData() { }
			public HyperlinkData(string href, string text, string title, string target)
			{
				_href = href;
				_text = text;
				_title = title;
				_target = target;
			}
		}

		protected override bool HasExportData { get { return EncodeTransferData((HyperlinkData)GetData()) != null; } }
		protected override void ExportData(XmlWriter writer, ExportContext context)
		{
            writer.WriteRaw(GetRawXml());
		}
        protected override void WriteXmlData(XmlWriter writer)
        {
            ExportData(writer, null);
        }
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
			if (String.IsNullOrEmpty(fieldNode.InnerXml))
			{
				this.SetData(null);
				return;
			}
			this.SetData(ConvertTo(new object[] { fieldNode.InnerXml }));
		}

        public string GetRawXml() // IRawXmlContainer Member
        {
            return EncodeTransferData((HyperlinkData)GetData());
        }

		protected override object ConvertTo(object[] handlerValues)
		{
			var data = new HyperlinkData();
			var src = (string)handlerValues[0];

			if (!String.IsNullOrEmpty(src))
			{
				try
				{
				    var escapeError = false;
					XmlAttribute attr;
					var xd = new XmlDocument();

                    try
                    {
                        xd.LoadXml(src);
                    }
                    catch(Exception e) // logged
                    {
                        SnLog.WriteException(e);

                        escapeError = true;
                    }

                    if (escapeError)
                    {
                        try
                        {
                            // robustness: load old not-replace-data
                            var escaped = src.Replace("&amp;", "&").Replace("&", "&amp;");

                            // this will throw an exception if something is still wrong...
                            xd.LoadXml(escaped);
                        }
                        catch (Exception)
                        {
                            // something is seriously wrong with the hyperlink data
                            return data;
                        }
                    }

                    if (xd.DocumentElement == null)
                        return data;

					data.Text = xd.DocumentElement.InnerXml;
					data.Text = data.Text.Length == 0 ? null : ReplaceSpecCharBack(data.Text);

					attr = xd.DocumentElement.Attributes["href"];
					data.Href = attr == null ? null : attr.Value;
					attr = xd.DocumentElement.Attributes["title"];
                    data.Title = attr == null ? null : ReplaceSpecCharBack(attr.Value);
					attr = xd.DocumentElement.Attributes["target"];
					data.Target = attr == null ? null : attr.Value;
				}
				catch(Exception e) // rethrow
				{
					throw new ApplicationException(String.Concat("Invalid hyperlink data: ", src, ". Content: ", this.Content.Path, ", Field: ", this.Name), e);
				}
			}
			return data;
		}
		protected override object[] ConvertFrom(object value)
		{
			var data = value as HyperlinkData;
			return new object[] { EncodeTransferData(data) };
		}
		private string EncodeTransferData(HyperlinkData data)
		{
			if (data == null)
				return null;

			data.Href = ReplaceSpecChar(data.Href);
			data.Title = ReplaceSpecChar(data.Title);
			data.Target = ReplaceSpecChar(data.Target);
			data.Text = ReplaceSpecChar(data.Text);

			var sb = new StringBuilder();
			sb.Append("<a");
			if (data.Href != null)
				sb.Append(" href=\"").Append(data.Href).Append("\"");
			if (data.Target != null)
				sb.Append(" target=\"").Append(data.Target).Append("\"");
			if (data.Title != null)
				sb.Append(" title=\"").Append(data.Title).Append("\"");
			sb.Append(">");
			sb.Append(data.Text ?? "");
			sb.Append("</a>");
			return sb.ToString();
		}

		private string ReplaceSpecChar(string text)
		{
			if (String.IsNullOrEmpty(text))
				return text;
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;").Replace("\"", "&quot;");
		}

		private string ReplaceSpecCharBack(string text)
		{
			if (String.IsNullOrEmpty(text))
				return text;
            return text.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&#39;", "'").Replace("&quot;", "\"");
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