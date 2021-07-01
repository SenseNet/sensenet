using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;
using System.ComponentModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Storage
{
    //UNDONE:<?schema: rename ActiveSchema -> StorageSchema
    /// <summary>
    /// ActiveSchema is a wrapper for NodeTypeManager. By using the ActiveSchema class you can the the NodeTypes, PropertyTypes and PermissionTypes currently in the system.
    /// </summary>
	public static class ActiveSchema
    {
        public static readonly List<string> NodeAttributeNames = new List<string>(new string[]{
            "Id", "Parent", "Name", "Path",
			"Index", "Locked", "LockedBy", "ETag", "LockType", "LockTimeout", "LockDate", "LockToken",
            "LastLockUpdate", "LastMinorVersionId", "LastMajorVersionId", "MajorVersion", "MinorVersion",
            "CreationDate", "CreatedBy", "ModificationDate", "ModifiedBy", "IsSystem", "OwnerId", "SavingState" });

        private static IDataStore DataStore => Providers.Instance.DataStore;
        internal static NodeTypeManager NodeTypeManager => NodeTypeManager.Current;

        /// <summary>
        /// Gets the DataProvider dependent earliest DateTime value
        /// </summary>
        public static DateTime DateTimeMinValue => DataStore.DateTimeMinValue;

        /// <summary>
        /// Gets the DataProvider dependent last DateTime value
        /// </summary>
        public static DateTime DateTimeMaxValue => DataStore.DateTimeMaxValue;

        /// <summary>
        /// Gets the maximum length of the short text datatype
        /// </summary>
        public static int ShortTextMaxLength { get { return 400; } }

        /// <summary>
        /// Gets the DataProvider dependent smallest decimal value
        /// </summary>
        public static decimal DecimalMinValue => DataStore.DecimalMinValue;

        /// <summary>
        /// Gets the DataProvider dependent biggest decimal value
        /// </summary>
        public static decimal DecimalMaxValue => DataStore.DecimalMaxValue;


        /// <summary>
        /// Gets the property types.
        /// </summary>
        /// <value>The property types.</value>
		public static TypeCollection<PropertyType> PropertyTypes => NodeTypeManager.PropertyTypes;

        /// <summary>
        /// Gets the node types.
        /// </summary>
        /// <value>The node types.</value>
		public static TypeCollection<NodeType> NodeTypes => NodeTypeManager.NodeTypes;

        /// <summary>
        /// Gets the ContentList types.
        /// </summary>
        /// <value>The ContentList types.</value>
        public static TypeCollection<ContentListType> ContentListTypes => NodeTypeManager.ContentListTypes;

        /// <summary>
        /// Resets the NodeTypeManager instance.
        /// </summary>
		public static void Reset()
        {
            // The NodeTypeManager distributes its restart, no distrib action needed
            NodeTypeManager.Restart();
        }

        public static void Reload()
        {
            NodeTypeManager.Reload();
        }

        public static TypeCollection<PropertyType> GetDynamicSignature(int nodeTypeId, int contentListTypeId)
        {
            var nodeType = NodeTypes.GetItemById(nodeTypeId);
            if (nodeType == null)
                return new TypeCollection<PropertyType>(NodeTypeManager);

            var nodePropertyTypes = nodeType.PropertyTypes;
            var allPropertyTypes = new TypeCollection<PropertyType>(nodePropertyTypes);
            if (contentListTypeId > 0)
                allPropertyTypes.AddRange(ContentListTypes.GetItemById(contentListTypeId).PropertyTypes);

            return allPropertyTypes;
        }

    }
}