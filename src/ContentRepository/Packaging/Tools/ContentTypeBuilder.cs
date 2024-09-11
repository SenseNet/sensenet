﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics.Analysis;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Tools
{
    #region Interfaces
    public interface IContentTypeBuilder
    {
        IContentTypeBuilder DisplayName(string value);
        IContentTypeBuilder Description(string value);
        IContentTypeBuilder IsSystemType(bool? value);
        IContentTypeBuilder Icon(string value);
        //IContentTypeBuilder AddAllowedChildTypes(params string[] typeNames);
        IFieldEditor Field(string name, string type = null);
        IContentTypeBuilder RemoveField(string name);
        IContentTypeBuilder AddCategory(params string[] category);
        IContentTypeBuilder ClearCategories();
        IContentTypeBuilder RemoveCategory(params string[] category);
    }
    public interface IFieldEditor
    {
        IFieldEditor DefaultValue(string value);
        IFieldEditor DisplayName(string value);
        IFieldEditor Description(string value);
        IFieldEditor Bind(string value);
        IFieldEditor VisibleBrowse(FieldVisibility visibility);
        IFieldEditor VisibleEdit(FieldVisibility visibility);
        IFieldEditor VisibleNew(FieldVisibility visibility);
        IFieldEditor FieldIndex(int value);
        IFieldEditor ReadOnly(bool value = true);
        IFieldEditor Compulsory(bool value = true);
        IFieldEditor ControlHint(string value);
        IFieldEditor Configure(string key, string value);
        IFieldEditor RemoveProperty(string name);
        IFieldEditor RemoveConfiguration(string key);
        IContentTypeBuilder Delete();
        IFieldEditor Field(string name, string type = null);
    }
    #endregion

    #region Internal helper classes

    internal enum CategoryOperation{ Add, Remove, Clear }

    internal class CtdBuilder : IContentTypeBuilder
    {
        internal string ContentTypeName { get; }
        internal string DisplayNameValue { get; set; }
        internal string DescriptionValue { get; set; }
        internal string IsSystemTypeValue { get; set; }
        internal string IconValue { get; set; }
        internal string[] AllowedChildTypesToAdd { get; set; }
        internal List<(CategoryOperation, string)> CategoryOperations{ get; } = new();

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

        public IContentTypeBuilder IsSystemType(bool? value)
        {
            // Empty means we have to remove the property, null means it was not set.
            IsSystemTypeValue = !value.HasValue ? string.Empty : value.ToString().ToLower();
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

        public IContentTypeBuilder RemoveField(string name)
        {
            var field = Field(name);
            return field.Delete();
        }

        public IContentTypeBuilder AddCategory(params string[] category)
        {
            foreach (var cat in category)
                CategoryOperations.Add(new(CategoryOperation.Add, cat));
            return this;
        }
        public IContentTypeBuilder ClearCategories()
        {
            CategoryOperations.Add(new(CategoryOperation.Clear, null));
            return this;
        }
        public IContentTypeBuilder RemoveCategory(params string[] category)
        {
            foreach (var cat in category)
                CategoryOperations.Add(new(CategoryOperation.Remove, cat));
            return this;
        }
    }

    internal class FieldEditor : IFieldEditor
    {
        internal string DisplayNameValue { get; set; }
        internal string DescriptionValue { get; set; }
        internal string BindValue { get; set; }
        internal ConfigurationInfo Configuration { get; } = new ConfigurationInfo();
        internal bool ConfigurationChanged { get; private set; }

        private readonly CtdBuilder _ctdBuilder;
        internal string FieldName { get; }
        internal string Type { get; }
        internal bool DeleteField { get; private set; }
        internal IList<string> PropertiesToDelete { get; } = new List<string>();
        internal IList<string> ConfigurationToDelete { get; } = new List<string>();

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
        public IFieldEditor Bind(string value)
        {
            BindValue = value;
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
            Configuration.ReadOnly = value;
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

        public IFieldEditor RemoveProperty(string name)
        {
            if (!string.IsNullOrEmpty(name) && !PropertiesToDelete.Contains(name))
                PropertiesToDelete.Add(name);

            return this;
        }
        public IFieldEditor RemoveConfiguration(string name)
        {
            if (!string.IsNullOrEmpty(name) && !ConfigurationToDelete.Contains(name))
            {
                ConfigurationToDelete.Add(name);
                ConfigurationChanged = true;
            }

            return this;
        }

        public IContentTypeBuilder Delete()
        {
            DeleteField = true;
            return _ctdBuilder;
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
        private readonly ILogger _logger;
        internal IList<CtdBuilder> ContentTypeBuilders { get; } = new List<CtdBuilder>();
        internal Dictionary<string, string> ChangedFieldTypes { get; } = new Dictionary<string, string>();

        public ContentTypeBuilder(ILogger<ContentTypeBuilder> logger)
        {
            _logger = (ILogger) logger ?? NullLogger.Instance;
        }

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

        public ContentTypeBuilder ChangeFieldType(string fieldName, string targetType)
        {
            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException(nameof(fieldName));
            if (string.IsNullOrEmpty(targetType))
                throw new ArgumentNullException(nameof(targetType));

            ChangedFieldTypes[fieldName] = targetType;

            return this;
        }

        public void Apply()
        {
            // Execute field type changes first so that subsequent field operations
            // are performed on the new field.
            foreach (var changedFieldType in ChangedFieldTypes)
            {
                ChangeFieldTypeInternal(changedFieldType.Key, changedFieldType.Value);
            }

            foreach (var ctdBuilder in ContentTypeBuilders)
            {
                var ct = ContentType.GetByName(ctdBuilder.ContentTypeName);
                if (ct == null)
                {
                    _logger.LogWarning($"Content type {ctdBuilder.ContentTypeName} does not exist.");
                    continue;
                }

                var ctdXml = LoadContentTypeXmlDocument(ct);

                EditContentTypeHeader(ctdXml, ctdBuilder);

                foreach (var fieldEditor in ctdBuilder.FieldEditors)
                {
                    EditField(ctdXml, fieldEditor, ctdBuilder);
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

                _logger.LogTrace($"Setting property {propertyName} of content type {builder.ContentTypeName} " +
                                 $"to {value.Substring(0, Math.Min(value.Length, 50))}");

                var propertyElement = LoadOrAddChild(xDoc.DocumentElement, propertyName);
                propertyElement.InnerXml = value;
            }
            void DeleteProperty(string propertyName)
            {
                var propertyElement = LoadChild(xDoc.DocumentElement, propertyName);
                if (propertyElement == null) 
                    return;

                _logger.LogTrace($"Deleting property {propertyName} of content type {builder.ContentTypeName}");
                xDoc.DocumentElement?.RemoveChild(propertyElement);
            }
            string PlayCategoryOperations(List<(CategoryOperation Verb, string Category)> categoryOperations)
            {
                var origElement = LoadChild(xDoc.DocumentElement, "Categories");
                var categories = origElement?.InnerText.Split(ContentType.XmlListSeparators,
                    StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                foreach (var operation in categoryOperations)
                {
                    switch (operation.Verb)
                    {
                        case CategoryOperation.Add:
                            if (!categories.Contains(operation.Category))
                                categories.Add(operation.Category);
                            break;
                        case CategoryOperation.Remove:
                            categories.Remove(operation.Category);
                            break;
                        case CategoryOperation.Clear:
                            categories.Clear();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return string.Join(" ", categories);
            }

            SetProperty("DisplayName", builder.DisplayNameValue);
            SetProperty("Description", builder.DescriptionValue);
            SetProperty("Icon", builder.IconValue);

            // Empty means we have to remove the property, null means it was not set.
            if (builder.IsSystemTypeValue == string.Empty)
                DeleteProperty("SystemType");
            else if (builder.IsSystemTypeValue != null)
                SetProperty("SystemType", builder.IsSystemTypeValue);

            var categories = PlayCategoryOperations(builder.CategoryOperations);
            if (string.IsNullOrEmpty(categories))
                DeleteProperty("Categories");
            else
                SetProperty("Categories", categories);
        }

        private void EditField(XmlDocument xDoc, FieldEditor fieldEditor, CtdBuilder ctdBuilder)
        {
            // if we should delete the field, do not throw an error if the xml node is missing
            var fieldElement = LoadFieldElement(xDoc, fieldEditor.FieldName, fieldEditor.Type, !fieldEditor.DeleteField);

            if (fieldEditor.DeleteField)
            {
                _logger.LogTrace($"Deleting field {fieldEditor.FieldName} of content type {ctdBuilder.ContentTypeName}");
                fieldElement?.ParentNode?.RemoveChild(fieldElement);
                return;
            }

            _logger.LogTrace($"Editing field {fieldEditor.FieldName} of content type {ctdBuilder.ContentTypeName}");

            SetProperty(fieldElement, "DisplayName", fieldEditor.DisplayNameValue);
            SetProperty(fieldElement, "Description", fieldEditor.DescriptionValue);
            SetPropertyAsAttribute(fieldElement, "Bind", "property", fieldEditor.BindValue);

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

                // remove configuration sections
                foreach (var configToDelete in fieldEditor.ConfigurationToDelete)
                {
                    var child = LoadChild(configNode, configToDelete);
                    if (child != null)
                        configNode.RemoveChild(child);
                }

                if (configNode.ChildNodes.Count == 0)
                    configNode.ParentNode?.RemoveChild(configNode);
            }

            // remove properties
            if (fieldElement != null)
            {
                foreach (var propertyToDelete in fieldEditor.PropertiesToDelete)
                {
                    var child = LoadChild(fieldElement, propertyToDelete);
                    if (child != null)
                        fieldElement.RemoveChild(child);
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
            static void SetPropertyAsAttribute(XmlNode parentNode, string elementName, string propertyName, string value)
            {
                if (value == null)
                    return;

                var propertyElement = (XmlElement)LoadOrAddChild(parentNode, elementName);
                propertyElement.SetAttribute(propertyName, value);
            }
        }

        private void ChangeFieldTypeInternal(string fieldName, string targetType)
        {
            // Load all content types that define this field. OrderByDescending is important because we
            // have to remove the field starting from the leaves.
            var contentTypeNames = ContentType.GetContentTypes()
                .Where(ct => ct.FieldSettings.Any(f => f.Name == fieldName && f.Owner.Id == ct.Id))
                .OrderByDescending(ct => ct.Path)
                .Select(ct => ct.Name)
                .ToArray();

            if (!contentTypeNames.Any())
                return;

            _logger.LogTrace($"Changing type of {fieldName} to {targetType} on the following " +
                             $"types: {string.Join(", ", contentTypeNames)}");

            var oldValues = new Dictionary<int, object>();
            var fieldXmls = new Dictionary<string, string>();

            foreach (var contentTypeName in contentTypeNames)
            {
                // load content types freshly to avoid cache issues
                var contentType = ContentType.GetByName(contentTypeName);
                var fieldSetting = contentType.GetFieldSettingByName(fieldName);
                var ctdXml = LoadContentTypeXmlDocument(contentType);
                var fieldElement = LoadFieldElement(ctdXml, fieldName, null, false);
                var currentType = fieldElement.GetAttribute("type");
                if (string.IsNullOrEmpty(currentType))
                {
                    _logger.LogWarning($"Field {fieldName} type is missing in {contentTypeName} content type xml.");
                    continue;
                }
                if (currentType == targetType)
                {
                    _logger.LogTrace($"The type of field {fieldName} is already {targetType} in {contentTypeName}.");
                    continue;
                }

                _logger.LogTrace($"Collecting values of {fieldName} for {contentType.Name} content items...");

                var skipZero = fieldSetting is NumberFieldSetting;
                var skipEmptyText = fieldSetting is ShortTextFieldSetting;

                // iterate through existing content items and memorize values
                foreach (var content in Content.All.DisableAutofilters().Where(c => c.TypeIs(contentType.Name)))
                {
                    var val = content[fieldName];
                    if (val == null)
                        continue;

                    // it does not make sense to re-save a content if the value is 0
                    // (it is similar to a 'null' value in case of number fields)
                    if (skipZero)
                    {
                        if (double.TryParse(val.ToString(), out var numVal) && numVal.CompareTo(0d) == 0)
                            continue;
                    }

                    // skip empty text in case of text fields
                    if (skipEmptyText && val is string stringValue && string.IsNullOrEmpty(stringValue))
                        continue;

                    oldValues[content.Id] = val;
                }

                _logger.LogTrace($"Memorized {oldValues.Count} values.");

                // remove old field
                fieldElement.ParentNode?.RemoveChild(fieldElement);
                ContentTypeInstaller.InstallContentType(ctdXml.OuterXml);

                _logger.LogTrace($"Field {fieldName} was removed from content type {contentType.Name}.");

                // set the new type and memorize the old field xml to preserve field properties
                fieldElement.SetAttribute("type", targetType);
                fieldXmls[contentType.Name] = fieldElement.InnerXml;
            }

            // Iterate through the types again in reverse order (root --> leaves) and register 
            // the field with the new type.
            foreach (var contentTypeName in contentTypeNames.Reverse())
            {
                var contentType = ContentType.GetByName(contentTypeName);
                var ctdXml = LoadContentTypeXmlDocument(contentType);

                // add new field and re-set previous field properties
                var fieldElement = LoadFieldElement(ctdXml, fieldName, targetType, false);

                if (fieldXmls.TryGetValue(contentTypeName, out var oldFieldXml))
                {
                    fieldElement.InnerXml = oldFieldXml;

                    // register the new field
                    ContentTypeInstaller.InstallContentType(ctdXml.OuterXml);

                    _logger.LogTrace($"Field {fieldName} was added to content type " +
                                     $"{contentTypeName} with the new type {targetType}.");
                }
            }

            if (!oldValues.Any()) 
                return;

            _logger.LogTrace($"Migrating values of field {fieldName}...");

            //TODO: let developers provide an archive field
            // If an archive field is provided, use that instead of the newly added field.
            // (e.g. migrating shorttext values is not possible if the new field is a reference field)
            //var targetFieldName = !string.IsNullOrEmpty(ArchiveFieldName) ? ArchiveFieldName : fieldName;

            var changedCount = 0;

            // iterate cached ids and set old values to the new field
            foreach (var content in Node.LoadNodes(oldValues.Keys).Select(Content.Create))
            {
                try
                {
                    // if a migration fails on a single content, we only log it. Patch log should be reviewed by the operator!
                    content[fieldName] = oldValues[content.Id];
                    content.SaveSameVersionAsync(CancellationToken.None).GetAwaiter().GetResult();
                    changedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during migrating {content.Path}. {ex.Message}");
                }
            }

            _logger.LogTrace($"Field {fieldName} was changed on {changedCount} content items.");
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

        // This list represents the order of xml nodes in a Field section of a CTD. We use
        // it to insert a new xml node to a correct location.
        private static readonly List<string> FieldPropertyOrder = new List<string>(new[]
        {
            "DisplayName", "Description", "Icon", "AllowedChildTypes", "Preview", "AllowIncrementalNaming", "Categories", "SystemType", 
            "AppInfo", "Bind", "Indexing", "Configuration"
        });

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
                    //TODO: implement insert before or after for CTD header
                    // Currently we use Field child node ordering for everything.

                    XmlNode insertBeforeElement = null;
                    XmlNode insertAfterElement = null;

                    var propertyIndex = FieldPropertyOrder.Contains(name) 
                        ? FieldPropertyOrder.IndexOf(name)
                        : -1;

                    // find a preceding node
                    var previousIndex = propertyIndex - 1;
                    while (previousIndex >= 0)
                    {
                        insertAfterElement = LoadChild(parentNode, FieldPropertyOrder[previousIndex]);
                        if (insertAfterElement != null)
                            break;

                        previousIndex--;
                    }

                    // find a subsequent node
                    var subsequentIndex = propertyIndex + 1;
                    while (subsequentIndex < FieldPropertyOrder.Count)
                    {
                        insertBeforeElement = LoadChild(parentNode, FieldPropertyOrder[subsequentIndex]);
                        if (insertBeforeElement != null)
                            break;

                        subsequentIndex++;
                    }

                    if (insertBeforeElement != null)
                        parentNode.InsertBefore(childElement, insertBeforeElement);
                    else if (insertAfterElement != null)
                        parentNode.InsertAfter(childElement, insertAfterElement);
                    else
                        parentNode.AppendChild(childElement);
                }
                else
                {
                    parentNode.AppendChild(childElement);
                }

                //TODO: log!
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
