using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.OData.Metadata
{
    internal class SchemaGenerationContext
    {
        public List<EntityType> EntityTypes = new List<EntityType>();
        public List<EnumType> EnumTypes = new List<EnumType>();
        public List<ComplexType> ComplexTypes = new List<ComplexType>();
        public List<Association> Associations = new List<Association>();

        public IEnumerable<FieldSetting> ListFieldSettings;
        public bool Flattening;

        private readonly string[] _allowedContentTypeNames;

        public SchemaGenerationContext()
        {
            _allowedContentTypeNames = (new MetaClassEnumerable()).Select(c => c.Name).ToArray();
        }

        [Obsolete("Use 'IsPermittedType' method instead.", true)]
        // ReSharper disable once IdentifierTypo
        public bool IsPermitteType(ContentType contentType)
        {
            return IsPermittedType(contentType);
        }

        public bool IsPermittedType(ContentType contentType)
        {
            return _allowedContentTypeNames.Contains(contentType.Name);
        }
    }
}
