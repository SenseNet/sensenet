using System;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.i18n;
using SenseNet.Diagnostics;
using SenseNet.OData.Typescript;

namespace SenseNet.OData.Metadata
{
    /// <summary>
    /// Metadata provider with a built-in cache that converts content types 
    /// to a JObject format appropriate for clients.
    /// </summary>
    public class ClientMetadataProvider : IClientMetadataProvider
    {
        private const string ClientMetadataProviderKey = "ClientMetadataProvider";
        private static readonly object MetadataProviderSync = new object();

        //======================================================================================= Static API

        /// <summary>
        /// Singleton instance for serving metadata objects for clients. Tests may change 
        /// its value temporarily as it is built on the Providers API.
        /// </summary>
        internal static IClientMetadataProvider Instance
        {
            get
            {
                // ReSharper disable once InconsistentlySynchronizedField
                var cmdProvider = Providers.Instance.GetProvider<IClientMetadataProvider>(ClientMetadataProviderKey);
                if (cmdProvider == null)
                {
                    lock (MetadataProviderSync)
                    {
                        cmdProvider = Providers.Instance.GetProvider<IClientMetadataProvider>(ClientMetadataProviderKey);
                        if (cmdProvider == null)
                        {
                            // default implementation
                            cmdProvider = new ClientMetadataProvider();
                            Providers.Instance.SetProvider(ClientMetadataProviderKey, cmdProvider);
                        }
                    }
                }

                return cmdProvider;
            }
            set => Providers.Instance.SetProvider(ClientMetadataProviderKey, value);
        }

        //======================================================================================= Constructors

        internal ClientMetadataProvider()
        {
            // per-instance event subscription
            ContentType.TypeSystemRestarted += OnTypeSystemRestarted;
            SenseNetResourceManager.ResourceManagerRestarted += OnResourceManagerRestarted;
        }
        ~ClientMetadataProvider()
        {
            ContentType.TypeSystemRestarted -= OnTypeSystemRestarted;
            SenseNetResourceManager.ResourceManagerRestarted -= OnResourceManagerRestarted;
        }

        //======================================================================================= Event handlers

        /// <summary>
        /// Instance-level event handler that is called when the content type system restarts.
        /// </summary>
        protected virtual void OnTypeSystemRestarted(object o, EventArgs eventArgs)
        {
            Reset();
        }
        /// <summary>
        /// Instance-level event handler that is called when the resource manager restarts.
        /// </summary>
        protected virtual void OnResourceManagerRestarted(object o, EventArgs eventArgs)
        {
            Reset();
        }

        internal void Reset()
        {
            _contentTypes.Clear();
            SnTrace.Repository.Write("ClientMetadataProvider Reset");
        }

        //======================================================================================= Cache

        private readonly ConcurrentDictionary<string, JObject> _contentTypes = new ConcurrentDictionary<string, JObject>();
        
        //======================================================================================= IClientMetadataProvider implementation

        /// <summary>
        /// Gets the converted content type from the cache. If it is not cached yet, it converts
        /// the content type to a cacheable format and inserts it into the cache.
        /// </summary>
        public object GetClientMetaClass(Class schemaClass)
        {
            if (schemaClass == null)
                throw new ArgumentNullException(nameof(schemaClass));

            if (!_contentTypes.TryGetValue(schemaClass.Name, out var serializedContentType))
            {

                serializedContentType = ConvertMetaClass(schemaClass);

                // We cache JObjects instead of strings because this way the OData
                // layer sets the response content type correctly (application/json).
                _contentTypes[schemaClass.Name] = serializedContentType;
            }
            
            // We have to clone the cached value, because of default value evaluation
            // that needs to be on-the-fly and user-specific.
            var clone = (JObject)serializedContentType.DeepClone();

            foreach (var fs in clone["FieldSettings"].Values<JToken>())
            {
                // set evaluated default value only if there is a default value
                var defaultValue = fs.Value<string>("DefaultValue");
                if (string.IsNullOrEmpty(defaultValue)) 
                    continue;

                var evaluated = FieldSetting.EvaluateDefaultValue(defaultValue);
                fs["EvaluatedDefaultValue"] = evaluated;
            }

            return clone;
        }

        //======================================================================================= Instance API

        /// <summary>
        /// Converts a schema class to a cacheable and serializable object.
        /// </summary>
        protected virtual JObject ConvertMetaClass(Class schemaClass)
        {
            if (schemaClass == null)
                throw new ArgumentNullException(nameof(schemaClass));

            // wrap the content type into a content to get localized displayname etc.
            var contentTypeContent = Content.Create(schemaClass.ContentType);

            // do not serialize null values to preserve compute time and bandwidth
            var seralizer = JsonSerializer.Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            return JObject.FromObject(new
            {
                ContentTypeName = contentTypeContent.Name,
                contentTypeContent.DisplayName,
                contentTypeContent.Description,
                contentTypeContent.Icon,
                ParentTypeName = schemaClass.ContentType.ParentType?.Name,
                AllowIndexing = schemaClass.ContentType.IndexingEnabled,
                schemaClass.ContentType.AllowIncrementalNaming,
                AllowedChildTypes = schemaClass.ContentType.AllowedChildTypeNames,
                schemaClass.ContentType.HandlerName,
                FieldSettings = schemaClass.ContentType.FieldSettings
                    .Where(f => f.Owner == schemaClass.ContentType)
            }, seralizer);
        }

        //======================================================================================= OData API

        /// <summary></summary>
        /// <snCategory>Content and Schema</snCategory>
        /// <param name="content"></param>
        /// <param name="contentTypeName"></param>
        /// <returns></returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static object GetSchema(Content content, string contentTypeName = null)
        {
            var sch = new Schema(TypescriptGenerationContext.DisabledContentTypeNames);

            // If the content type name filter is provided, the result array contains
            // only that type. In the future it may contain parent content types too
            // if necessary.
            var classes = string.IsNullOrEmpty(contentTypeName)
                ? sch.Classes
                : sch.Classes.Where(c => c.Name == contentTypeName);

            return classes.Select(c => Instance.GetClientMetaClass(c)).ToArray();
        }
    }
}
