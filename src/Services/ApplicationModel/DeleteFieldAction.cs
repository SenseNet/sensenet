using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ApplicationModel
{
    public class DeleteFieldAction : UrlAction
    {
        public override bool IsHtmlOperation { get; } = true;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;

        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("name", typeof(string)) };

        public override object Execute(Content content, params object[] parameters)
        {
            var contentList = content.ContentHandler as ContentList;
            if (contentList == null)
                throw new InvalidOperationException("You cannot edit fields of any other content but a content list.");

            var fieldName = parameters != null && parameters.Length > 0 ? parameters[0] as string : null;
            if (string.IsNullOrEmpty(fieldName))
                throw new InvalidOperationException("Field Name is missing from post data.");

            if (!fieldName.StartsWith("#"))
                fieldName = "#" + fieldName;

            var fieldSetting = contentList.FieldSettingContents.FirstOrDefault(fs => string.CompareOrdinal(fs.Name, fieldName) == 0) as FieldSettingContent;
            if (fieldSetting == null)
                throw new InvalidOperationException("Unknown field: " + fieldName);

            // the content handler takes care of removing the column from views and clearing field values
            fieldSetting.Delete();

            return string.Empty;
        }
    }
}
