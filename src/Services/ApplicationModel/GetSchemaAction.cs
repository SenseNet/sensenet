using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema.Metadata;
using System.Linq;

namespace SenseNet.Services.ApplicationModel
{
    public class SchemaProvider
    {
        //UNDONE: from TypescriptFormatter. ToDo: put this somewhere. const? web.config? Setting?
        public static readonly string[] ContentTypeBlacklist =
            {"Application", "ApplicationCacheFile", "FieldSettingContent", "JournalNode"};

        private static object GetSerializableSchemaClass(Class schemaClass)
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
            };
        }

        [ODataFunction]
        public static object GetSchema(Content content)
        {
            return new Schema(ContentTypeBlacklist).Classes.Select(GetSerializableSchemaClass);
        }
    }
}
