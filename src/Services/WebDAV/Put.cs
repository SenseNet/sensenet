using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal;
using SenseNet.Portal.Handlers;
using SenseNet.Portal.WebDAV;
using SN = SenseNet.ContentRepository;
using SenseNet.Diagnostics;

namespace SenseNet.Services.WebDav
{
    public class Put : IHttpMethod
    {
        private WebDavHandler _handler;
        private const string FILEPROPERTYNAME = "Binary";

        public Put(WebDavHandler handler)
        {
            _handler = handler;
        }

        private void SetBinaryStream(Node node, Stream fileStream)
        {
            SetBinaryStream(node, fileStream, FILEPROPERTYNAME);
        }

        private void SetBinaryStream(Node node, Stream fileStream, string propertyName)
        {
            SetBinaryStream(node, fileStream, propertyName, null);
        }

        private void SetBinaryStream(Node node, Stream fileStream, string propertyName, string binaryName)
        {
            using (Stream inputStream = fileStream)
            {
                inputStream.Seek(0, SeekOrigin.Begin);

                // check if msdavext-proppatch header is given
                if (_handler.Context.Request.Headers["X-MSDAVEXT"] == "PROPPATCH")
                {
                    using (var reader = new System.IO.StreamReader(inputStream))
                    {
                        // 16 chars: size of properties
                        //  x chars: properties
                        // 16 chars: file size
                        char[] buf = new char[16];
                        reader.Read(buf, 0, 16);
                        var hexValue = string.Join("", buf);
                        int decValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

                        // let's skip to start of actual document stream
                        using (var resultStream = new OffsetStream(inputStream, 32 + decValue))
                        {
                            resultStream.Position = 0;
                            SetBinaryStreamStripped(node, resultStream, propertyName, binaryName);
                        }
                    }
                }
                else
                {
                    SetBinaryStreamStripped(node, inputStream, propertyName, binaryName);
                }
            }
        }

        private void SetBinaryStreamStripped(Node node, Stream inputStream, string propertyName, string binaryName)
        {
            var bdata = (BinaryData)node[propertyName];
            bdata.SetStream(inputStream);

            if (!string.IsNullOrEmpty(binaryName))
                bdata.FileName = binaryName;

            node[propertyName] = bdata;
            node.Save();
        }

        /// <summary>
        /// Checks an existing node whether it has a binary property with 
        /// matching name and sets the binary value if needed
        /// </summary>
        /// <returns>True, if binary value was set</returns>
        private bool SetBinaryAttachment(Node existingNode, string binaryName)
        {
            bool foundBin = false;

            foreach (var propType in existingNode.PropertyTypes)
            {
                //TODO: BETTER FILENAME CHECK!
                if (propType.DataType == DataType.Binary)
                {
                    string tempBinaryName = WebDavHandler.GetAttachmentName(existingNode, propType.Name);

                    if (tempBinaryName.CompareTo(binaryName) == 0)
                    {
                        SetBinaryStream(existingNode, _handler.Context.Request.InputStream, propType.Name);
                        foundBin = true;
                        break;
                    }
                }
            }

            return foundBin;
        }

        // ===========================================================================================================

        public void HandleMethod()
        {
            var parentPath = RepositoryPath.GetParentPath(_handler.GlobalPath);
            var fileName = RepositoryPath.GetFileName(_handler.GlobalPath);
            var parentNode = Node.LoadNode(parentPath);
            var node = Node.LoadNode(_handler.GlobalPath);

            switch (_handler.WebdavType)
            {
                case WebdavType.File:
                case WebdavType.Folder:
                case WebdavType.Page:
                    if (node == null || node is IFile)
                        HandleFile(fileName, parentNode, node);
                    return;
                case WebdavType.Content:
                    HandleContent(parentPath, fileName, parentNode, node);
                    return;
                case WebdavType.ContentType:
                    InstallContentType();
                    return;
                default:
                    throw new SnNotSupportedException("Unknown WebdavType" + _handler.WebdavType);
            }
        }

