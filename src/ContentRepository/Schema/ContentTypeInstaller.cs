using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using System.IO;
using System.Xml.XPath;
using SenseNet.Configuration;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
    public class ContentTypeInstaller
    {
        private class CTD
        {
            public string Name;
            public string ParentName;
            public XPathDocument Document;

            public CTD(string name, string parentName, XPathDocument doc)
            {
                Name = name;
                ParentName = parentName;
                Document = doc;
            }
        }

        // ========================================================================== Batch installer

        private CTD _contentTypeCTD;
        private SchemaEditor _editor;
        private Dictionary<string, CTD> _docs;

        public static ContentTypeInstaller CreateBatchContentTypeInstaller()
        {
            SchemaEditor editor = new SchemaEditor();
            editor.Load();
            return CreateBatchContentTypeInstaller(editor);
        }
        public static ContentTypeInstaller CreateBatchContentTypeInstaller(SchemaEditor editor)
        {
            return new ContentTypeInstaller(editor);
        }

        private ContentTypeInstaller(SchemaEditor editor)
        {
            _editor = editor;
            _docs = new Dictionary<string, CTD>();
        }

        public void AddContentType(Stream contentTypeDefinitionXml)
        {
            StreamReader reader = new StreamReader(contentTypeDefinitionXml);
            string ctdXml = reader.ReadToEnd();
            reader.Close();
            AddContentType(ctdXml);
        }
        public void AddContentType(string contentTypeDefinitionXml)
        {
            XPathDocument ctd = new XPathDocument(new StringReader(contentTypeDefinitionXml));
            XPathNavigator nav = ctd.CreateNavigator().SelectSingleNode("/*[1]");

            // check xml namespace
            if (nav.NamespaceURI != ContentType.ContentDefinitionXmlNamespace)
                if (RepositoryEnvironment.BackwardCompatibilityXmlNamespaces)
                    if (nav.NamespaceURI != ContentType.ContentDefinitionXmlNamespaceOld)
                        throw new ApplicationException("Passed XML is not a ContentTypeDefinition");

            string name = nav.GetAttribute("name", "");
            string parentName = nav.GetAttribute("parentType", "");
            AddContentType(name, new CTD(name, parentName, ctd));
        }
        private void AddContentType(string name, CTD ctd)
        {
            if (name == typeof(ContentType).Name)
                _contentTypeCTD = ctd;
            else
                _docs.Add(name, ctd);
        }
        public void ExecuteBatch()
        {
            // Install considering dependencies
            if (_contentTypeCTD != null)
                Install(_contentTypeCTD);

            List<CTD> docList = new List<CTD>(_docs.Values);
            Stack<CTD> stack = new Stack<CTD>();
            CTD parent = null;
            while (docList.Count > 0)
            {
                CTD doc = parent ?? docList[0];
                docList.Remove(doc);
                _docs.Remove(doc.Name);
                if (_docs.ContainsKey(doc.ParentName))
                {
                    stack.Push(doc);
                    parent = _docs[doc.ParentName];
                }
                else
                {
                    Install(doc);
                    while (stack.Count > 0)
                        Install(stack.Pop());
                    parent = null;
                }
            }
            _editor.Register();

            // The ContentTypeManager distributes its reset, no custom DistributedAction call needed
            ContentTypeManager.Reset();
        }

        private void Install(CTD ctd)
        {
            var contentType = ContentTypeManager.LoadOrCreateNew(ctd.Document);

            // skip notification during content type install to avoid missing field errors
            contentType.DisableObserver(TypeResolver.GetType(NodeObserverNames.NOTIFICATION, false));

            ContentTypeManager.ApplyChangesInEditor(contentType, _editor);
            contentType.Save(false);
            ContentTypeManager.Current.AddContentType(contentType);
        }

        // ========================================================================== Static installer

        public static void InstallContentType(Stream contentTypeDefinitionXml)
        {
            StreamReader reader = new StreamReader(contentTypeDefinitionXml);
            string xml = reader.ReadToEnd();
            reader.Close();
            InstallContentType(xml);
        }
        public static void InstallContentType(params string[] contentTypeDefinitionXmls)
        {
            if (contentTypeDefinitionXmls.Length == 0)
                return;
            var installer = CreateBatchContentTypeInstaller();
            foreach (var xml in contentTypeDefinitionXmls)
                installer.AddContentType(xml);
            installer.ExecuteBatch();
        }


        public static void RemoveContentType(string contentTypeName)
        {
            RemoveContentType(ContentTypeManager.Current.GetContentTypeByName(contentTypeName));
        }
        public static void RemoveContentType(ContentType contentType)
        {
            contentType.Delete();
            // The ContentTypeManager distributes its reset, no custom DistributedAction call needed
            ContentTypeManager.Reset();
        }
    }
}