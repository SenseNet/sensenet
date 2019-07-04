using System;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.DataModel;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Schema
{
    public abstract class SchemaWriter
    {
        protected const int NodeTypeSchemaId = 1;
        protected const int ContentListTypeSchemaId = 2;

        public virtual bool CanWriteDifferences => true;

        public virtual Task WriteSchemaAsync(RepositorySchemaData schema) //UNDONE:DB@@@@@ Rewrite to abstract?
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Derived classes need to start a new transactional session.
        /// </summary>
        public abstract void Open();
        /// <summary>
        /// Derived classes need to commit the transactional session.
        /// </summary>
        public abstract void Close();

        // ============================ PropertySlot

        public abstract void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty);
        /// <summary>
        /// When overridden in a derived class, deletes an unused existing PropertyType
        /// </summary>
        /// <param name="propertyType">Unused existing PropertyType</param>
        public abstract void DeletePropertyType(PropertyType propertyType);

        // ============================ NodeType

        public abstract void CreateNodeType(NodeType parent, string name, string className);
        public abstract void ModifyNodeType(NodeType nodeType, NodeType parent, string className);
        /// <summary>
        /// When overridden in a derived class, deletes the passed NodeType.
        /// Before NodeType deleting removes all PropertyTypes from the passed NodeType but does not reset the 
        /// property values because all nodes instatiated by passed NodeType had been deleted.
        /// </summary>
        /// <param name="nodeType">NodeType to delete</param>
        public abstract void DeleteNodeType(NodeType nodeType);

        // ============================ ContentListType

        public abstract void CreateContentListType(string name);
        public abstract void DeleteContentListType(ContentListType contentListType);

        // ============================ PropertyType assignment

        public abstract void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared);
        /// <summary>
        /// When overridden in a derived class, removes the PropertyType from the owner PropertySet and resets the
        /// property values into all nodes instatiated by passed PropertySet.
        /// </summary>
        /// <param name="propertyType">PropertyType to remove</param>
        /// <param name="owner">Owner PropertySet</param>
        public abstract void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner);
        public abstract void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared);

    }
}