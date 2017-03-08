using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using System;
using System.Web;
using SenseNet.Configuration;

namespace SenseNet.Services.WebDav
{
    public class Lock : IHttpMethod
    {
        private WebDavHandler _handler;
        public Lock(WebDavHandler handler)
        {
            _handler = handler;
        }
        private static int GetRequestTimeout()
        {
            int timeout;
            var timeoutStr = "Second-604800";
            if (HttpContext.Current.Request.Headers["Timeout"] != null)
                timeoutStr = HttpContext.Current.Request.Headers["Timeout"];

            if (!int.TryParse(timeoutStr.Substring("Second-".Length), out timeout))
                timeout = 180;

            return timeout;
        }
        public void HandleMethod()
        {
            GenerateLockResponse(_handler.GlobalPath, _handler.Context);
        }
        public static void GenerateLockResponse(string path, HttpContext context) 
        {
            var timeout = GetRequestTimeout();

            context.Response.StatusCode = 200;

            var node = Node.LoadNode(path);
            if (node != null)
            {
                // if node is not checked out, it should not be opened for edit in word
                if (!node.Locked)
                {
                    if (Webdav.AutoCheckoutFiles)
                    {
                        var gc = node as GenericContent;
                        gc.CheckOut();
                        WriteSuccess(context, node, timeout);
                        return;
                    }
                    else
                    {
                        context.Response.AddHeader("X-MSDAVEXT_Error", "589839; The%20file%20%22%22%20is%20not%20checked%20out%2e");
                        context.Response.StatusCode = 403;
                        return;
                    }
                }
                // if node is checked out by someone else, it should not be opened for edit in word
                if (node.LockedById != User.Current.Id)
                {
                    context.Response.StatusCode = 403;
                    return;
                }
            }

            WriteSuccess(context, node, timeout);
        }
        private static void WriteSuccess(HttpContext context, Node node, int timeout)
        {
            var writer = GetSuccessResult(node, timeout);
            if (writer != null)
            {
                writer.Flush();
                writer.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);

                var reader = new System.IO.StreamReader(writer.BaseStream, System.Text.Encoding.UTF8);
                var ret = reader.ReadToEnd();

                context.Response.AddHeader("Content-Length", ret.Length.ToString());
                context.Response.Write(ret);
            }
        }
        private static System.Xml.XmlTextWriter GetSuccessResult(Node node, int timeout)
        {
            System.Xml.XmlTextWriter writer = Common.GetXmlWriter();
            writer.WriteStartElement(XmlNS.DAV_Prefix, "prop", XmlNS.DAV);
            Lock.WriteLockDiscovery(writer, node, timeout);
            writer.WriteEndElement(); // prop
            return writer;
        }
        internal static void WriteLockDiscovery(System.Xml.XmlTextWriter writer, Node node, int timeout)
        {
            // lockdiscovery
            // <D:activelock>
            //        <D:locktype><D:write/></D:locktype>
            //        <D:lockscope><D:exclusive/></D:lockscope>
            //        <D:depth>0</D:depth>
            //        <D:owner>James Smith</D:owner>
            //        <D:timeout>Infinite</D:timeout>
            //        <D:locktoken>
            //                <D:href>opaquelocktoken:f81de2ad-7f3d-a1b3-4f3c-00a0c91a9d76</D:href>
            //        </D:locktoken>
            // </D:activelock>
            writer.WriteStartElement(XmlNS.DAV_Prefix, "lockdiscovery", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "activelock", XmlNS.DAV);

            writer.WriteStartElement(XmlNS.DAV_Prefix, "locktype", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "write", XmlNS.DAV);
            writer.WriteEndElement(); // write
            writer.WriteEndElement(); // locktype
            writer.WriteStartElement(XmlNS.DAV_Prefix, "lockscope", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "exclusive", XmlNS.DAV);
            writer.WriteEndElement(); // exclusive
            writer.WriteEndElement(); // lockscope
            writer.WriteStartElement(XmlNS.DAV_Prefix, "depth", XmlNS.DAV);
            writer.WriteString("0");
            writer.WriteEndElement(); // depth
            writer.WriteStartElement(XmlNS.DAV_Prefix, "owner", XmlNS.DAV);
            if (node != null && node.LockedBy != null)
                writer.WriteString(node.LockedBy.Name);
            else
                writer.WriteString(User.Current.Name);
            writer.WriteEndElement(); // owner
            writer.WriteStartElement(XmlNS.DAV_Prefix, "timeout", XmlNS.DAV);
            writer.WriteString("Second-" + timeout);
            writer.WriteEndElement(); // timeout
            writer.WriteStartElement(XmlNS.DAV_Prefix, "locktoken", XmlNS.DAV);
            writer.WriteStartElement(XmlNS.DAV_Prefix, "href", XmlNS.DAV);
            writer.WriteString("opaquelocktoken:" + Guid.NewGuid().ToString());
            writer.WriteEndElement(); // href
            writer.WriteEndElement(); // locktoken

            writer.WriteEndElement(); // activelock
            writer.WriteEndElement(); // lockdiscovery
        }
    }
}
