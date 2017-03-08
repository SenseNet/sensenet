using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace SenseNet.Packaging.Steps
{
    /// <summary>
    /// Simple step for changing a field type in a ContentType definition xml. It is able
    /// to handle only compatible field types as it replaces the field type without making
    /// any migration or field remove/add operations.
    /// </summary>
    public class ChangeFieldTypeInCTD : ChangeFieldType
    {
        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            // field name and new type must be provided
            if (string.IsNullOrEmpty(FieldName) || string.IsNullOrEmpty(FieldType))
                throw new PackagingException(SR.Errors.InvalidParameters);

            if (!string.IsNullOrEmpty(ArchiveFieldName))
                throw new PackagingException("ArchiveFieldName must NOT be provided in this step.");
            if (!string.IsNullOrEmpty(FieldXml))
                throw new PackagingException("FieldXml must NOT be provided in this step.");

            var ctdXDoc = LoadContentTypeXmlDocument();
            var fieldElement = LoadFieldElement(ctdXDoc, FieldName);
            var oldType = fieldElement.Attributes["type"].Value;

            fieldElement.Attributes["type"].Value = FieldType;

            context.Console.WriteLine("Changing the type of the field {0} from {1} to {2}...", FieldName, oldType, FieldType);

            // re-register the content type
            ContentRepository.Schema.ContentTypeInstaller.InstallContentType(ctdXDoc.OuterXml);
        }
    }
}
