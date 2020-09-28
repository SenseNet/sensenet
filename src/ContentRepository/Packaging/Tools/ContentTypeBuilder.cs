using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Tools
{
    #region Interfaces
    public interface IContentTypeBuilder
    {
        IContentTypeBuilder DisplayName(string value);
        IContentTypeBuilder Description(string value);
        IContentTypeBuilder Icon(string value);
        //IContentTypeBuilder AddAllowedChildTypes(params string[] typeNames);
        IFieldEditor Field(string name, string type = null);
    }
    public interface IFieldEditor
    {
        IFieldEditor DefaultValue(string value);
        IFieldEditor DisplayName(string value);
        IFieldEditor Description(string value);
        IFieldEditor VisibleBrowse(FieldVisibility visibility);
        IFieldEditor VisibleEdit(FieldVisibility visibility);
        IFieldEditor VisibleNew(FieldVisibility visibility);
        IFieldEditor FieldIndex(int value);
        IFieldEditor ReadOnly(bool value = true);
        IFieldEditor Compulsory(bool value = true);
        IFieldEditor ControlHint(string value);
        IFieldEditor Configure(string key, string value);
        IFieldEditor Field(string name, string type = null);
    }
    #endregion

    #region Internal helper classes

    internal class CtdBuilder : IContentTypeBuilder
    {
        internal string ContentTypeName { get; }
        internal string DisplayNameValue { get; set; }
        internal string DescriptionValue { get; set; }
        internal string IconValue { get; set; }
        internal string[] AllowedChildTypesToAdd { get; set; }

        internal IList<FieldEditor> FieldEditors { get; } = new List<FieldEditor>();

        internal CtdBuilder(string name)
        {
            ContentTypeName = name;
        }

        public IContentTypeBuilder DisplayName(string value)
        {
            DisplayNameValue = value;
            return this;
        }
        public IContentTypeBuilder Description(string value)
        {
            DescriptionValue = value;
            return this;
        }
        public IContentTypeBuilder Icon(string value)
        {
            IconValue = value;
            return this;
        }
        //public IContentTypeBuilder AddAllowedChildTypes(params string[] typeNames)
        //{
        //    AllowedChildTypesToAdd = typeNames;
        //    return this;
        //}
        public IFieldEditor Field(string name, string type = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var editor = FieldEditors.FirstOrDefault(fe => fe.FieldName == name);
            if (editor != null)
                return editor;

            editor = new FieldEditor(this, name, type);
            FieldEditors.Add(editor);

            return editor;
        }
    }

    internal class FieldEditor : IFieldEditor
    {
        internal string DisplayNameValue { get; set; }
        internal string DescriptionValue { get; set; }
        internal ConfigurationInfo Configuration { get; } = new ConfigurationInfo();
        internal bool ConfigurationChanged { get; private set; }

        private CtdBuilder _ctdBuilder;
        internal string FieldName { get; }
        internal string Type { get; }

        internal FieldEditor(CtdBuilder ctdBuilder, string name, string type)
        {
            _ctdBuilder = ctdBuilder;
            FieldName = name;
            Type = type;
        }

        public IFieldEditor DefaultValue(string value)
        {
            Configuration.DefaultValue = value;
            ConfigurationChanged = true;
            return this;
        }
        public IFieldEditor DisplayName(string value)
        {
            DisplayNameValue = value;
            return this;
        }
        public IFieldEditor Description(string value)
        {
            DescriptionValue = value;
            return this;
        }

        public IFieldEditor VisibleBrowse(FieldVisibility visibility)
        {
            Configuration.VisibleBrowse = visibility;
            ConfigurationChanged = true;
            return this;
        }
        public IFieldEditor VisibleEdit(FieldVisibility visibility)
        {
            Configuration.VisibleEdit = visibility;
            ConfigurationChanged = true;
            return this;
        }
        public IFieldEditor VisibleNew(FieldVisibility visibility)
        {
            Configuration.VisibleNew = visibility;
            ConfigurationChanged = true;
            return this;
        }

        public IFieldEditor FieldIndex(int value)
        {
            Configuration.FieldIndex = value;
            ConfigurationChanged = true;
            return this;
        }

        public IFieldEditor ReadOnly(bool value = true)
        {
            Configuration.ReadOnly = true;
            ConfigurationChanged = true;
            return this;
        }

        public IFieldEditor Compulsory(bool value = true)
        {
            Configuration.Compulsory = value;
            ConfigurationChanged = true;
            return this;
        }

        public IFieldEditor ControlHint(string value)
        {
            Configuration.ControlHint = value;
            ConfigurationChanged = true;
            return this;
        }

        public IFieldEditor Configure(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (Configuration.FieldSpecific == null)
                Configuration.FieldSpecific = new Dictionary<string, object>();

            Configuration.FieldSpecific[key] = value;
            ConfigurationChanged = true;
            return this;
        }

        public IFieldEditor Field(string name, string type = null)
        {
            return _ctdBuilder.Field(name, type);
        }
    }

    #endregion

    /// <summary>
    /// Basic Content Type editor API for modifying content types.
    /// </summary>
    public class ContentTypeBuilder
    {
        internal IList<CtdBuilder> ContentTypeBuilders { get; } = new List<CtdBuilder>();

        public IContentTypeBuilder Type(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var ctb = ContentTypeBuilders.FirstOrDefault(cb => cb.ContentTypeName == name);
            if (ctb != null)
                return ctb;

            ctb = new CtdBuilder(name);
            ContentTypeBuilders.Add(ctb);

            return ctb;
        }
        public void Apply()
        {
            foreach (var ctdBuilder in ContentTypeBuilders)
            {
                var ct = ContentType.GetByName(ctdBuilder.ContentTypeName);
                if (ct == null)
                {
                    //UNDONE: log missing ctd!
                    continue;
                }

                var ctdXml = LoadContentTypeXmlDocument(ct);

                EditContentTypeHeader(ctdXml, ctdBuilder);

                foreach (var fieldEditor in ctdBuilder.FieldEditors)
                {
                    EditField(ctdXml, fieldEditor);
                }

                // apply changes
                ContentTypeInstaller.InstallContentType(ctdXml.OuterXml);
            }
        }

        #region Helper methods

        private const string NamespacePrefix = "x";
        private const string ConfigurationName = "Configuration";

        private void EditContentTypeHeader(XmlDocument xDoc, CtdBuilder builder)
        {
            void SetProperty(string propertyName, string value)
            {
                if (value == null) 
                    return;

                var propertyElement = LoadOrAddChild(xDoc.DocumentElement, propertyName);
                propertyElement.InnerXml = value;
            }

            SetProperty("DisplayName", builder.DisplayNameValue);
            SetProperty("Description", builder.DescriptionValue);
            SetProperty("Icon", builder.IconValue);
        }

        private void EditField(XmlDocument xDoc, FieldEditor fieldEditor)
        {
            var fieldElement = LoadFieldElement(xDoc, fieldEditor.FieldName, fieldEditor.Type);

            SetProperty(fieldElement, "DisplayName", fieldEditor.DisplayNameValue);
            SetProperty(fieldElement, "Description", fieldEditor.DescriptionValue);

            if (fieldEditor.ConfigurationChanged)
            {
                var configNode = LoadOrAddChild(fieldElement, ConfigurationName, false);

                SetProperty(configNode, "DefaultValue", fieldEditor.Configuration.DefaultValue);
                SetProperty(configNode, "ControlHint", fieldEditor.Configuration.ControlHint);
                SetIntProperty(configNode, "FieldIndex", fieldEditor.Configuration.FieldIndex);
                SetBoolProperty(configNode, "ReadOnly", fieldEditor.Configuration.ReadOnly);
                SetBoolProperty(configNode, "Compulsory", fieldEditor.Configuration.Compulsory);

                SetVisibility(configNode, "VisibleBrowse", fieldEditor.Configuration.VisibleBrowse);
                SetVisibility(configNode, "VisibleEdit", fieldEditor.Configuration.VisibleEdit);
                SetVisibility(configNode, "VisibleNew", fieldEditor.Configuration.VisibleNew);

                if (fieldEditor.Configuration.FieldSpecific != null)
                {
                    foreach (var kv in fieldEditor.Configuration.FieldSpecific)
                    {
                        SetProperty(configNode, kv.Key, kv.Value?.ToString() ?? string.Empty);
                    }
                }
            }

            static void SetVisibility(XmlNode parentNode, string propertyName, FieldVisibility? visibility)
            {
                if (!visibility.HasValue)
                    return;
                SetProperty(parentNode, propertyName, visibility.Value.ToString());
            }
            static void SetIntProperty(XmlNode parentNode, string propertyName, int? value)
            {
                if (!value.HasValue)
                    return;
                SetProperty(parentNode, propertyName, value.Value.ToString());
            }
            static void SetBoolProperty(XmlNode parentNode, string propertyName, bool? value)
            {
                if (!value.HasValue)
                    return;
                SetProperty(parentNode, propertyName, value.Value.ToString());
            }
            static void SetProperty(XmlNode parentNode, string propertyName, string value)
            {
                if (value == null)
                    return;

                var propertyElement = LoadOrAddChild(parentNode, propertyName);
                propertyElement.InnerXml = value;
            }
        }

        private static XmlElement LoadFieldElement(XmlDocument xDoc, string fieldName, string type, bool throwOnError = true)
        {
            var fieldNode = xDoc.SelectSingleNode($"//{NamespacePrefix}:Field[@name='{fieldName}']", 
                    GetNamespaceManager(xDoc)) as XmlElement;

            if (fieldNode == null && !string.IsNullOrEmpty(type))
            {
                var parentNode = LoadOrAddChild(xDoc.DocumentElement, "Fields");
                if (parentNode != null)
                {
                    fieldNode = xDoc.CreateElement("Field", xDoc.DocumentElement.NamespaceURI);
                    parentNode.AppendChild(fieldNode);

                    fieldNode.SetAttribute("name", fieldName);
                    fieldNode.SetAttribute("type", type);
                }
            }

            if (fieldNode == null && throwOnError)
                throw new PackagingException(string.Format(SR.Errors.Content.FieldNotFound_1, fieldName));

            return fieldNode;
        }
        private static XmlNode LoadOrAddChild(XmlNode parentNode, string name, bool insertIfPossible = true, IDictionary<string, string> attributes = null)
        {
            if (parentNode?.OwnerDocument?.DocumentElement == null)
                throw new InvalidOperationException("Invalid CTD xml.");

            var childElement = LoadChild(parentNode, name);
            if (childElement == null)
            {
                childElement = parentNode.OwnerDocument.CreateElement(name, parentNode.OwnerDocument.DocumentElement.NamespaceURI);

                if (insertIfPossible)
                {
                    //UNDONE: implement insert before or after

                    XmlNode insertBeforeElement = null;
                    XmlNode insertAfterElement = null;
                    
                    //if (!string.IsNullOrEmpty(InsertBefore))
                    //    insertBeforeElement = LoadChild(parentNode, InsertBefore);
                    //if (!string.IsNullOrEmpty(InsertAfter))
                    //    insertAfterElement = LoadChild(parentNode, InsertAfter);

                    //if (insertBeforeElement != null)
                    //    parentNode.InsertBefore(childElement, insertBeforeElement);
                    //else if (insertAfterElement != null)
                    //    parentNode.InsertAfter(childElement, insertAfterElement);
                    //else
                        parentNode.AppendChild(childElement);
                }
                else
                {
                    parentNode.AppendChild(childElement);
                }

                //UNDONE: log!
                //Logger.LogMessage("New xml element was created: " + name);
            }

            if (attributes != null && childElement is XmlElement xmlElement)
            {
                foreach (var key in attributes.Keys)
                {
                    xmlElement.SetAttribute(key, attributes[key]);
                }
            }

            return childElement;
        }
        protected static XmlNode LoadChild(XmlNode parent, string childName)
        {
            return parent.SelectSingleNode($"{NamespacePrefix}:{childName}", GetNamespaceManager(parent.OwnerDocument));
        }
        internal static XmlNamespaceManager GetNamespaceManager(XmlDocument xDoc)
        {
            var xnm = new XmlNamespaceManager(xDoc.NameTable);
            xnm.AddNamespace(NamespacePrefix, xDoc.DocumentElement?.NamespaceURI ?? string.Empty);

            return xnm;
        }
        private static XmlDocument LoadContentTypeXmlDocument(ContentType contentType)
        {
            var ctdXDoc = new XmlDocument();
            using (var ctdStream = contentType.Binary.GetStream())
                ctdXDoc.Load(ctdStream);

            return ctdXDoc;
        }

        #endregion
    }
}
