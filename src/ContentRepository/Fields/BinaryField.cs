using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using IO = System.IO;
using SenseNet.ContentRepository.Storage;

using SenseNet.ContentRepository.Schema;
using System.Web;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("Binary")]
    [DataSlot(0, RepositoryDataType.Binary, typeof(BinaryData))]
    [DefaultFieldSetting(typeof(BinaryFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.Binary")]
    public class BinaryField : Field
    {
        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(XmlWriter writer, ExportContext context)
        {
            // <Binary attachment="Home.aspx" />
            BinaryData binaryData = this.GetData() as BinaryData;

            var attName = WriteXmlDataPrivate(writer);

            var fsDirectory = context.CurrentDirectory;
            if (!String.IsNullOrEmpty(fsDirectory) && Directory.Exists(fsDirectory))
            {
                Stream source = null;
                FileStream target = null;
                var fsPath = Path.Combine(fsDirectory, attName);
                try
                {
                    source = binaryData.GetStream();
                    target = new FileStream(fsPath, FileMode.Create);
                    if (source != null)
                        for (var i = 0; i < source.Length; i++)
                            target.WriteByte((byte)source.ReadByte());
                }
                finally
                {
                    if (source != null)
                        source.Close();
                    if (target != null)
                    {
                        target.Flush();
                        target.Close();
                    }
                }
            }
        }
        protected override void WriteXmlData(XmlWriter writer)
        {
            WriteXmlDataPrivate(writer);
        }
        private string WriteXmlDataPrivate(XmlWriter writer)
        {
            // <Binary attachment="Home.aspx" />

            var attName = AttachmentName();
            writer.WriteAttributeString("attachment", attName);

            return attName;
        }
        public string AttachmentName()
        {
            BinaryData binaryData = this.GetData() as BinaryData;

            var ext = Path.GetExtension(binaryData.FileName);
            var contentName = this.Content.Name;
            if (!(this.Content.ContentHandler is IFolder))
                if (ext == Path.GetExtension(this.Content.Name))
                    ext = "";

            var fieldExtName = String.Empty;
            var expectedFieldExtName = "." + this.Name;
            if (this.Name != "Binary")
            {
                if (ext.ToLower() != expectedFieldExtName.ToLower())
                    fieldExtName = expectedFieldExtName;
            }

            var attName = String.Concat(contentName, fieldExtName, ext);
            return attName;
        }

        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            XmlAttribute attachmentAttr = fieldNode.Attributes["attachment"];

            BinaryData binaryData = this.GetData() as BinaryData;
            if (binaryData == null) binaryData = new BinaryData();

            if (attachmentAttr == null)
            {
                binaryData.SetStream(RepositoryTools.GetStreamFromString(fieldNode.InnerXml));
                binaryData.FileName = String.Concat(this.Content.ContentHandler.Name, ".", this.Name, ".txt");
            }
            else
            {
                string path = context.UnescapeFileName(attachmentAttr.Value);
                binaryData.FileName = Path.Combine(context.CurrentDirectory, path);

                // webdav request: we don't have a binary file 
                // next to the content as an attachment, so ignore the value
                var trans = Providers.Instance.CompatibilitySupport.GetRequestHeader("Translate");
                var webdav = (!string.IsNullOrEmpty(trans) && trans.ToLower().CompareTo("f") == 0);


                if (!webdav)
                {
                    var stream = context.GetAttachmentStream(path);
                    if (stream == null)
                        return;

                    binaryData.SetStream(stream);
                }
            }

            this.SetData(binaryData);
        }
        protected override void ImportData(XmlNode fieldNode)
        {
            XmlAttribute attachmentAttr = fieldNode.Attributes["attachment"];

            BinaryData binaryData = this.GetData() as BinaryData;
            if (binaryData == null) binaryData = new BinaryData();

            if (attachmentAttr == null)
            {
                binaryData.SetStream(RepositoryTools.GetStreamFromString(fieldNode.InnerXml));
                binaryData.FileName = String.Concat(this.Content.ContentHandler.Name, ".", this.Name, ".txt");
            }
            else
            {
                binaryData.FileName = attachmentAttr.Value.Replace("$amp;", "&");

                var stream = Stream.Null;
                if (stream == null)
                    return;

                binaryData.SetStream(stream);
            }

            this.SetData(binaryData);
        }

        protected override void ExportData2(XmlWriter writer, ExportContext context)
        {
            // <Binary attachment="Home.aspx" />
            BinaryData binaryData = this.GetData() as BinaryData;

            var ext = Path.GetExtension(binaryData.FileName);
            var name1 = this.Content.Name;
            if (ext == Path.GetExtension(this.Content.Name))
                ext = "";
            var name2 = this.Name == "Binary" ? "" : "." + this.Name;
            var attName = String.Concat(name1, name2, ext);
            var fsPath = Path.Combine(context.CurrentDirectory, attName);

            writer.WriteAttributeString("attachment", attName);
        }

        protected override bool ParseValue(string value)
        {
            var binaryData = new BinaryData();
            binaryData.SetStream(RepositoryTools.GetStreamFromString(value));
            this.SetData(binaryData);
            return true;
        }

        // ============================================================================= Static API

        public static string GetBinaryUrl(int contentId, string propertyName, long checksum = 0)
        {
            // Although this is a url that should not be constructed in this layer,
            // there are several features that need it.
            return checksum > 0
                ? $"/binaryhandler.ashx?nodeid={contentId}&propertyname={HttpUtility.UrlEncode(propertyName)}&checksum={HttpUtility.UrlEncode(checksum.ToString())}"
                : $"/binaryhandler.ashx?nodeid={contentId}&propertyname={HttpUtility.UrlEncode(propertyName)}";
        }
    }
}
