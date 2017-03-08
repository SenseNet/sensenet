using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SCS = SenseNet.ContentRepository.Schema;

namespace SenseNet.Packaging.Steps
{
    public class ChangeFieldType : AddField
    {
        private static readonly string NAMESPACE_PATTERN = @"\sxmlns=""[^""]+""";
        private static readonly string FIELD_TYPE_PATTERN = @"\stype=""[^""]+""";
        private static readonly string FIELD_TYPE = @" type=""{0}""";

        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public string ArchiveFieldName { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var contentType = LoadContentType();

            // either the old field name or the new fragment should be provided
            if (string.IsNullOrEmpty(FieldName) && string.IsNullOrEmpty(FieldXml))
                throw new PackagingException(SR.Errors.InvalidParameters);

            // load the CTD as xml
            var ctdXDoc = LoadContentTypeXmlDocument(contentType);

            XmlElement fieldNode = null;

            // if the old field name is provided, it should exist in the CTD
            if (!string.IsNullOrEmpty(FieldName))
            {
                fieldNode = LoadFieldElement(ctdXDoc, FieldName);

                // if the field xml was not provided in the manifest, we have to use the old field's xml fragment
                if (string.IsNullOrEmpty(FieldXml))
                {
                    // in this case the new field type must be provided in the manifest
                    if (string.IsNullOrEmpty(FieldType))
                        throw new PackagingException(SR.Errors.InvalidParameters);

                    // change the old type to the new in the xml fragment and set the new field xml 
                    FieldXml = Regex.Replace(fieldNode.OuterXml, FIELD_TYPE_PATTERN, string.Format(FIELD_TYPE, FieldType));

                    // remove the unnecessary namespace attribute
                    FieldXml = Regex.Replace(FieldXml, NAMESPACE_PATTERN, string.Empty);
                }
            }

            // determine the new field name
            string fieldName;
            ParseField(out fieldName);

            // in most cases the old field name is the same as the new one, provided in the field xml fragment
            if (string.IsNullOrEmpty(FieldName))
            {
                FieldName = fieldName;

                // load the old field xml node from the CTD if we did not load it before
                if (fieldNode == null)
                    fieldNode = LoadFieldElement(ctdXDoc, FieldName);
            }

            // load the old fieldsetting to determine its type
            var fs = contentType.GetFieldSettingByName(FieldName);
            if (fs == null)
                throw new PackagingException(string.Format(SR.Errors.Content.FieldNotFound_1, FieldName));

            Logger.LogMessage("Collecting old values...");

            var oldValues = new Dictionary<int, object>();
            var skipZero = fs is NumberFieldSetting;
            var skipEmptyText = fs is ShortTextFieldSetting;

            // iterate through existing content items and memorize values
            foreach (var content in Content.All.DisableAutofilters().Where(c => c.TypeIs(ContentType)))
            {
                var val = content[FieldName];
                if (val == null)
                    continue;

                // it does not make sense to re-save a content if the value is 0 (it is similar to a 'null' value in case of number fields)
                if (skipZero)
                {
                    double numVal;
                    if (double.TryParse(val.ToString(), out numVal) && numVal.CompareTo(0d) == 0)
                        continue;
                }

                // skip empty text in case of text fields
                if (skipEmptyText && val is string && string.IsNullOrEmpty((string)val))
                    continue;

                oldValues[content.Id] = val;
            }

            // remove field from the CTD
            fieldNode.ParentNode.RemoveChild(fieldNode);

            // re-register the content type
            SCS.ContentTypeInstaller.InstallContentType(ctdXDoc.OuterXml);

            Logger.LogMessage("{0} field was removed from content type {1}.", FieldName, ContentType);
            
            // add new field
            base.Execute(context);

            if (oldValues.Count > 0)
            {
                Logger.LogMessage("Migrating values...");

                // If an archive field is provided, use that instead of the newly added field.
                // (e.g. migrating shorttext values is not possible if the new field is a reference field)
                var targetFieldName = !string.IsNullOrEmpty(ArchiveFieldName) ? ArchiveFieldName : fieldName;

                // iterate cached ids and set old values to the new field
                foreach (var content in Node.LoadNodes(oldValues.Keys).Select(Content.Create))
                {
                    Logger.LogMessage("Saving content {0}", content.Path);

                    try
                    {
                        // if a migration fails on a single content, we only log it. Patch log should be reviewed by the operator!
                        content[targetFieldName] = oldValues[content.Id];
                        content.SaveSameVersion();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarningMessage(string.Format("Error during migrating {0}. Message: {1}", content.Path, ex.Message));
                    }
                }
            }

            Logger.LogMessage("{0} content were changed.", oldValues.Count);
        }
    }
}
