using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
namespace SenseNet.Packaging.Steps
{
    internal static class Exporter
    {
        private static readonly List<string> ForbiddenFileNames = new List<string>(new [] { "PRN", "LST", "TTY", "CRT", "CON" });
        private static readonly string Cr = Environment.NewLine;

        private static int _exceptions;
        public static void Export(string repositoryPath, string targetPath, string queryString = null)
        {
            try
            {
                // check fs folder
                var dirInfo = new DirectoryInfo(targetPath);
                ExportContext context;

                if (!dirInfo.Exists)
                {
                    Logger.LogMessage("Creating target directory: " + targetPath + " ... ");
                    Directory.CreateDirectory(targetPath);
                    Logger.LogMessage("Ok");
                }
                else
                {
                    Logger.LogMessage("Target directory exists: " + targetPath 
                        + ". Exported contents will override existing subelements.");
                }

                if (!string.IsNullOrWhiteSpace(queryString))
                {
                    LogExportHeader(repositoryPath, queryString, targetPath);
                    context = new ExportContext(repositoryPath, targetPath);
                    ExportByFilterText(context, targetPath, queryString);
                    LogExportFooter(context);
                }
                else
                {
                    // load export root
                    var root = Content.Load(repositoryPath);
                    if (root == null)
                    {
                        Logger.LogMessage("Content does not exist: " + repositoryPath);
                    }
                    else
                    {
                        LogExportHeader(repositoryPath, queryString, targetPath);
                        context = new ExportContext(repositoryPath, targetPath);
                        ExportContentTree(root, context, targetPath, "");
                        LogExportFooter(context);
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
            LogExportFinished();
        }

        private static void LogExportHeader(string repositoryPath, string queryPath, string targetPath)
        {
            Logger.LogMessage("=========================== Export ===========================");
            Logger.LogMessage("From: " + repositoryPath);
            Logger.LogMessage("To:   " + targetPath);

            if (queryPath != null)
            {
                Logger.LogMessage("Filter: " + queryPath);
            }
            Logger.LogMessage("==============================================================");
        }
        private static void LogExportFooter(ExportContext context)
        {
            Logger.LogMessage("--------------------------------------------------------------");
            Logger.LogMessage("Outer references:");
            var outerRefs = context.GetOuterReferences();
            if (outerRefs.Count == 0)
                Logger.LogMessage("All references are exported.");
            else
                foreach (var item in outerRefs)
                {
                    Logger.LogMessage(item);
                }
        }
        private static void LogExportFinished()
        {
            Logger.LogMessage("==============================================================");
            if (_exceptions == 0)
            {
                Logger.LogMessage("Export is successfully finished.");
            }
            else
            {
                Logger.LogMessage("Export is finished with " + _exceptions + " errors.");
            }
        }

        private static void ExportContentTree(Content content, ExportContext context, string fsPath, string indent)
        {
            try
            {
                ExportContent(content, context, fsPath, indent);
            }
            catch (Exception ex)
            {
                LogException(ex);
                return;
            }

            //TODO: SmartFolder may contain real items too
            if (content.ContentHandler is SmartFolder)
                return;

            // create folder only if it has children
            var contentAsFolder = content.ContentHandler as IFolder;
            var contentAsGeneric = content.ContentHandler as GenericContent;

            // try everything that can have children (generic content, content types or other non-gc nodes)
            if (contentAsFolder == null && contentAsGeneric == null)
                return;

            try
            {
                var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled };
                var queryResult = contentAsFolder == null ? contentAsGeneric.GetChildren(settings) : contentAsFolder.GetChildren(settings);
                if (queryResult.Count == 0)
                    return;

                var children = queryResult.Nodes;
                var fileName = GetSafeFileNameFromContentName(content.Name);
                var newDir = Path.Combine(fsPath, fileName);
                if (System.IO.File.Exists(newDir))
                    newDir = Path.Combine(fsPath, fileName + ".Children");

                if (!(content.ContentHandler is ContentType))
                    Directory.CreateDirectory(newDir);

                var newIndent = indent + "  ";
                foreach (var childContent in from node in children select Content.Create(node))
                    ExportContentTree(childContent, context, newDir, newIndent);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
        private static void ExportContent(Content content, ExportContext context, string fsPath, string indent)
        {
            if (content.ContentHandler is ContentType)
            {
                Logger.LogMessage(indent + content.Name);
                ExportContentType(content, context);
                return;
            }
            context.CurrentDirectory = fsPath;
            Logger.LogMessage(indent + content.Name);
            string metaFilePath = Path.Combine(fsPath, content.Name + ".Content");
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "  ";
            XmlWriter writer = null;
            try
            {
                writer = XmlWriter.Create(metaFilePath, settings);

                // <?xml version="1.0" encoding="utf-8"?>
                // <ContentMetaData>
                //    <ContentType>Site</ContentType>
                //    <Fields>
                //        ...
                writer.WriteStartDocument();
                writer.WriteStartElement("ContentMetaData");
                writer.WriteElementString("ContentType", content.ContentType.Name);
                writer.WriteElementString("ContentName", content.Name);
                writer.WriteStartElement("Fields");
                try
                {
                    content.ExportFieldData(writer, context);
                }
                catch (Exception e)
                {
                    LogException(e);
                    writer.WriteComment(String.Concat("EXPORT ERROR", Cr, e.Message, Cr, e.StackTrace));
                }
                writer.WriteEndElement();
                writer.WriteStartElement("Permissions");
                writer.WriteElementString("Clear", null);
                content.ContentHandler.Security.ExportPermissions(writer);
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

        private static void LogException(Exception exception)
        {
            _exceptions++;
            Logger.LogException(exception);
        }
        private static string GetSafeFileNameFromContentName(string name)
        {
            if (ForbiddenFileNames.Contains(name.ToUpper()))
                return name + "!";
            return name;
        }
        
        private static void ExportByFilterText(ExportContext context, string fsRoot, string queryText)
        {
            var query = ContentQuery.CreateQuery(queryText);
            query.AddClause(@"InTree:""" + context.SourceFsPath + @"""", ChainOperator.And);
            var result = query.Execute();
            var maxCount = result.Count;
            var count = 0;
            foreach (var nodeId in result.Identifiers)
            {
                try
                {
                    var content = Content.Load(nodeId);
                    var relIndex = RepositoryPath.GetParentPath(context.SourceFsPath).Length + 1;
                    var relPath = content.Path.Substring(relIndex).Replace("/", "\\");
                    var fsPath = Path.Combine(fsRoot, relPath);
                    var fsDir = Path.GetDirectoryName(fsPath);
                    var dirInfo = new DirectoryInfo(fsDir);
                    if (!dirInfo.Exists)
                        Directory.CreateDirectory(fsDir);

                    ExportContent(content, context, fsDir, string.Concat(++count, "/", maxCount, ": ", Path.GetDirectoryName(relPath), "\\"));
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }
        }
        private static void ExportContentType(Content content, ExportContext context)
        {
            BinaryData binaryData = ((ContentType)content.ContentHandler).Binary;

            var name = content.Name + "Ctd.xml";
            var fsPath = Path.Combine(context.ContentTypeDirectory, name);

            Stream source = null;
            FileStream target = null;
            try
            {
                source = binaryData.GetStream();
                target = new FileStream(fsPath, FileMode.Create);
                for (var i = 0; i < source.Length; i++)
                    target.WriteByte((byte)source.ReadByte());
            }
            finally
            {
                source?.Close();
                if (target != null)
                {
                    target.Flush();
                    target.Close();
                }
            }
        }
    }
}
