using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.Portal.OData;

namespace SenseNet.ApplicationModel
{
    public class EditFieldAction : UrlAction
    {
        public override bool IsHtmlOperation { get; } = true;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;

        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter(null, null) };

        public override object Execute(Content content, params object[] parameters)
        {
            var postData = parameters.FirstOrDefault() as string;
            if (string.IsNullOrEmpty(postData))
                return null;

            var contentList = content.ContentHandler as ContentList;
            if (contentList == null)
                throw new InvalidOperationException("You cannot edit fields of any other content but a content list.");
            
            var model = ODataHandler.Read(postData);
            if (model == null)
                throw new InvalidOperationException("Invalid post data");

            var fieldName = model.Value<string>("Name");
            if (string.IsNullOrEmpty(fieldName))
                throw new InvalidOperationException("Field Name is missing from post data.");

            if (!fieldName.StartsWith("#"))
                fieldName = "#" + fieldName;

            var fieldSetting = contentList.FieldSettingContents.FirstOrDefault(fs => string.CompareOrdinal(fs.Name, fieldName) == 0);
            if (fieldSetting == null)
                throw new InvalidOperationException("Unknown field: " + fieldName);

            var fsContent = Content.Create(fieldSetting);

            ODataHandler.UpdateFields(fsContent, model);

            fsContent.Save();

            return fsContent;
        }
    }
}
