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
using SenseNet.Diagnostics;
using SenseNet.OData.Writers;
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
        }
        ~ClientMetadataProvider()
        {
            ContentType.TypeSystemRestarted -= OnTypeSystemRestarted;
        }

        //======================================================================================= Event handlers

        /// <summary>
        /// Instance-level event handler that is called when the content type system restarts.
        /// </summary>
        protected virtual void OnTypeSystemRestarted(object o, EventArgs eventArgs)
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

            if (_contentTypes.TryGetValue(schemaClass.Name, out var serializedContentType))
                return serializedContentType;

            var ctData = ConvertMetaClass(schemaClass);

            // We cache JObjects instead of strings because this way the OData
            // layer sets the response content type correctly (application/json).
            return _contentTypes[schemaClass.Name] = ctData;
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
                FieldSettings = schemaClass.ContentType.FieldSettings
                    .Where(f => f.Owner == schemaClass.ContentType)
            }, seralizer);
        }

        //======================================================================================= OData API

        [ODataFunction]
        [ContentTypes(N.PortalRoot)]
        [AllowedRoles(N.All)]
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