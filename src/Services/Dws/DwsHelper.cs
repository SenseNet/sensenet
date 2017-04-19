using System;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.Dws
{
    public class DwsHelper
    {
        /// <summary>
        /// Method to convert a custom Object to XML string
        /// </summary>
        /// <param name="pObject">Object that is to be serialized to XML</param>
        /// <returns>XML string</returns>
        public static string SerializeObject(object pObject)
        {
            try
            {
                var memoryStream = new MemoryStream();
                var xs = new XmlSerializer(pObject.GetType());
                var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

                xs.Serialize(xmlTextWriter, pObject);
                memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
                var XmlizedString = UTF8ByteArrayToString(memoryStream.ToArray());
                return XmlizedString;
            }
            catch (Exception e)
            {
                SnLog.WriteException(e);
                return null;
            }
        }

        /// <summary>
        /// Method to reconstruct an Object from XML string
        /// </summary>
        /// <param name="pXmlizedString"></param>
        /// <returns></returns>
        public static object DeserializeObject(string pXmlizedString, Type pObjectType)
        {
            var xs = new XmlSerializer(pObjectType);
            var memoryStream = new MemoryStream(StringToUTF8ByteArray(pXmlizedString));

            return xs.Deserialize(memoryStream);
        }

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        private static string UTF8ByteArrayToString(byte[] characters)
        {
            var encoding = new UTF8Encoding();
            return encoding.GetString(characters);
        }

        /// <summary>
        /// Converts the String to UTF8 Byte array and is used in De serialization
        /// </summary>
        /// <param name="pXmlString"></param>
        /// <returns></returns>
        private static byte[] StringToUTF8ByteArray(string pXmlString)
        {
            var encoding = new UTF8Encoding();
            return encoding.GetBytes(pXmlString);
        }

        /// <summary>
        /// Gets repository path from host-prefixed content url address
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <returns></returns>
        public static string GetPathFromUrl(string pageUrl)
        {
            pageUrl = HttpUtility.UrlDecode(pageUrl) ?? string.Empty;
            
            var hostIdx = pageUrl.IndexOf(HttpContext.Current.Request.Url.Host, StringComparison.InvariantCulture);
            if (hostIdx < 0)
                return pageUrl;

            var prefixLength = hostIdx + HttpContext.Current.Request.Url.Host.Length;
            var path = pageUrl.Substring(prefixLength);

            return GetFullPath(path);
        }

        public static string GetFullPath(string partialPath)
        {
            // remove trailing slash
            partialPath = partialPath.TrimEnd('/');

            if (partialPath.StartsWith("/Root/", StringComparison.OrdinalIgnoreCase))
                return partialPath;

            string absPath1;

            if (PortalContext.Current.Site == null)
            {
                SnTrace.Web.Write($"WebDav: current site is unknown, cannot convert partial path '{partialPath}' to a site-relative path.");
                absPath1 = partialPath;
            }
            else
            {
                absPath1 = RepositoryPath.Combine(PortalContext.Current.Site.Path, partialPath);
                if (Node.Exists(absPath1))
                    return absPath1;
            }

            var absPath2 = RepositoryPath.Combine("/Root", partialPath);
            if (Node.Exists(absPath2))
                return absPath2;

            return absPath1;
        }

        /// <summary>
        /// Gets host string ie. http://localhost
        /// </summary>
        /// <returns></returns>
        public static string GetHostStr()
        {
            var url = HttpContext.Current.Request.Url.ToString();
            var hostIdx = url.IndexOf(HttpContext.Current.Request.Url.Host);
            var prefixLength = hostIdx + HttpContext.Current.Request.Url.Host.Length;
            return url.Substring(0, prefixLength);
        }

        /// <summary>
        /// Checks if current authenticated user is visitor, and returns with 401. True if user is Visitor.
        /// </summary>
        /// <returns></returns>
        public static bool CheckVisitor()
        {
            if (!SenseNet.ContentRepository.User.Current.IsAuthenticated)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                if (PortalContext.Current.AuthenticationMode == "Windows")
                    AuthenticationHelper.DenyAccess(HttpContext.Current.ApplicationInstance);
                else
                    AuthenticationHelper.ForceBasicAuthentication(HttpContext.Current);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the first ancestor of type "Document Library" for the given Node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Node GetDocumentLibraryForNode(Node node)
        {
            return Node.GetAncestorOfNodeType(node, "DocumentLibrary");
        }
    }
}
