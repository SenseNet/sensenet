using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SNCS = SenseNet.ContentRepository.Schema;

namespace SenseNet.Packaging.Steps
{
    /// <summary>
    /// Base step class for making changes in a CTD. It contains helper methods used by specialized steps.
    /// </summary>
    public abstract class EditContentType : Step
    {
        protected static readonly string NAMESPACE = "x";

        public string ContentType { get; set; }
        public string InsertAfter { get; set; }
        public string InsertBefore { get; set; }

        protected SNCS.ContentType LoadContentType()
        {
            // check content type name
            if (string.IsNullOrEmpty(ContentType))
                throw new PackagingException(SR.Errors.ContentTypeSteps.InvalidContentTypeName);

            var contentType = SNCS.ContentType.GetByName(ContentType);
            if (contentType == null)
                throw new PackagingException(SR.Errors.ContentTypeSteps.ContentTypeNotFound);

            return contentType;
        }

        protected string LoadContentTypeString(SNCS.ContentType contentType)
        {
            using (var reader = new StreamReader(contentType.Binary.GetStream()))
                return reader.ReadToEnd();
        }

        protected XmlDocument LoadContentTypeXmlDocument()
        {
            var contentType = LoadContentType();
            return  LoadContentTypeXmlDocument(contentType);
        }

        protected internal static XmlDocument LoadContentTypeXmlDocument(SNCS.ContentType contentType)
        {
            var ctdXDoc = new XmlDocument();
            using (var ctdStream = contentType.Binary.GetStream())
                ctdXDoc.Load(ctdStream);

            return ctdXDoc;
        }

        protected static XmlNode LoadChild(XmlNode parent, string childName)
        {
            return parent.SelectSingleNode(string.Format("{0}:{1}", NAMESPACE, childName), GetNamespaceManager(parent.OwnerDocument));
        }

        protected internal static XmlElement LoadFieldElement(XmlDocument xDoc, string fieldName, bool throwOnError = true)
        {
            var fieldNode = xDoc.SelectSingleNode(string.Format("//{0}:Field[@name='{1}']", NAMESPACE, fieldName), GetNamespaceManager(xDoc)) as XmlElement;
            if (fieldNode == null && throwOnError)
                throw new PackagingException(string.Format(SR.Errors.Content.FieldNotFound_1, fieldName));

            return fieldNode;
        }

        internal static XmlNamespaceManager GetNamespaceManager(XmlDocument xDoc)
        {
            var nsmgr = new XmlNamespaceManager(xDoc.NameTable);
            nsmgr.AddNamespace(NAMESPACE, xDoc.DocumentElement.NamespaceURI);

            return nsmgr;
        }

        protected XmlNode LoadOrAddChild(XmlNode parentNode, string name, bool insertIfPossible = true, IDictionary<string, string> attributes = null)
        {
            var childElement = LoadChild(parentNode, name);
            if (childElement == null)
            {
                childElement = parentNode.OwnerDocument.CreateElement(name, parentNode.OwnerDocument.DocumentElement.NamespaceURI);

                if (insertIfPossible)
                {
                    XmlNode insertBeforeElement = null;
                    XmlNode insertAfterElement = null;

                    if (!string.IsNullOrEmpty(InsertBefore))
                        insertBeforeElement = LoadChild(parentNode, InsertBefore);
                    if (!string.IsNullOrEmpty(InsertAfter))
                        insertAfterElement = LoadChild(parentNode, InsertAfter);

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

                Logger.LogMessage("New xml element was created: " + name);
            }

            if (attributes != null && childElement is XmlElement)
            {
                foreach (var key in attributes.Keys)
                {
                    ((XmlElement)childElement).SetAttribute(key, attributes[key]);
                }
            }

            return childElement;
        }
    }

    /// <summary>
    /// Adds or edits one of the properties (Icon, AllowedChildTypes) in the header of a CTD.
    /// </summary>
    public class EditContentTypeHeader : EditContentType
    {
        public string PropertyName { get; set; }

        [DefaultProperty]
        public string InnerXml { get; set; }

        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(PropertyName))
                throw new SnNotSupportedException("Please provide a property name.");

            context.AssertRepositoryStarted();

            var xDoc = LoadContentTypeXmlDocument();

            // Edit the handler attribute. No other attribute is editable on de document element.
            if (string.Compare(PropertyName, "handler", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                xDoc.DocumentElement.SetAttribute("handler", InnerXml);
            }
            else
            {
                var propertyElement = LoadOrAddChild(xDoc.DocumentElement, PropertyName);
                propertyElement.InnerXml = InnerXml;
            }

            // apply changes
            SNCS.ContentTypeInstaller.InstallContentType(xDoc.OuterXml);
        }
    }

    /// <summary>
    /// Specialized step for editing field properties in a CTD. It is capable of adding/editing 
    /// simple properties like DisplayName.
    /// </summary>
    public class EditField : EditContentType
    {
        public string FieldName { get; set; }
        public string PropertyName { get; set; }

        [DefaultProperty]
        public string FieldXml { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var xDoc = LoadContentTypeXmlDocument();
            var parentElement = LoadParent(xDoc);

            if (string.IsNullOrEmpty(PropertyName)) 
                throw new SnNotSupportedException("Please provide a property name.");

            Logger.LogMessage("Editing content type {0}, field {1}...", ContentType, FieldName);

            var propertyElement = LoadOrAddChild(parentElement, PropertyName);
            propertyElement.InnerXml = FieldXml;

            // apply changes
            SNCS.ContentTypeInstaller.InstallContentType(xDoc.OuterXml);
        }

        /// <summary>
        /// Load or create the parent xml node that should contain the provided property (e.g. the Field or the Configuration element).
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        protected virtual XmlNode LoadParent(XmlDocument xDoc)
        {
            return LoadFieldElement(xDoc, FieldName);
        }
    }

    /// <summary>
    /// Specialized step for editing field configuration values in a CTD. It is capable of 
    /// adding/editing simple config values like ReadOnly or MinValue.
    /// </summary>
    public class EditFieldConfiguration : EditField
    {
        private static readonly string CONFIGURATION_NAME = "Configuration";

        protected override XmlNode LoadParent(XmlDocument xDoc)
        {
            var fieldElement = LoadFieldElement(xDoc, FieldName);
            var configNode = LoadOrAddChild(fieldElement, CONFIGURATION_NAME, false);

            return configNode;
        }
    }
}