        private void HandleFile(string fileName, Node parentNode, Node node)
        {
            if (_handler.Context.Request.InputStream.Length == 0)
            {
                // handle empty files - but only create the file for the 
                // _second_ empty request that contains the "If" header
                if (node == null && _handler.Context.Request.Headers.AllKeys.Contains("If"))
                {
                    CreateFile(fileName, parentNode);
                    return;
                }

                _handler.Context.Response.StatusCode = 200;
                _handler.Context.Response.AddHeader("X-MSDAVEXTLockTimeout", "Second-3600");
                _handler.Context.Response.AddHeader("Lock-Token", "<opaquelocktoken:" + Guid.NewGuid().ToString() + ">");
                _handler.Context.Response.AddHeader("Content-Length", "0");
                _handler.Context.Response.Flush();
                return;
            }

            var file = node as IFile;
            if (file == null)
                CreateFile(fileName, parentNode);
            else
                UpdateFile(node);
        }
        private void CreateFile(string fileName, Node parentNode)
        {
            string realFileName = fileName;

            _handler.Context.Response.StatusCode = 201; // created

            if ((parentNode == null) || !(parentNode is IFolder))
            {
                _handler.Context.Response.StatusCode = 403;
                _handler.Context.Response.Flush();
                return;
            }
            try
            {
                // search for contents referring to this binary
                foreach (Node existingNode in ((IFolder)parentNode).Children)
                {
                    if (SetBinaryAttachment(existingNode, realFileName))
                        return;
                }

                // special filetype, referred in web.config
                string contentType = UploadHelper.GetContentType(realFileName, parentNode.Path);
                if (contentType != null)
                {
                    WebDavProvider.Current.AssertCreateContent(parentNode.Path, fileName, contentType);

                    var nodeType = ActiveSchema.NodeTypes[contentType];
                    var specialFile = nodeType.CreateInstance(parentNode) as IFile;

                    if (specialFile == null)
                    {
                        _handler.Context.Response.StatusCode = 405;
                    }
                    else
                    {
                        var specialNode = (Node)specialFile;
                        if (specialNode.NodeType.IsInstaceOfOrDerivedFrom("Page"))
                            throw new NotSupportedException("Creating or ediding a Page through Webdav is not supported.");

                        specialFile.Binary = new BinaryData();
                        specialNode.Name = fileName;

                        SetBinaryStream(specialNode, _handler.Context.Request.InputStream, FILEPROPERTYNAME, realFileName);
                    }

                    return;
                }

                WebDavProvider.Current.AssertCreateContent(parentNode.Path, fileName, typeof(SN.File).Name);

                // general file
                var fileContent = Content.CreateNew(typeof(SN.File).Name, parentNode, fileName);
                var fileNode = (SN.File)fileContent.ContentHandler;
                fileNode.Binary = new BinaryData();
                SetBinaryStream(fileNode, _handler.Context.Request.InputStream, FILEPROPERTYNAME, realFileName);
            }
            catch (SecurityException e) // logged
            {
                SnLog.WriteException(e);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (SenseNetSecurityException ee) // logged
            {
                SnLog.WriteException(ee);
                _handler.Context.Response.StatusCode = 403;
            }

            return;
        }
        private void UpdateFile(Node file)
        {
            WebDavProvider.Current.AssertModifyContent(file);

            _handler.Context.Response.StatusCode = 200; // created

            try
            {
                // existing file
                SetBinaryStream(file, _handler.Context.Request.InputStream);
            }
            catch (SenseNetSecurityException e) // logged
            {
                SnLog.WriteException(e);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (Exception ee) // logged
            {
                // in some cases content validation fails for the first 
                // webdav request with "empty" stream - swallow this exception
                SnLog.WriteException(ee);
                _handler.Context.Response.StatusCode = 200;
            }
        }

        private void HandleContent(string parentPath, string fileName, Node parentNode, Node node)
        {
            Content content = null;
            var contentXml = new XmlDocument();
            string contentString = RepositoryTools.GetStreamString(_handler.Context.Request.InputStream);

            if (!string.IsNullOrEmpty(contentString))
            {
                contentXml.LoadXml(contentString);

                // new content or previously uploded binary exists!
                if (node == null || node is IFile)
                {
                    string ctName = contentXml.SelectSingleNode("/ContentMetaData/ContentType").InnerText;

                    if (node == null || ctName.CompareTo(node.NodeType.Name) != 0)
                    {
                        WebDavProvider.Current.AssertCreateContent(parentPath, fileName, ctName);

                        var nodeType = ActiveSchema.NodeTypes[ctName];
                        node = nodeType.CreateInstance(parentNode);
                        node.Name = fileName;
                    }
                }
                else
                {
                    WebDavProvider.Current.AssertModifyContent(node);
                }

                content = Content.Create(node);

                XmlNodeList allFields = contentXml.SelectNodes("/ContentMetaData/Fields/*");
                XmlNodeList binaryFields = contentXml.SelectNodes("/ContentMetaData/Fields/*[@attachment]");

                // set attachments first
                foreach (XmlNode field in binaryFields)
                {
                    string attachmentName = field.Attributes["attachment"].Value;
                    var attachment = Node.LoadNode(RepositoryPath.Combine(parentPath, attachmentName)) as SN.File;

                    // previously uploaded attachment found
                    if (attachment != null && attachment.Id != node.Id)
                    {
                        attachment.Name = Guid.NewGuid().ToString();
                        attachment.Save();

                        SetBinaryStream(content.ContentHandler, attachment.Binary.GetStream(), field.Name, attachmentName);
                        attachment.Delete();
                    }
                }

                var transferringContext = new ImportContext(allFields, "", node.Id == 0, true, false);

                // import flat properties
                content.ImportFieldData(transferringContext);

                // update references
                transferringContext.UpdateReferences = true;
                content.ImportFieldData(transferringContext);
            }

            _handler.Context.Response.StatusCode = 200;
        }

        private void InstallContentType()
        {
            string contentString = RepositoryTools.GetStreamString(_handler.Context.Request.InputStream);
            if (!string.IsNullOrEmpty(contentString))
                ContentTypeInstaller.InstallContentType(contentString);
            _handler.Context.Response.StatusCode = 200;
        }

    }
}
