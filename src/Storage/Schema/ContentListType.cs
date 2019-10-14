using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Schema
{
	public class ContentListType : PropertySet
	{
		internal ContentListType(int id, string name, ISchemaRoot schemaRoot) : base(id, name, schemaRoot) { }

		internal override void AddPropertyType(PropertyType propertyType)
		{
			if (!propertyType.IsContentListProperty)
                throw new SchemaEditorCommandException(String.Concat("Only ContentListProperty can be assigned to a ContentListType. ContentListType=", this.Name, ", PropertyType=", propertyType.Name));
			if (!this.PropertyTypes.Contains(propertyType))
				this.PropertyTypes.Add(propertyType);
		}
		internal override void RemovePropertyType(PropertyType propertyType)
		{
			this.PropertyTypes.Remove(propertyType);
		}

		public static ContentListType GetByName(string contentListTypeName)
		{
            return NodeTypeManager.Current.ContentListTypes[contentListTypeName];
		}
	}
}