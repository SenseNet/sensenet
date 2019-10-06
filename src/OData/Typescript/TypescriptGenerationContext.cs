using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.OData.Typescript
{
    internal class TypescriptGenerationContext
    {
        public const string ComplexTypesModuleName = "ComplexTypes";
        public const string EnumTypesModuleName = "Enums";
        public const string SchemaModuleName = "Schemas";
        public const string FieldSettingsModuleName = "FieldSettings";

        public Dictionary<string, string> EmittedEnumerationNames = new Dictionary<string, string>();
        public List<Enumeration> Enumerations = new List<Enumeration>();
        public List<ComplexType> ComplexTypes = new List<ComplexType>();

        public static readonly string[] DisabledContentTypeNames = { "Application", "ApplicationCacheFile", "FieldSettingContent", "JournalNode" };
        public static readonly string[] PropertyBlacklist = { "Type", "TypeIs", "InTree", "InFolder", "AllFieldSettingContents" };

        public string GetEnumerationFullName(Enumeration enumeration)
        {
            return enumeration.Class.Name + enumeration.FieldSetting.Name;
        }
    }
}
