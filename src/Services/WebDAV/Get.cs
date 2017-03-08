using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.WebDav
{
    public static class NodeConverter
    {
        public static void TransformContentType(Node node, HttpContext context, object extension)
        {
            Stream outputStream = context.Response.OutputStream;
            var content = Content.Load(node.Path);
            var ct = content.ContentHandler as ContentType;

            using (var sw = new StreamWriter(outputStream))
            {
                if (ct != null) sw.Write(ct.ToXml());
            }
        }

        public static void TransformContent(Node node, HttpContext context, object extension)
        {
            var outputStream = context.Response.OutputStream;
            var content = Content.Load(node.Path);
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, IndentChars = "  " };
            XmlWriter writer = null;

            try
            {
                writer = XmlWriter.Create(outputStream, settings);
                writer.WriteStartDocument();
                writer.WriteStartElement("ContentMetaData");
                writer.WriteElementString("ContentType", content.ContentType.Name);
                writer.WriteElementString("ContentName", content.Name);
                writer.WriteStartElement("Fields");

                var expContext = new ExportContext(content.Path, "");
                content.ExportFieldData(writer, expContext);

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        public static void TransformFile(Node node, HttpContext context, object extension)
        {
            var binaryPropertyName = extension.ToString();
            string contentType;
            BinaryFileName fileName;

            var binaryStream = DocumentBinaryProvider.Current.GetStream(node, binaryPropertyName, out contentType, out fileName);

            context.Response.ContentType = contentType;
            context.Response.AddHeader("Content-Length", ((BinaryData)node[binaryPropertyName]).Size.ToString());
            context.Response.BufferOutput = true;
            context.Response.Flush();

            if (binaryStream != null)
                binaryStream.CopyTo(context.Response.OutputStream);

            // let the client code log file downloads
            var file = node as ContentRepository.File;
            if (file != null)
                ContentRepository.File.Downloaded(file.Id);
        }
    }

    public static class ConverterPattern
    {
        public static Dictionary<Type, Action<Node, HttpContext, object>> TransformerRules =
            new Dictionary<Type, Action<Node, HttpContext, object>>();

        public static Action<Node, HttpContext, object> DefaultTransformer;

        static ConverterPattern()
        {
            TransformerRules.Add(typeof(IFile), NodeConverter.TransformFile);
            TransformerRules.Add(typeof(ContentType), NodeConverter.TransformContentType);
            TransformerRules.Add(typeof(Node), NodeConverter.TransformContent);
        }

        public static void DoTransform(Node node, WebDavHandler handler, string binaryPropertyName)
        {
            var content = Content.Load(node.Path);
            var nodeType = content.ContentHandler.GetType();

            var transformer = TransformerRules.Where(rule => nodeType.IsSubclassOf(rule.Key) || 
                                                                nodeType == rule.Key).Select(rule => rule.Value).FirstOrDefault();

            if (node is IFile)
            {
                switch (handler.WebdavType)
                {
                    case WebdavType.Page:
                        transformer = NodeConverter.TransformFile;
                        break;
                    case WebdavType.Content:
                        transformer = NodeConverter.TransformContent;
                        break;
                    case WebdavType.ContentType:
                        transformer = NodeConverter.TransformContentType;
                        break;
                    default:
                        transformer = NodeConverter.TransformFile;
                        break;
                }
            }

            if (transformer != null)
            {
                transformer(node, handler.Context, binaryPropertyName);
            }
        }
    }

    /// <summary>
    /// Summary description for Get.
    /// </summary>
    public class Get : IHttpMethod
    {
        private WebDavHandler _handler;
        
        public Get(WebDavHandler handler)
        {
            _handler = handler;
        }
        
        #region IHttpMethod Members

        public void HandleMethod()
        {
            var node = Node.LoadNode(_handler.GlobalPath);
            var binaryPropertyName = "Binary";
            
            if (node == null)
            {
                var parentPath = RepositoryPath.GetParentPath(_handler.GlobalPath);
                var currentName = RepositoryPath.GetFileName(_handler.GlobalPath);

                node = Node.LoadNode(parentPath);

                var foundNode = WebDavHandler.GetNodeByBinaryName(node, currentName, out binaryPropertyName);

                if (foundNode != null)
                {
                    node = foundNode;
                }
                else
                {
                    binaryPropertyName = "Binary";

                    // check if parent is contenttype 
                    // (contenttypes are listed under their own folder, so the node exists only virtually)
                    var parentIsContentType = (node != null && node is ContentType);

                    if (!parentIsContentType)
                    {
                        _handler.Context.Response.StatusCode = 404;
                        _handler.Context.Response.Flush();
                    }

                    // parent is contenttype, continue operation on parent (foldernode's name is valid CTD name)
                }
            }

            // load specific version
            if (node != null && !string.IsNullOrEmpty(PortalContext.Current.VersionRequest))
            {
                VersionNumber version;
                if (VersionNumber.TryParse(PortalContext.Current.VersionRequest, out version))
                {
                    var nodeVersion = Node.LoadNode(node.Id, version);
                    if (nodeVersion != null && nodeVersion.SavingState == ContentSavingState.Finalized)
                        node = nodeVersion;
                }
            }

            ConverterPattern.DoTransform(node, _handler, binaryPropertyName);
            _handler.Context.Response.Flush();
            
            return;
        }

        #endregion
    }
}
