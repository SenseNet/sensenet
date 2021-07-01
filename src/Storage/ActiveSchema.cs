using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    //UNDONE:<?schema: rename ActiveSchema -> StorageSchema
    /// <summary>
    /// ActiveSchema is a wrapper for NodeTypeManager. By using the ActiveSchema class you can the the NodeTypes, PropertyTypes and PermissionTypes currently in the system.
    /// </summary>
	public class ActiveSchema
    {
        public static readonly List<string> NodeAttributeNames = new List<string>(new string[]{
            "Id", "Parent", "Name", "Path",
			"Index", "Locked", "LockedBy", "ETag", "LockType", "LockTimeout", "LockDate", "LockToken",
            "LastLockUpdate", "LastMinorVersionId", "LastMajorVersionId", "MajorVersion", "MinorVersion",
            "CreationDate", "CreatedBy", "ModificationDate", "ModifiedBy", "IsSystem", "OwnerId", "SavingState" });

        private static IDataStore DataStore => Providers.Instance.DataStore;

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
        /// Gets property types that belongs to a NodeType and a ContentListType combination.
        /// If the <paramref name="nodeTypeId"/> is less than 1, the result is empty.
        /// The <paramref name="contentListTypeId"/> can be less than 1 if it is irrelevant.
        /// </summary>
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


        /* ============================================================================================ Instance management */

        #region Distributed Action child class
        [Serializable]
        internal class NodeTypeManagerRestartDistributedAction : SenseNet.Communication.Messaging.DistributedAction
        {
            public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return Task.CompletedTask;

                RestartPrivate();

                return Task.CompletedTask;
            }
        }
        #endregion

        private static NodeTypeManager _nodeTypeManager;
        private static readonly object _lock = new object();
        internal static NodeTypeManager NodeTypeManager
        {
            get
            {
                if (_nodeTypeManager == null)
                {
                    lock (_lock)
                    {
                        if (_nodeTypeManager == null)
                        {
                            LoadPrivate();
                        }
                    }
                }
                return _nodeTypeManager;
            }
        }

        /// <summary>
        /// Reloads the storage schema without sending any distributed action.
        /// </summary>
        public static void Reload()
        {
            RestartPrivate();
        }

        /// <summary>
        /// Reloads the storage schema and distributes a NodeTypeManagerRestart action.
        /// </summary>
        internal static void Reset()
        {
            SnLog.WriteInformation("NodeTypeManager.Restart called.", EventId.RepositoryRuntime,
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });
            new NodeTypeManagerRestartDistributedAction().ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Restarts the NodeTypeManager without sending any distributed action.
        /// Do not call this method explicitly, the system will call it if necessary (when the reset is triggered by an another instance).
        /// </summary>
        private static void RestartPrivate()
        {
            SnLog.WriteInformation("NodeTypeManager.Restart executed.", EventId.RepositoryRuntime,
                properties: new Dictionary<string, object> { { "AppDomain", AppDomain.CurrentDomain.FriendlyName } });
            NodeObserver.FireOnReset();

            lock (_lock)
            {
                Providers.Instance.DataStore.Reset();
                LoadPrivate();
            }
        }

        private static void LoadPrivate()
        {
            // this method must be called inside a lock block!
            var current = new NodeTypeManager();
            current.Load();

            _nodeTypeManager = current;

            NodeObserver.FireOnStart();
            SnLog.WriteInformation("NodeTypeManager created: " + _nodeTypeManager);
        }

    }
}