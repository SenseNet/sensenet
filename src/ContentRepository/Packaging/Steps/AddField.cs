using System;
using System.IO;
using System.Linq;
using System.Xml;
using SnSchema = SenseNet.ContentRepository.Schema;

namespace SenseNet.Packaging.Steps
{
    public class AddField : EditContentType
    {
        [DefaultProperty]
        public string FieldXml { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var contentType = LoadContentType();
            var contentTypeXmlDoc = LoadContentTypeXmlDocument(contentType);

            // check if the field is already defined in this CTD
            string fieldName;
            var fieldXml = ParseField(out fieldName);
            var fieldElement = LoadFieldElement(contentTypeXmlDoc, fieldName, false);
            if (fieldElement != null)
                throw new PackagingException(SR.Errors.ContentTypeSteps.FieldExists);

            // load the CTD as xml
            var ctd = LoadContentTypeString(contentType);

            // append the field
            var p = ctd.IndexOf("</Fields>");
            if (p < 0)
                throw new PackagingException(SR.Errors.ContentTypeSteps.InvalidFieldXml);

            // the action
            ctd = ctd.Insert(p, fieldXml);
            SnSchema.ContentTypeInstaller.InstallContentType(ctd);

            Logger.LogMessage("The content type '{0}' is extended with the '{1}' field.", this.ContentType, fieldName);
        }

        protected string ParseField(out string fieldName)
        {
            var fieldXml = this.FieldXml.Trim();
            if (fieldXml.StartsWith("<![CDATA[") && fieldXml.EndsWith("]]>"))
                fieldXml = fieldXml.Substring(9, fieldXml.Length - 12).Trim();

            var xml = new XmlDocument();
            xml.LoadXml(fieldXml);

            var nameAttr = xml.SelectSingleNode("/Field/@name") as XmlAttribute;
            if (nameAttr == null)
                throw new PackagingException(SR.Errors.ContentTypeSteps.InvalidField_NameNotFound);

            fieldName = nameAttr.Value;
            if (String.IsNullOrEmpty(fieldName))
                throw new PackagingException(SR.Errors.ContentTypeSteps.FieldNameCannotBeNullOrEmpty);

            return fieldXml;
        }
    }
}
