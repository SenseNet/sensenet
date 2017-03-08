using System;
namespace SenseNet.ContentRepository.Storage.Schema
{
    public abstract class PropertySet : SchemaItem
    {
        private TypeCollection<PropertyType> _propertyTypes;
        public TypeCollection<PropertyType> PropertyTypes
        {
            get { return _propertyTypes; }
        }


        internal PropertySet(int id, string name, ISchemaRoot schemaRoot) : base(schemaRoot, name, id)
        {
            _propertyTypes = new TypeCollection<PropertyType>(schemaRoot);
        }


        internal abstract void AddPropertyType(PropertyType propertyType);
        internal abstract void RemovePropertyType(PropertyType propertyType);

    }
}