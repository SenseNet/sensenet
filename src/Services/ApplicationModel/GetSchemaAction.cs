using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Services.ApplicationModel
{
    public class GetSchemaAction : ActionBase
    {

        // from TypescriptGenerationContext. ToDo: put this somewhere. const? web.config? Setting?
        private readonly string[] PropertyBlacklist = { "Type", "TypeIs", "InTree", "InFolder", "AllFieldSettingContents" };

        // from TypescriptModuleWriter. ToDo: put this somewhere. const? web.config? Setting?
        private readonly string[] FieldSettingPropertyBlackList =
        {
            "Aspect", "ShortName", "FieldClassName", "DisplayNameStoredValue", "DescriptionStoredValue", "Bindings",
            "IsRerouted", "Owner", "ParentFieldSetting", "FullName", "BindingName", "IndexingInfo", "OutputMethod",
            "Visible", "LocalizationEnabled", "FieldDataType"
        };

        // from TypescriptFormatter. ToDo: put this somewhere. const? web.config? Setting?
        public readonly string[] ContentTypeBlacklist = new[]
                {"Application", "ApplicationCacheFile", "FieldSettingContent", "JournalNode"};

        private object getSerializableFieldSetting(FieldSetting fieldSetting)
        {
            var settings = new Dictionary<string, object> { };

            settings.Add("Type", fieldSetting.GetType().Name);

            var propertyInfos = fieldSetting.GetType().GetProperties()
                    .Where(p => !FieldSettingPropertyBlackList.Contains(p.Name));

            foreach (var propertyInfo in propertyInfos)
            {
                var name = propertyInfo.Name;
                var value = propertyInfo.GetValue(fieldSetting);
                if (value != null)
                    settings.Add(name, value);

            }

            // ToDo: serialize nested dictionary in a more straightforward way
            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(settings));
        }

        private object getSerializableSchemaClass(Class schemaClass)
        {
            return new
            {
                ContentTypeName = schemaClass.ContentType.Name,
                schemaClass.ContentType.DisplayName,
                schemaClass.ContentType.Description,
                schemaClass.ContentType.Icon,
                ParentTypeName = schemaClass.ContentType.ParentType?.Name,
                AllowIndexing = schemaClass.ContentType.IndexingEnabled,
                schemaClass.ContentType.AllowIncrementalNaming,
                AllowedChildTypes = schemaClass.ContentType.AllowedChildTypeNames,
                FieldSettings = schemaClass.ContentType.FieldSettings
                          .Where(f => f.Owner == schemaClass.ContentType)
                          .Select(f => getSerializableFieldSetting(f))
            };
        }


        public override string Uri => null;

        public override bool IsHtmlOperation { get { return false; } }
        public override bool IsODataOperation { get { return true; } }
        public override bool CausesStateChange { get { return false; } }

        public override object Execute(Content content, params object[] parameters)
        {
            var serializerSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            var schema = new Schema(ContentTypeBlacklist)
                .Classes.Select(ct => getSerializableSchemaClass(ct));
            return schema;
        }
    }
}
