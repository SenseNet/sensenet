using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Services.WebDav
{
    public class Delete : IHttpMethod
    {
        private WebDavHandler _handler;
        public Delete(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            var node = Node.LoadNode(_handler.GlobalPath);
            System.Xml.XmlTextWriter writer;

            if (node == null)
            {
                _handler.Context.Response.StatusCode = 404;
                return;
            }

            try
            {
                node.Delete();

                _handler.Context.Response.StatusCode = 204;
                return;
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                _handler.Context.Response.StatusCode = 207;
                writer = GetLockedErrorResponse(_handler.Path);
            }

            if (writer != null)
            {
                writer.Flush();
                writer.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
                var reader = new System.IO.StreamReader(writer.BaseStream, System.Text.Encoding.UTF8);
                _handler.Context.Response.Write(reader.ReadToEnd());
            }
        }

        private System.Xml.XmlTextWriter GetLockedErrorResponse(string path)
        {
            System.Xml.XmlTextWriter writer = Common.GetXmlWriter();
            writer.WriteStartElement(XmlNS.DAV_Prefix, "multistatus", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "response", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "href", XmlNS.DAV);
            writer.WriteString(_handler.RepositoryPathToUrl(path));
            writer.WriteEndElement(); // multistatus/response/href
            writer.WriteStartElement(XmlNS.DAV_Prefix, "status", XmlNS.DAV);
            writer.WriteString("HTTP/1.1 423 Locked");
            writer.WriteEndElement(); // multistatus/response/status
            writer.WriteEndElement(); // multistatus/response
            writer.WriteEndElement(); // multistatus
            return writer;
        }
        private System.Xml.XmlTextWriter GetAccessDenyErrorResponse(string path)
        {
            System.Xml.XmlTextWriter writer = Common.GetXmlWriter();
            writer.WriteStartElement(XmlNS.DAV_Prefix, "multistatus", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "response", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "href", XmlNS.DAV);
            writer.WriteString(_handler.RepositoryPathToUrl(path));
            writer.WriteEndElement(); // multistatus/response/href
            writer.WriteStartElement(XmlNS.DAV_Prefix, "status", XmlNS.DAV);
            writer.WriteString("HTTP/1.1 403 Forbidden");
            writer.WriteEndElement(); // multistatus/response/status
            writer.WriteEndElement(); // multistatus/response
            writer.WriteEndElement(); // multistatus
            return writer;
        }
        #endregion
    }

}
