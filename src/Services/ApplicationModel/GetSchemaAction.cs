using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json.Linq;
using SenseNet.Diagnostics;

namespace SenseNet.Services.ApplicationModel
{
    public class SchemaProvider
    {
        //UNDONE: from TypescriptFormatter. ToDo: put this somewhere. const? web.config? Setting?
        private static readonly string[] ContentTypeBlacklist =
            {"Application", "ApplicationCacheFile", "FieldSettingContent", "JournalNode"};

        private const string SchemaProviderKey = "SchemaProvider";
        private static readonly object SchemaProviderSync = new object();

        static SchemaProvider()
        {
            // The cached schema elements here need to be dropped
            // when the content type system changes.
            ContentType.TypeSystemRestarted += (sender, args) => { Instance.Reset(); };
        }

        internal static SchemaProvider Instance
        {
            get
            {
                // ReSharper disable once InconsistentlySynchronizedField
                var schp = Providers.Instance.GetProvider<SchemaProvider>(SchemaProviderKey);
                if (schp == null)
                {
                    lock (SchemaProviderSync)
                    {
                        schp = Providers.Instance.GetProvider<SchemaProvider>(SchemaProviderKey);
                        if (schp == null)
                        {
                            schp = new SchemaProvider();
                            Providers.Instance.SetProvider(SchemaProviderKey, schp);
                        }
                    }
                }

                return schp;
            }
            set => Providers.Instance.SetProvider(SchemaProviderKey, value);
        }

        private readonly ConcurrentDictionary<string, JObject> _contentTypes = new ConcurrentDictionary<string, JObject>();

        /// <summary>
        /// Gets the serialized content type from the cache. If it is not there yet, it converts
        /// the content type to a cacheable format and inserts it into the cache.
        /// </summary>
        private JObject GetSerializedContentType(Class schemaClass)
        {
            if (_contentTypes.TryGetValue(schemaClass.Name, out var serializedContentType))
                return serializedContentType;

            var ctData = GetSerializableSchemaClass(schemaClass);

            // We cache JObjects instead of strings because this way the OData
            // layer sets the response content type correctly (application/json).
            return _contentTypes[schemaClass.Name] = JObject.FromObject(ctData);
        }

        private void Reset()
        {
            _contentTypes.Clear();
            SnTrace.Repository.Write("SchemaProvider Reset");
        }

        private static object GetSerializableSchemaClass(Class schemaClass)
        {
            // This method is responsible for constructing a serializable object
            // in a format appropriate for the client.

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

        // ======================================================================================= OData API

        [ODataFunction]
        public static object GetSchema(Content content)
        {
            var sch = new Schema(ContentTypeBlacklist);

            return sch.Classes.Select(c => Instance.GetSerializedContentType(c)).ToArray();
        }
    }
}