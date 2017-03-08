using System.Xml;

namespace SenseNet.Services.WebDav
{
    public class Proppatch : IHttpMethod
    {
        private WebDavHandler _handler;
        private XmlTextWriter _writer;
        public Proppatch(WebDavHandler handler)
        {
            _handler = handler;
        }

        // request:
        // <?xml version="1.0" encoding="utf-8" ?>
        // <D:propertyupdate xmlns:D="DAV:" xmlns:Z="urn:schemas-microsoft-com:">
        //     <D:set>
        //         <D:prop>
        //             <Z:Win32CreationTime>Mon, 11 Apr 2005 13:05:05 GMT</Z:Win32CreationTime>
        //             <Z:Win32LastAccessTime>Tue, 17 May 2005 13:47:40 GMT</Z:Win32LastAccessTime>
        //             <Z:Win32LastModifiedTime>Mon, 11 Apr 2005 13:05:05 GMT</Z:Win32LastModifiedTime>
        //             <Z:Win32FileAttributes>00000000</Z:Win32FileAttributes>
        //         </D:prop>
        //     </D:set>
        // </D:propertyupdate>

        // Erre a request-re egy nem valos valaszt adunk, mert a repository-ban maskep ertelmezettek ezek a dolgok
        public void HandleMethod()
        {
            _handler.Context.Response.StatusCode = 207;

            var xd = new XmlDocument();
            var xmlns = new XmlNamespaceManager(xd.NameTable);

            xmlns.AddNamespace("D", XmlNS.DAV);
            xmlns.AddNamespace("Z", XmlNS.Win32);
            xd.Load(_handler.Context.Request.InputStream);

            _writer = Common.GetXmlWriter();

            _writer.WriteStartElement(XmlNS.DAV_Prefix, "multistatus", XmlNS.DAV);
            _writer.WriteStartElement(XmlNS.DAV_Prefix, "response", XmlNS.DAV);
            _writer.WriteStartElement(XmlNS.DAV_Prefix, "href", XmlNS.DAV);
            _writer.WriteString(_handler.RepositoryPathToUrl(_handler.Path));
            _writer.WriteEndElement(); // href
            _writer.WriteStartElement(XmlNS.DAV_Prefix, "propstat", XmlNS.DAV);

            foreach (XmlNode xn in xd.SelectNodes("/D:propertyupdate/D:set/D:prop/Z:*", xmlns))
            {
                _writer.WriteStartElement(XmlNS.DAV_Prefix, "prop", XmlNS.DAV);
                _writer.WriteStartElement(XmlNS.Win32_Prefix, xn.LocalName, XmlNS.Win32);
                _writer.WriteString(xn.InnerText);
                _writer.WriteEndElement(); // xn.localname

                _writer.WriteStartElement(XmlNS.DAV_Prefix, "status", XmlNS.DAV);
                _writer.WriteString("HTTP/1.1 200 OK");
                _writer.WriteEndElement(); // status

                _writer.WriteEndElement(); // prop
            }

            _writer.WriteEndElement(); // propstat
            _writer.WriteEndElement(); // response
            _writer.WriteEndElement(); // multistatus

            _writer.Flush();
            _writer.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);

            var reader = new System.IO.StreamReader(_writer.BaseStream, System.Text.Encoding.UTF8);
            string ret = reader.ReadToEnd();

            _writer.Close();

            _handler.Context.Response.Write(ret);
            _handler.Context.Response.Flush();
        }
    }
}
