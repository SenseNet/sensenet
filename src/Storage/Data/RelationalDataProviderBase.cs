﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using SenseNet.Storage.DataModel.Usage;

// ReSharper disable AccessToDisposedClosure

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <inheritdoc />
    /// <summary>
    /// Data provider base class for relational databases.
    /// </summary>
    public abstract class RelationalDataProviderBase : DataProvider
    {
        protected int IndexBlockSize = 100;

        public virtual IDataPlatform<DbConnection, DbCommand, DbParameter> GetPlatform() { return null; } //TODO:~ UNDELETABLE

        /// <summary>
        /// Constructs a platform-specific context that is able to hold transaction- and connection-related information.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A new data context object.</returns>
        public abstract SnDataContext CreateDataContext(CancellationToken cancellationToken);
        /* =============================================================================================== Nodes */
        
        public override async Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            CancellationToken cancellationToken)
        {
            try
            {
                var needToCleanupFiles = false;
                using (var ctx = CreateDataContext(cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        // Insert new rows int Nodes and Versions tables
                        var _ = await ctx.ExecuteReaderAsync(InsertNodeAndVersionScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                                ctx.CreateParameter("@NodeTypeId", DbType.Int32, nodeHeadData.NodeTypeId),
                                ctx.CreateParameter("@ContentListTypeId", DbType.Int32, nodeHeadData.ContentListTypeId > 0 ? (object) nodeHeadData.ContentListTypeId : DBNull.Value),
                                ctx.CreateParameter("@ContentListId", DbType.Int32, nodeHeadData.ContentListId > 0 ? (object) nodeHeadData.ContentListId : DBNull.Value),
                                ctx.CreateParameter("@CreatingInProgress", DbType.Byte, nodeHeadData.CreatingInProgress ? (byte) 1 : 0),
                                ctx.CreateParameter("@IsDeleted", DbType.Byte, nodeHeadData.IsDeleted ? (byte) 1 : 0),
                                ctx.CreateParameter("@IsInherited", DbType.Byte, (byte) 0),
                                ctx.CreateParameter("@ParentNodeId", DbType.Int32, nodeHeadData.ParentNodeId),
                                ctx.CreateParameter("@Name", DbType.String, 450, nodeHeadData.Name),
                                ctx.CreateParameter("@DisplayName", DbType.String, 450, (object)nodeHeadData.DisplayName ?? DBNull.Value),
                                ctx.CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, nodeHeadData.Path),
                                ctx.CreateParameter("@Index", DbType.Int32, nodeHeadData.Index),
                                ctx.CreateParameter("@Locked", DbType.Byte, nodeHeadData.Locked ? (byte) 1 : 0),
                                ctx.CreateParameter("@LockedById", DbType.Int32, nodeHeadData.LockedById > 0 ? (object) nodeHeadData.LockedById : DBNull.Value),
                                ctx.CreateParameter("@ETag", DbType.AnsiString, 50, nodeHeadData.ETag ?? string.Empty),
                                ctx.CreateParameter("@LockType", DbType.Int32, nodeHeadData.LockType),
                                ctx.CreateParameter("@LockTimeout", DbType.Int32, nodeHeadData.LockTimeout),
                                ctx.CreateParameter("@LockDate", DbType.DateTime2, nodeHeadData.LockDate),
                                ctx.CreateParameter("@LockToken", DbType.AnsiString, 50, nodeHeadData.LockToken ?? string.Empty),
                                ctx.CreateParameter("@LastLockUpdate", DbType.DateTime2, nodeHeadData.LastLockUpdate),
                                ctx.CreateParameter("@NodeCreationDate", DbType.DateTime2, nodeHeadData.CreationDate),
                                ctx.CreateParameter("@NodeCreatedById", DbType.Int32, nodeHeadData.CreatedById),
                                ctx.CreateParameter("@NodeModificationDate", DbType.DateTime2, nodeHeadData.ModificationDate),
                                ctx.CreateParameter("@NodeModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                                ctx.CreateParameter("@IsSystem", DbType.Byte, nodeHeadData.IsSystem ? (byte) 1 : 0),
                                ctx.CreateParameter("@OwnerId", DbType.Int32, nodeHeadData.OwnerId),
                                ctx.CreateParameter("@SavingState", DbType.Int32, (int)nodeHeadData.SavingState),
                                ctx.CreateParameter("@ChangedData", DbType.String, int.MaxValue, JsonConvert.SerializeObject(versionData.ChangedData)),
                                ctx.CreateParameter("@MajorNumber", DbType.Int16, (short)versionData.Version.Major),
                                ctx.CreateParameter("@MinorNumber", DbType.Int16, (short)versionData.Version.Minor),
                                ctx.CreateParameter("@Status", DbType.Int16, (short)versionData.Version.Status),
                                ctx.CreateParameter("@VersionCreationDate", DbType.DateTime2, versionData.CreationDate),
                                ctx.CreateParameter("@VersionCreatedById", DbType.Int32, nodeHeadData.CreatedById),
                                ctx.CreateParameter("@VersionModificationDate", DbType.DateTime2, versionData.ModificationDate),
                                ctx.CreateParameter("@VersionModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                                ctx.CreateParameter("@DynamicProperties", DbType.String, int.MaxValue, SerializeDynamicProperties(dynamicData.DynamicProperties)),
                                ctx.CreateParameter("@ContentListProperties", DbType.String, int.MaxValue, SerializeDynamicProperties(dynamicData.ContentListProperties)),
                            });
                        }, async (reader, cancel) =>
                        {
                            cancel.ThrowIfCancellationRequested();
                            if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                            {
                                nodeHeadData.NodeId = reader.GetInt32("NodeId");
                                nodeHeadData.Timestamp = reader.GetSafeLongFromBytes("NodeTimestamp");
                                nodeHeadData.LastMajorVersionId = reader.GetSafeInt32("LastMajorVersionId");
                                nodeHeadData.LastMinorVersionId = reader.GetInt32("LastMinorVersionId");
                                versionData.VersionId = reader.GetInt32("VersionId");
                                versionData.Timestamp = reader.GetSafeLongFromBytes("VersionTimestamp");
                            }
                            else
                            {
                                throw new DataException("Node was not saved. The InsertNodeAndVersion script returned nothing.");
                            }
                            return true;
                        }).ConfigureAwait(false);

                        var versionId = versionData.VersionId;

                        // Manage ReferenceProperties
                        if (dynamicData.ReferenceProperties.Any())
                            await InsertReferencePropertiesAsync(dynamicData.ReferenceProperties, versionId, ctx).ConfigureAwait(false);

                        // Manage LongTextProperties
                        if (dynamicData.LongTextProperties.Any())
                            await InsertLongTextPropertiesAsync(dynamicData.LongTextProperties, versionId, ctx).ConfigureAwait(false);

                        // Manage BinaryProperties
                        foreach (var item in dynamicData.BinaryProperties)
                            await SaveBinaryPropertyAsync(item.Value, versionId, item.Key.Id, true, true, ctx).ConfigureAwait(false);

                        transaction.Commit();
                    }
                    needToCleanupFiles = ctx.NeedToCleanupFiles;
                }

                if (needToCleanupFiles)
                    await BlobStorage.DeleteOrphanedFilesAsync(cancellationToken);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                var msg = "Node was not saved. For more details see the inner exception.";
                var transformedException = GetException(e, msg);
                if (transformedException != null)
                    throw transformedException;
                throw new DataException(msg, e);
            }
        }
        /// <summary>
        /// Combines dynamic properties into a single value before saving.
        /// </summary>
        /// <param name="properties">A collection of dynamic properties.</param>
        /// <returns>A string containing all dynamic property names and values in a serialized format.</returns>
        public virtual string SerializeDynamicProperties(IDictionary<PropertyType, object> properties)
        {
            var lines = properties
                .Select(x => SerializeDynamicProperty(x.Key, x.Value))
                .Where(x => x != null)
                .ToArray();
            return $"\r\n{string.Join("\r\n", lines)}\r\n";
        }
        /// <summary>
        /// Serializes a single dynamic property.
        /// </summary>
        /// <param name="propertyType">Property type.</param>
        /// <param name="propertyValue">Value of the property.</param>
        /// <returns>A serialized dynamic property name and value pair.</returns>
        protected virtual string SerializeDynamicProperty(PropertyType propertyType, object propertyValue)
        {
            if (propertyValue == null)
                return null;

            string value;
            switch (propertyType.DataType)
            {
                // DataType.Text and Binary types are not used here
                case DataType.String:
                    value = ((string)propertyValue).Replace("\\", "\\\\").Replace("\r\n", "\\r\\n");
                    break;
                case DataType.DateTime:
                    value = ((DateTime)propertyValue).ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                    break;
                case DataType.Reference:
                    value = string.Join(",", ((IEnumerable<int>)propertyValue).Select(x => x.ToString()));
                    if (value.Length == 0)
                        return null; // Do not provide empty value.
                    break;
                case DataType.Int: // because of enums
                    value = Convert.ToString((int)propertyValue, CultureInfo.InvariantCulture);
                    break;
                default:
                    value = Convert.ToString(propertyValue, CultureInfo.InvariantCulture);
                    break;
            }
            return $"{propertyType.Name}:{value}";
        }
        protected abstract string InsertNodeAndVersionScript { get; }
        protected virtual async Task InsertReferencePropertiesAsync(IDictionary<PropertyType, List<int>> referenceProperties, int versionId, SnDataContext ctx)
        {
            var parameters = new List<DbParameter> {ctx.CreateParameter("@VersionId", DbType.Int32, versionId)};
            var sqlBuilder = new StringBuilder(InsertReferencePropertiesHeadScript);
            var index = 0;
            foreach (var item in referenceProperties)
            {
                index++;
                sqlBuilder.AppendFormat(InsertReferencePropertiesScript, index);
                parameters.Add(ctx.CreateParameter("@PropertyTypeId" + index, DbType.Int32, item.Key.Id));
                parameters.Add(ctx.CreateParameter("@ReferredNodeIds" + index, DbType.String, int.MaxValue, string.Join(",", item.Value.Select(x => x.ToString()))));
            }
            await ctx.ExecuteNonQueryAsync(sqlBuilder.ToString(), cmd =>
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }).ConfigureAwait(false);
        }
        protected abstract string InsertReferencePropertiesHeadScript { get; }
        protected abstract string InsertReferencePropertiesScript { get; }
        protected virtual async Task InsertLongTextPropertiesAsync(IDictionary<PropertyType, string> longTextProperties, int versionId, SnDataContext ctx)
        {
            var longTextSqlBuilder = new StringBuilder();
            var longTextSqlParameters = new List<DbParameter>();
            var index = 0;
            longTextSqlBuilder.Append(InsertLongtextPropertiesHeadScript);
            longTextSqlParameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
            foreach (var item in longTextProperties.Where(x => x.Value != null))
            {
                longTextSqlBuilder.AppendFormat(InsertLongtextPropertiesScript, ++index);
                longTextSqlParameters.Add(ctx.CreateParameter("@PropertyTypeId" + index, DbType.Int32, item.Key.Id));
                longTextSqlParameters.Add(ctx.CreateParameter("@Length" + index, DbType.Int32, item.Value.Length));
                longTextSqlParameters.Add(ctx.CreateParameter("@Value" + index, DbType.String, int.MaxValue, item.Value));
            }
            await ctx.ExecuteNonQueryAsync(longTextSqlBuilder.ToString(),
                cmd => { cmd.Parameters.AddRange(longTextSqlParameters.ToArray()); }).ConfigureAwait(false);
        }
        protected abstract string InsertLongtextPropertiesHeadScript { get; }
        protected abstract string InsertLongtextPropertiesScript { get; }

        /// <inheritdoc />
        public override async Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken, string originalPath = null)
        {
            try
            {
                var needToCleanupFiles = false;
                using (var ctx = CreateDataContext(cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        var versionId = versionData.VersionId;

                        // Update version
                        var rawVersionTimestamp = await ctx.ExecuteScalarAsync(UpdateVersionScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                                ctx.CreateParameter("@VersionId", DbType.Int32, versionData.VersionId),
                                ctx.CreateParameter("@NodeId", DbType.Int32, versionData.NodeId),
                                ctx.CreateParameter("@MajorNumber", DbType.Int16, versionData.Version.Major),
                                ctx.CreateParameter("@MinorNumber", DbType.Int16, versionData.Version.Minor),
                                ctx.CreateParameter("@Status", DbType.Int16, versionData.Version.Status),
                                ctx.CreateParameter("@CreationDate", DbType.DateTime2, versionData.CreationDate),
                                ctx.CreateParameter("@CreatedById", DbType.Int32, versionData.CreatedById),
                                ctx.CreateParameter("@ModificationDate", DbType.DateTime2, versionData.ModificationDate),
                                ctx.CreateParameter("@ModifiedById", DbType.Int32, versionData.ModifiedById),
                                ctx.CreateParameter("@ChangedData", DbType.String, int.MaxValue, JsonConvert.SerializeObject(versionData.ChangedData)),
                                ctx.CreateParameter("@DynamicProperties", DbType.String, int.MaxValue,
                                    SerializeDynamicProperties(dynamicData.DynamicProperties)),
                                ctx.CreateParameter("@ContentListProperties", DbType.String, int.MaxValue,
                                    SerializeDynamicProperties(dynamicData.ContentListProperties)),
                            });
                        }).ConfigureAwait(false);
                        var versionTimestamp = ConvertTimestampToInt64(rawVersionTimestamp);
                        if(versionTimestamp == 0L)
                            throw new ContentNotFoundException(
                                $"Version not found. VersionId: {versionData.VersionId} NodeId: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
                        versionData.Timestamp = versionTimestamp;

                        // Update Node
                        var rawNodeTimestamp = await ctx.ExecuteScalarAsync(UpdateNodeScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                            #region ctx.CreateParameter("@NodeId", DbType.Int32, ...
                            ctx.CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                            ctx.CreateParameter("@NodeTypeId", DbType.Int32, nodeHeadData.NodeTypeId),
                            ctx.CreateParameter("@ContentListTypeId", DbType.Int32, nodeHeadData.ContentListTypeId > 0 ? (object) nodeHeadData.ContentListTypeId : DBNull.Value),
                            ctx.CreateParameter("@ContentListId", DbType.Int32, nodeHeadData.ContentListId > 0 ? (object) nodeHeadData.ContentListId : DBNull.Value),
                            ctx.CreateParameter("@CreatingInProgress", DbType.Byte, nodeHeadData.CreatingInProgress ? (byte) 1 : 0),
                            ctx.CreateParameter("@IsDeleted", DbType.Byte, nodeHeadData.IsDeleted ? (byte) 1 : 0),
                            ctx.CreateParameter("@IsInherited", DbType.Byte, (byte)0),
                            ctx.CreateParameter("@ParentNodeId", DbType.Int32, nodeHeadData.NodeId == Identifiers.PortalRootId ? (object)DBNull.Value : nodeHeadData.ParentNodeId),
                            ctx.CreateParameter("@Name", DbType.String, 450, nodeHeadData.Name),
                            ctx.CreateParameter("@DisplayName", DbType.String, 450, (object)nodeHeadData.DisplayName ?? DBNull.Value),
                            ctx.CreateParameter("@Path", DbType.String, 450, nodeHeadData.Path),
                            ctx.CreateParameter("@Index", DbType.Int32, nodeHeadData.Index),
                            ctx.CreateParameter("@Locked", DbType.Byte, nodeHeadData.Locked ? (byte) 1 : 0),
                            ctx.CreateParameter("@LockedById", DbType.Int32, nodeHeadData.LockedById > 0 ? (object) nodeHeadData.LockedById : DBNull.Value),
                            ctx.CreateParameter("@ETag", DbType.AnsiString, 50, nodeHeadData.ETag ?? string.Empty),
                            ctx.CreateParameter("@LockType", DbType.Int32, nodeHeadData.LockType),
                            ctx.CreateParameter("@LockTimeout", DbType.Int32, nodeHeadData.LockTimeout),
                            ctx.CreateParameter("@LockDate", DbType.DateTime2, nodeHeadData.LockDate),
                            ctx.CreateParameter("@LockToken", DbType.AnsiString, 50, nodeHeadData.LockToken ?? string.Empty),
                            ctx.CreateParameter("@LastLockUpdate", DbType.DateTime2, nodeHeadData.LastLockUpdate),
                            ctx.CreateParameter("@CreationDate", DbType.DateTime2, nodeHeadData.CreationDate),
                            ctx.CreateParameter("@CreatedById", DbType.Int32, nodeHeadData.CreatedById),
                            ctx.CreateParameter("@ModificationDate", DbType.DateTime2, nodeHeadData.ModificationDate),
                            ctx.CreateParameter("@ModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                            ctx.CreateParameter("@IsSystem", DbType.Byte, nodeHeadData.IsSystem ? (byte) 1 : 0),
                            ctx.CreateParameter("@OwnerId", DbType.Int32, nodeHeadData.OwnerId),
                            ctx.CreateParameter("@SavingState", DbType.Int32, (int) nodeHeadData.SavingState),
                            ctx.CreateParameter("@NodeTimestamp", DbType.Binary, ConvertInt64ToTimestamp(nodeHeadData.Timestamp)),
                            #endregion
                        });
                        }).ConfigureAwait(false);
                        nodeHeadData.Timestamp = ConvertTimestampToInt64(rawNodeTimestamp);

                        // Update subtree if needed
                        if (originalPath != null)
                            await UpdateSubTreePathAsync(originalPath, nodeHeadData.Path, ctx).ConfigureAwait(false);

                        // Delete unnecessary versions and update last versions
                        await ManageLastVersionsAsync(versionIdsToDelete, nodeHeadData, ctx).ConfigureAwait(false);

                        // Manage ReferenceProperties
                        if (dynamicData.ReferenceProperties.Any())
                            await UpdateReferencePropertiesAsync(dynamicData.ReferenceProperties, versionId, ctx).ConfigureAwait(false);

                        // Manage LongTextProperties
                        if (dynamicData.LongTextProperties.Any())
                            await UpdateLongTextPropertiesAsync(dynamicData.LongTextProperties, versionId, ctx).ConfigureAwait(false);

                        // Manage BinaryProperties
                        foreach (var item in dynamicData.BinaryProperties)
                            await SaveBinaryPropertyAsync(item.Value, versionId, item.Key.Id, false, false, ctx).ConfigureAwait(false);

                        transaction.Commit();
                    }
                    needToCleanupFiles = ctx.NeedToCleanupFiles;
                }

                if (needToCleanupFiles)
                    await BlobStorage.DeleteOrphanedFilesAsync(cancellationToken);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string msg = "Node was not updated. For more details see the inner exception.";
                var transformedException = GetException(e, msg);
                if (transformedException != null)
                    throw transformedException;
                throw new DataException(msg, e);
            }
        }
        protected async Task UpdateSubTreePathAsync(string originalPath, string path, SnDataContext ctx)
        {
            await ctx.ExecuteNonQueryAsync(UpdateSubTreePathScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@OldPath", DbType.String, PathMaxLength, originalPath),
                    ctx.CreateParameter("@NewPath", DbType.String, PathMaxLength, path),
                });
            }).ConfigureAwait(false);
        }
        protected virtual async Task ManageLastVersionsAsync(IEnumerable<int> versionIdsToDelete, NodeHeadData nodeHeadData, SnDataContext ctx)
        {
            var versionIdsParam = (object)DBNull.Value;
            if (versionIdsToDelete != null)
            {
                var versionIds = versionIdsToDelete as int[] ?? versionIdsToDelete.ToArray();
                if (versionIds.Length > 0)
                {
                    await BlobStorage.DeleteBinaryPropertiesAsync(versionIds, ctx).ConfigureAwait(false);

                    versionIdsParam = string.Join(",", versionIds.Select(x => x.ToString()));
                }
            }

            await ctx.ExecuteReaderAsync(ManageLastVersionsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                    ctx.CreateParameter("@VersionIds", DbType.AnsiString, versionIdsParam)
                });
            }, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    nodeHeadData.Timestamp = reader.GetSafeLongFromBytes("NodeTimestamp");
                    nodeHeadData.LastMajorVersionId = reader.GetSafeInt32("LastMajorVersionId");
                    nodeHeadData.LastMinorVersionId = reader.GetInt32("LastMinorVersionId");
                }
                return true;
            }).ConfigureAwait(false);
        }
        protected virtual async Task SaveBinaryPropertyAsync(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, bool isNewProperty,
            SnDataContext dataContext)
        {
            if (value == null || value.IsEmpty)
                await BlobStorage.DeleteBinaryPropertyAsync(versionId, propertyTypeId, dataContext).ConfigureAwait(false);
            else if (value.Id == 0 || isNewProperty)
                await BlobStorage.InsertBinaryPropertyAsync(value, versionId, propertyTypeId, isNewNode, dataContext).ConfigureAwait(false);
            else
                await BlobStorage.UpdateBinaryPropertyAsync(value, dataContext).ConfigureAwait(false);
        }
        protected abstract string UpdateVersionScript { get; }
        protected abstract string UpdateNodeScript { get; }
        protected abstract string UpdateSubTreePathScript { get; }
        protected abstract string ManageLastVersionsScript { get; }
        protected virtual async Task UpdateReferencePropertiesAsync(IDictionary<PropertyType, List<int>> referenceProperties, int versionId, SnDataContext ctx)
        {
            var parameters = new List<DbParameter> { ctx.CreateParameter("@VersionId", DbType.Int32, versionId) };
            var sqlBuilder = new StringBuilder(UpdateReferencePropertiesHeadScript);
            var index = 0;
            foreach (var item in referenceProperties)
            {
                index++;
                sqlBuilder.AppendFormat(UpdateReferencePropertiesScript, index);
                parameters.Add(ctx.CreateParameter("@PropertyTypeId" + index, DbType.Int32, item.Key.Id));
                parameters.Add(ctx.CreateParameter("@ReferredNodeIds" + index, DbType.String, int.MaxValue, string.Join(",", item.Value.Select(x => x.ToString()))));
            }
            await ctx.ExecuteNonQueryAsync(sqlBuilder.ToString(), cmd =>
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }).ConfigureAwait(false);
        }
        protected abstract string UpdateReferencePropertiesHeadScript { get; }
        protected abstract string UpdateReferencePropertiesScript { get; }
        protected virtual async Task UpdateLongTextPropertiesAsync(IDictionary<PropertyType, string> longTextProperties, int versionId, SnDataContext ctx)
        {
            var longTextSqlBuilder = new StringBuilder();
            var longTextSqlParameters = new List<DbParameter>();
            var index = 0;
            longTextSqlBuilder.Append(UpdateLongtextPropertiesHeadScript);
            longTextSqlParameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
            foreach (var item in longTextProperties)
            {
                longTextSqlBuilder.AppendFormat(UpdateLongtextPropertiesScript, ++index);
                longTextSqlParameters.Add(ctx.CreateParameter("@PropertyTypeId" + index, DbType.Int32, item.Key.Id));
                longTextSqlParameters.Add(ctx.CreateParameter("@Length" + index, DbType.Int32, item.Value.Length));
                longTextSqlParameters.Add(ctx.CreateParameter("@Value" + index, DbType.String, int.MaxValue, item.Value));
            }
            await ctx.ExecuteNonQueryAsync(longTextSqlBuilder.ToString(),
                cmd => { cmd.Parameters.AddRange(longTextSqlParameters.ToArray()); }).ConfigureAwait(false);
        }
        protected abstract string UpdateLongtextPropertiesHeadScript { get; }
        protected abstract string UpdateLongtextPropertiesScript { get; }

        /// <inheritdoc />
        public override async Task CopyAndUpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete, CancellationToken cancellationToken, int expectedVersionId = 0,
            string originalPath = null)
        {
            try
            {
                var needToCleanupFiles = false;
                using (var ctx = CreateDataContext(cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        // Copy and update version
                        var versionId = await ctx.ExecuteReaderAsync(CopyVersionAndUpdateScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                                ctx.CreateParameter("@PreviousVersionId", DbType.Int32, versionData.VersionId),
                                ctx.CreateParameter("@DestinationVersionId", DbType.Int32, (expectedVersionId != 0) ? (object)expectedVersionId : DBNull.Value),
                                ctx.CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                                ctx.CreateParameter("@MajorNumber", DbType.Int16, versionData.Version.Major),
                                ctx.CreateParameter("@MinorNumber", DbType.Int16, versionData.Version.Minor),
                                ctx.CreateParameter("@Status", DbType.Int16, versionData.Version.Status),
                                ctx.CreateParameter("@CreationDate", DbType.DateTime2, versionData.CreationDate),
                                ctx.CreateParameter("@CreatedById", DbType.Int32, versionData.CreatedById),
                                ctx.CreateParameter("@ModificationDate", DbType.DateTime2, versionData.ModificationDate),
                                ctx.CreateParameter("@ModifiedById", DbType.Int32, versionData.ModifiedById),
                                ctx.CreateParameter("@ChangedData", DbType.String, int.MaxValue, JsonConvert.SerializeObject(versionData.ChangedData)),
                                ctx.CreateParameter("@DynamicProperties", DbType.String, int.MaxValue,
                                    SerializeDynamicProperties(dynamicData.DynamicProperties)),
                                ctx.CreateParameter("@ContentListProperties", DbType.String, int.MaxValue,
                                    SerializeDynamicProperties(dynamicData.ContentListProperties)),
                            });
                        }, async (reader, cancel) =>
                        {
                            cancel.ThrowIfCancellationRequested();
                            while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                            {
                                cancel.ThrowIfCancellationRequested();
                                versionData.VersionId = reader.GetInt32("VersionId");
                                versionData.Timestamp = reader.GetSafeLongFromBytes("Timestamp");
                            }
                            cancel.ThrowIfCancellationRequested();
                            if (await reader.NextResultAsync(cancel).ConfigureAwait(false))
                            {
                                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                                {
                                    var binId = reader.GetInt32("BinaryPropertyId");
                                    var propId = reader.GetInt32("PropertyTypeId");
                                    var propertyType = ActiveSchema.PropertyTypes.GetItemById(propId);
                                    if (propertyType != null)
                                        if (dynamicData.BinaryProperties.TryGetValue(propertyType, out var binaryData))
                                            binaryData.Id = binId;
                                }
                            }
                            return versionData.VersionId;
                        }).ConfigureAwait(false);

                        // Update Node
                        var rawNodeTimestamp = await ctx.ExecuteScalarAsync(UpdateNodeScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                            #region ctx.CreateParameter("@NodeId", DbType.Int32, ...
                            ctx.CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                            ctx.CreateParameter("@NodeTypeId", DbType.Int32, nodeHeadData.NodeTypeId),
                            ctx.CreateParameter("@ContentListTypeId", DbType.Int32, nodeHeadData.ContentListTypeId > 0 ? (object) nodeHeadData.ContentListTypeId : DBNull.Value),
                            ctx.CreateParameter("@ContentListId", DbType.Int32, nodeHeadData.ContentListId > 0 ? (object) nodeHeadData.ContentListId : DBNull.Value),
                            ctx.CreateParameter("@CreatingInProgress", DbType.Byte, nodeHeadData.CreatingInProgress ? (byte) 1 : 0),
                            ctx.CreateParameter("@IsDeleted", DbType.Byte, nodeHeadData.IsDeleted ? (byte) 1 : 0),
                            ctx.CreateParameter("@IsInherited", DbType.Byte, (byte)0),
                            ctx.CreateParameter("@ParentNodeId", DbType.Int32, nodeHeadData.NodeId == Identifiers.PortalRootId ? (object)DBNull.Value : nodeHeadData.ParentNodeId),
                            ctx.CreateParameter("@Name", DbType.String, 450, nodeHeadData.Name),
                            ctx.CreateParameter("@DisplayName", DbType.String, 450, (object)nodeHeadData.DisplayName ?? DBNull.Value),
                            ctx.CreateParameter("@Path", DbType.String, 450, nodeHeadData.Path),
                            ctx.CreateParameter("@Index", DbType.Int32, nodeHeadData.Index),
                            ctx.CreateParameter("@Locked", DbType.Byte, nodeHeadData.Locked ? (byte) 1 : 0),
                            ctx.CreateParameter("@LockedById", DbType.Int32, nodeHeadData.LockedById > 0 ? (object) nodeHeadData.LockedById : DBNull.Value),
                            ctx.CreateParameter("@ETag", DbType.AnsiString, 50, nodeHeadData.ETag ?? string.Empty),
                            ctx.CreateParameter("@LockType", DbType.Int32, nodeHeadData.LockType),
                            ctx.CreateParameter("@LockTimeout", DbType.Int32, nodeHeadData.LockTimeout),
                            ctx.CreateParameter("@LockDate", DbType.DateTime2, nodeHeadData.LockDate),
                            ctx.CreateParameter("@LockToken", DbType.AnsiString, 50, nodeHeadData.LockToken ?? string.Empty),
                            ctx.CreateParameter("@LastLockUpdate", DbType.DateTime2, nodeHeadData.LastLockUpdate),
                            ctx.CreateParameter("@CreationDate", DbType.DateTime2, nodeHeadData.CreationDate),
                            ctx.CreateParameter("@CreatedById", DbType.Int32, nodeHeadData.CreatedById),
                            ctx.CreateParameter("@ModificationDate", DbType.DateTime2, nodeHeadData.ModificationDate),
                            ctx.CreateParameter("@ModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                            ctx.CreateParameter("@IsSystem", DbType.Byte, nodeHeadData.IsSystem ? (byte) 1 : 0),
                            ctx.CreateParameter("@OwnerId", DbType.Int32, nodeHeadData.OwnerId),
                            ctx.CreateParameter("@SavingState", DbType.Int32, (int) nodeHeadData.SavingState),
                            ctx.CreateParameter("@NodeTimestamp", DbType.Binary, ConvertInt64ToTimestamp(nodeHeadData.Timestamp)),
                            #endregion
                        });
                        }).ConfigureAwait(false);
                        nodeHeadData.Timestamp = ConvertTimestampToInt64(rawNodeTimestamp);

                        // Update subtree if needed
                        if (originalPath != null)
                            await UpdateSubTreePathAsync(originalPath, nodeHeadData.Path, ctx).ConfigureAwait(false);

                        // Delete unnecessary versions and update last versions
                        await ManageLastVersionsAsync(versionIdsToDelete, nodeHeadData, ctx).ConfigureAwait(false);

                        // Manage ReferenceProperties
                        if (dynamicData.LongTextProperties.Any())
                            await UpdateReferencePropertiesAsync(dynamicData.ReferenceProperties, versionId, ctx).ConfigureAwait(false);

                        // Manage LongTextProperties
                        if (dynamicData.LongTextProperties.Any())
                            await UpdateLongTextPropertiesAsync(dynamicData.LongTextProperties, versionId, ctx).ConfigureAwait(false);

                        // Manage BinaryProperties
                        foreach (var item in dynamicData.BinaryProperties)
                            await SaveBinaryPropertyAsync(item.Value, versionId, item.Key.Id, false, false, ctx).ConfigureAwait(false);

                        transaction.Commit();
                    }
                    needToCleanupFiles = ctx.NeedToCleanupFiles;
                }

                if (needToCleanupFiles)
                    await BlobStorage.DeleteOrphanedFilesAsync(cancellationToken);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string msg = "Node was not updated. For more details see the inner exception.";
                var transformedException = GetException(e, msg);
                if (transformedException != null)
                    throw transformedException;
                throw new DataException(msg, e);
            }
        }
        protected abstract string CopyVersionAndUpdateScript { get; }

        /// <inheritdoc />
        public override async Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken)
        {
            try
            {
                var needToCleanupFiles = false;
                using (var ctx = CreateDataContext(cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        // Update Node
                        var rawNodeTimestamp = await ctx.ExecuteScalarAsync(UpdateNodeScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                            #region ctx.CreateParameter("@NodeId", DbType.Int32, ...
                            ctx.CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                            ctx.CreateParameter("@NodeTypeId", DbType.Int32, nodeHeadData.NodeTypeId),
                            ctx.CreateParameter("@ContentListTypeId", DbType.Int32, nodeHeadData.ContentListTypeId > 0 ? (object) nodeHeadData.ContentListTypeId : DBNull.Value),
                            ctx.CreateParameter("@ContentListId", DbType.Int32, nodeHeadData.ContentListId > 0 ? (object) nodeHeadData.ContentListId : DBNull.Value),
                            ctx.CreateParameter("@CreatingInProgress", DbType.Byte, nodeHeadData.CreatingInProgress ? (byte) 1 : 0),
                            ctx.CreateParameter("@IsDeleted", DbType.Byte, nodeHeadData.IsDeleted ? (byte) 1 : 0),
                            ctx.CreateParameter("@IsInherited", DbType.Byte, (byte)0),
                            ctx.CreateParameter("@ParentNodeId", DbType.Int32, nodeHeadData.NodeId == Identifiers.PortalRootId ? (object)DBNull.Value : nodeHeadData.ParentNodeId),
                            ctx.CreateParameter("@Name", DbType.String, 450, nodeHeadData.Name),
                            ctx.CreateParameter("@DisplayName", DbType.String, 450, (object)nodeHeadData.DisplayName ?? DBNull.Value),
                            ctx.CreateParameter("@Path", DbType.String, 450, nodeHeadData.Path),
                            ctx.CreateParameter("@Index", DbType.Int32, nodeHeadData.Index),
                            ctx.CreateParameter("@Locked", DbType.Byte, nodeHeadData.Locked ? (byte) 1 : 0),
                            ctx.CreateParameter("@LockedById", DbType.Int32, nodeHeadData.LockedById > 0 ? (object) nodeHeadData.LockedById : DBNull.Value),
                            ctx.CreateParameter("@ETag", DbType.AnsiString, 50, nodeHeadData.ETag ?? string.Empty),
                            ctx.CreateParameter("@LockType", DbType.Int32, nodeHeadData.LockType),
                            ctx.CreateParameter("@LockTimeout", DbType.Int32, nodeHeadData.LockTimeout),
                            ctx.CreateParameter("@LockDate", DbType.DateTime2, nodeHeadData.LockDate),
                            ctx.CreateParameter("@LockToken", DbType.AnsiString, 50, nodeHeadData.LockToken ?? string.Empty),
                            ctx.CreateParameter("@LastLockUpdate", DbType.DateTime2, nodeHeadData.LastLockUpdate),
                            ctx.CreateParameter("@CreationDate", DbType.DateTime2, nodeHeadData.CreationDate),
                            ctx.CreateParameter("@CreatedById", DbType.Int32, nodeHeadData.CreatedById),
                            ctx.CreateParameter("@ModificationDate", DbType.DateTime2, nodeHeadData.ModificationDate),
                            ctx.CreateParameter("@ModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                            ctx.CreateParameter("@IsSystem", DbType.Byte, nodeHeadData.IsSystem ? (byte) 1 : 0),
                            ctx.CreateParameter("@OwnerId", DbType.Int32, nodeHeadData.OwnerId),
                            ctx.CreateParameter("@SavingState", DbType.Int32, (int) nodeHeadData.SavingState),
                            ctx.CreateParameter("@NodeTimestamp", DbType.Binary, ConvertInt64ToTimestamp(nodeHeadData.Timestamp)),
                            #endregion
                        });
                        }).ConfigureAwait(false);
                        nodeHeadData.Timestamp = ConvertTimestampToInt64(rawNodeTimestamp);

                        // Delete unnecessary versions and update last versions
                        await ManageLastVersionsAsync(versionIdsToDelete, nodeHeadData, ctx).ConfigureAwait(false);

                        transaction.Commit();
                    }

                    needToCleanupFiles = ctx.NeedToCleanupFiles;
                }

                if (needToCleanupFiles)
                    await BlobStorage.DeleteOrphanedFilesAsync(cancellationToken);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string msg = "NodeHead was not updated. For more details see the inner exception.";
                var transformedException = GetException(e, msg);
                if (transformedException != null)
                    throw transformedException;
                throw new DataException(msg, e);
            }
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds, CancellationToken cancellationToken)
        {
            var ids = string.Join(",", versionIds.Select(x => x.ToString()));
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodesScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@VersionIds", DbType.AnsiString, int.MaxValue, ids),
                            ctx.CreateParameter("@LongTextMaxSize", DbType.Int32, DataStore.TextAlternationSizeLimit)

                        });
                }, async (reader, cancel) =>
                {
                    var result = new Dictionary<int, NodeData>();

                    // BASE DATA
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();

                        var versionId = reader.GetInt32("VersionId");
                        var nodeTypeId = reader.GetInt32("NodeTypeId");
                        var contentListTypeId = reader.GetSafeInt32("ContentListTypeId");

                        var nodeData = new NodeData(nodeTypeId, contentListTypeId)
                        {
                            Id = reader.GetInt32("NodeId"),
                            VersionId = versionId,
                            Version = new VersionNumber(reader.GetInt16("MajorNumber"), reader.GetInt16("MinorNumber"),
                                (VersionStatus)reader.GetInt16("Status")),
                            ContentListTypeId = contentListTypeId,
                            ContentListId = reader.GetSafeInt32("ContentListId"),
                            CreatingInProgress = reader.GetSafeBooleanFromByte("CreatingInProgress"),
                            IsDeleted = reader.GetSafeBooleanFromByte("IsDeleted"),
                            // not used: IsInherited
                            ParentId = reader.GetSafeInt32("ParentNodeId"),
                            Name = reader.GetString("Name"),
                            DisplayName = reader.GetSafeString("DisplayName"),
                            Path = reader.GetString("Path"),
                            Index = reader.GetInt32("Index"),
                            Locked = reader.GetSafeBooleanFromByte("Locked"),
                            LockedById = reader.GetSafeInt32("LockedById"),
                            ETag = reader.GetString("ETag"),
                            LockType = reader.GetInt32("LockType"),
                            LockTimeout = reader.GetInt32("LockTimeout"),
                            LockDate = reader.GetDateTimeUtc("LockDate"),
                            LockToken = reader.GetString("LockToken"),
                            LastLockUpdate = reader.GetDateTimeUtc("LastLockUpdate"),
                            CreationDate = reader.GetDateTimeUtc("NodeCreationDate"),
                            CreatedById = reader.GetInt32("NodeCreatedById"),
                            ModificationDate = reader.GetDateTimeUtc("NodeModificationDate"),
                            ModifiedById = reader.GetInt32("NodeModifiedById"),
                            IsSystem = reader.GetSafeBooleanFromByte("IsSystem"),
                            OwnerId = reader.GetSafeInt32("OwnerId"),
                            SavingState = reader.GetSavingState("SavingState"),
                            ChangedData = reader.GetChangedData("ChangedData"),
                            NodeTimestamp = reader.GetSafeLongFromBytes("NodeTimestamp"),
                            VersionCreationDate = reader.GetDateTimeUtc("CreationDate"),
                            VersionCreatedById = reader.GetInt32("CreatedById"),
                            VersionModificationDate = reader.GetDateTimeUtc("ModificationDate"),
                            VersionModifiedById = reader.GetInt32("ModifiedById"),
                            VersionTimestamp = reader.GetSafeLongFromBytes("VersionTimestamp"),
                        };

                        var dynamicPropertySource = reader.GetSafeString("DynamicProperties");
                        if (dynamicPropertySource != null)
                            foreach (var item in DeserializeDynamicProperties(dynamicPropertySource))
                                nodeData.SetDynamicRawData(item.Key, item.Value);
                        var contentListPropertySource = reader.GetSafeString("ContentListProperties");
                        if (contentListPropertySource != null)
                            foreach (var item in DeserializeDynamicProperties(contentListPropertySource))
                                nodeData.SetDynamicRawData(item.Key, item.Value);

                        result.Add(versionId, nodeData);
                    }

                    // BINARY PROPERTIES
                    cancel.ThrowIfCancellationRequested();
                    await reader.NextResultAsync(cancel).ConfigureAwait(false);
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();

                        var versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
                        var propertyTypeId = reader.GetInt32(reader.GetOrdinal("PropertyTypeId"));

                        var value = GetBinaryDataValueFromReader(reader);

                        var nodeData = result[versionId];
                        nodeData.SetDynamicRawData(propertyTypeId, value);
                    }

                    // REFERENCE PROPERTIES
                    cancel.ThrowIfCancellationRequested();
                    await reader.NextResultAsync(cancel).ConfigureAwait(false);
                    // -- collect references
                    var referenceCollector = new Dictionary<int, Dictionary<int, List<int>>>();
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();

                        var versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
                        var propertyTypeId = reader.GetInt32(reader.GetOrdinal("PropertyTypeId"));
                        var referredNodeId = reader.GetInt32(reader.GetOrdinal("ReferredNodeId"));

                        if (!referenceCollector.ContainsKey(versionId))
                            referenceCollector.Add(versionId, new Dictionary<int, List<int>>());
                        var referenceCollectorPerVersion = referenceCollector[versionId];
                        if (!referenceCollectorPerVersion.ContainsKey(propertyTypeId))
                            referenceCollectorPerVersion.Add(propertyTypeId, new List<int>());
                        referenceCollectorPerVersion[propertyTypeId].Add(referredNodeId);

                    }
                    // -- set references to NodeData
                    foreach (var item in referenceCollector)
                    {
                        var nodeData = result[item.Key];
                        foreach (var subItem in item.Value)
                            nodeData.SetDynamicRawData(subItem.Key, subItem.Value);
                    }

                    // LONGTEXT PROPERTIES
                    cancel.ThrowIfCancellationRequested();
                    await reader.NextResultAsync(cancel).ConfigureAwait(false);
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();

                        var versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
                        var propertyTypeId = reader.GetInt32("PropertyTypeId");
                        var value = reader.GetSafeString("Value");

                        var nodeData = result[versionId];
                        nodeData.SetDynamicRawData(propertyTypeId, value);
                    }

                    return result.Values;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadNodesScript { get; }
        public virtual IDictionary<PropertyType, object> DeserializeDynamicProperties(string src)
        {
            return src.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(DeserializeDynamiProperty).Where(x => x.PropertyType != null)
                .ToDictionary(x => x.PropertyType, x => x.Value);
        }
        protected virtual (PropertyType PropertyType, object Value) DeserializeDynamiProperty(string src)
        {
            var p = src.IndexOf(':');
            var propertyName = src.Substring(0, p);
            var stringValue = src.Substring(p + 1);
            var propertyType = ActiveSchema.PropertyTypes[propertyName];
            if (propertyType == null)
                return (null, null);
            object value;
            switch (propertyType.DataType)
            {
                case DataType.String:
                    value = stringValue.Replace("\\r\\n", "\r\n").Replace("\\\\", "\\");
                    break;
                case DataType.Int:
                    value = int.Parse(stringValue);
                    break;
                case DataType.Currency:
                    value = decimal.Parse(stringValue, CultureInfo.InvariantCulture);
                    break;
                case DataType.DateTime:
                    value = DateTime.Parse(stringValue);
                    break;
                case DataType.Reference:
                    value = stringValue.Length > 0
                        ? stringValue.Split(',').Select(int.Parse).ToArray()
                        : new int[0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (propertyType, value);
        }
        protected virtual BinaryDataValue GetBinaryDataValueFromReader(DbDataReader reader)
        {
            return new BinaryDataValue
            {
                Id = reader.GetInt32("BinaryPropertyId"),
                FileId = reader.GetInt32("FileId"),
                ContentType = reader.GetSafeString("ContentType"),
                FileName = new BinaryFileName(
                    reader.GetSafeString("FileNameWithoutExtension") ?? "",
                    reader.GetSafeString("Extension") ?? ""),
                Size = reader.GetInt64("Size"),
                Checksum = reader.GetSafeString("Checksum"),
                BlobProviderName = reader.GetSafeString("BlobProvider"),
                BlobProviderData = reader.GetSafeString("BlobProviderData"),
                Timestamp = reader.GetSafeLongFromBytes("Timestamp"),
                Stream = null
            };
        }

        public override Task DeleteNodeAsync(NodeHeadData nodeHeadData, CancellationToken cancellationToken)
        {
            return DeleteNodeAsync(nodeHeadData, 500, cancellationToken);
        }
        public virtual async Task DeleteNodeAsync(NodeHeadData nodeHeadData, int partitionSize, CancellationToken cancellationToken)
        {
            try
            {
                using (var ctx = CreateDataContext(cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        await ctx.ExecuteNonQueryAsync(DeleteNodeScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                                ctx.CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                                ctx.CreateParameter("@Timestamp", DbType.Binary, ConvertInt64ToTimestamp(nodeHeadData.Timestamp)),
                                ctx.CreateParameter("@PartitionSize", DbType.Int32, partitionSize),

                            });
                        }).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }

                await BlobStorage.DeleteOrphanedFilesAsync(cancellationToken);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string msg = "Node was not updated. For more details see the inner exception.";
                var transformedException = GetException(e, msg);
                if (transformedException != null)
                    throw transformedException;
                throw new DataException(msg, e);
            }
        }
        protected abstract string DeleteNodeScript { get; }

        public override async Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var ctx = CreateDataContext(cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        var result = await ctx.ExecuteScalarAsync(MoveNodeScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                                ctx.CreateParameter("@SourceNodeId", DbType.Int32, sourceNodeHeadData.NodeId),
                                ctx.CreateParameter("@TargetNodeId", DbType.Int32, targetNodeId),
                                ctx.CreateParameter("@SourceTimestamp", DbType.Binary,
                                    ConvertInt64ToTimestamp(sourceNodeHeadData.Timestamp)),
                            });
                        }).ConfigureAwait(false);

                        transaction.Commit();

                        sourceNodeHeadData.Timestamp = ConvertTimestampToInt64(result);
                    }
                }

                await BlobStorage.DeleteOrphanedFilesAsync(cancellationToken);
            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                const string msg = "Node cannot be moved. For more details see the inner exception.";
                var transformedException = GetException(e, msg);
                if (transformedException != null)
                    throw transformedException;
                throw new DataException(msg, e);
            }

        }
        protected abstract string MoveNodeScript { get; }

        public override async Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] propertiesToLoad,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<int, string>();
            if (propertiesToLoad == null || propertiesToLoad.Length == 0)
                return result;

            var propParamPrefix = "@Prop";
            var sql = string.Format(LoadTextPropertyValuesScript, string.Join(", ",
                Enumerable.Range(0, propertiesToLoad.Length).Select(i => propParamPrefix + i)));

            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
                    for (int i = 0; i < propertiesToLoad.Length; i++)
                        cmd.Parameters.Add(ctx.CreateParameter(propParamPrefix + i, DbType.Int32, propertiesToLoad[i]));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(reader.GetInt32("PropertyTypeId"), reader.GetSafeString("Value"));
                    }
                    return result;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadTextPropertyValuesScript { get; }

        public override async Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                return await BlobStorage.LoadBinaryPropertyAsync(versionId, propertyTypeId, ctx).ConfigureAwait(false);
        }

        public override async Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = (int) await ctx.ExecuteScalarAsync(NodeExistsScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, path));
                }).ConfigureAwait(false);
                return result != 0;
            }
        }
        protected abstract string NodeExistsScript { get; }

        /* =============================================================================================== NodeHead */

        /// <inheritdoc />
        public override async Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadByPathScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, PathMaxLength, path));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!await reader.ReadAsync(cancel).ConfigureAwait(false))
                        return null;
                    return GetNodeHeadFromReader(reader);
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadNodeHeadByPathScript { get; }

        /// <inheritdoc />
        public override async Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadByIdScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@NodeId", DbType.Int32, nodeId));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!await reader.ReadAsync(cancel).ConfigureAwait(false))
                        return null;
                    return GetNodeHeadFromReader(reader);
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadNodeHeadByIdScript { get; }

        /// <inheritdoc />
        public override async Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadByVersionIdScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@VersionId", DbType.Int32, versionId));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!await reader.ReadAsync(cancel).ConfigureAwait(false))
                        return null;
                    return GetNodeHeadFromReader(reader);
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadNodeHeadByVersionIdScript { get; }

        /// <inheritdoc />
        public override async Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds, CancellationToken cancellationToken)
        {
            var ids = string.Join(",", nodeIds.Select(x => x.ToString()));
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadsByIdSetScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@NodeIds", DbType.AnsiString, int.MaxValue, ids));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<NodeHead>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(GetNodeHeadFromReader(reader));
                    }

                    return result;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadNodeHeadsByIdSetScript { get; }

        public override async Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(GetVersionNumbersByNodeIdScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@NodeId", DbType.Int32, nodeId));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<NodeHead.NodeVersion>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(new NodeHead.NodeVersion(
                            new VersionNumber(
                                reader.GetInt16("MajorNumber"),
                                reader.GetInt16("MinorNumber"),
                                (VersionStatus)reader.GetInt16("Status")),
                            reader.GetInt32("VersionId")));
                    }
                    return result.ToArray();
                }).ConfigureAwait(false);
            }
        }
        protected abstract string GetVersionNumbersByNodeIdScript { get; }

        public override async Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(string path,
            CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(GetVersionNumbersByPathScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, path));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<NodeHead.NodeVersion>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(new NodeHead.NodeVersion(
                            new VersionNumber(
                                reader.GetInt16("MajorNumber"),
                                reader.GetInt16("MinorNumber"),
                                (VersionStatus)reader.GetInt16("Status")),
                            reader.GetInt32("VersionId")));
                    }
                    return result.ToArray();
                }).ConfigureAwait(false);
            }
        }
        protected abstract string GetVersionNumbersByPathScript { get; }

        protected virtual NodeHead GetNodeHeadFromReader(DbDataReader reader)
        {
            return new NodeHead(
                reader.GetInt32(0),         // nodeId,
                reader.GetString(1),        // name,
                reader.GetSafeString(2),    // displayName,
                reader.GetString(3),        // pathInDb,
                reader.GetSafeInt32(4),     // parentNodeId,
                reader.GetInt32(5),         // nodeTypeId,
                reader.GetSafeInt32(6),     // contentListTypeId,
                reader.GetSafeInt32(7),     // contentListId,
                reader.GetDateTimeUtc(8),   // creationDate,
                reader.GetDateTimeUtc(9),   // modificationDate,
                reader.GetSafeInt32(10),    // lastMinorVersionId,
                reader.GetSafeInt32(11),    // lastMajorVersionId,
                reader.GetSafeInt32(12),    // ownerId,
                reader.GetSafeInt32(13),    // creatorId,
                reader.GetSafeInt32(14),    // modifierId,
                reader.GetSafeInt32(15),    // index,
                reader.GetSafeInt32(16),    // lockerId
                ConvertTimestampToInt64(reader.GetValue(17))    // timestamp
            );
        }

        public override async Task<IEnumerable<NodeHead>> LoadNodeHeadsFromPredefinedSubTreesAsync(IEnumerable<string> paths, bool resolveAll, bool resolveChildren,
            CancellationToken cancellationToken)
        {
            var pathList = paths.ToList();
            List<NodeHead> heads;
            using (var ctx = CreateDataContext(cancellationToken))
            {
                string sql;
                if (resolveAll)
                {
                    sql = GetAppModelScript(pathList, true, resolveChildren);
                    List<NodeHead>[] resultSorter;
                    heads = await ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<NodeHead>();
                        resultSorter = new List<NodeHead>[pathList.Count];
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            var nodeHead = NodeHead.Get(reader.GetInt32(0));
                            var searchPath = resolveChildren
                                ? RepositoryPath.GetParentPath(nodeHead.Path)
                                : nodeHead.Path;
                            var index = pathList.IndexOf(searchPath);
                            if (resultSorter[index] == null)
                                resultSorter[index] = new List<NodeHead>();
                            resultSorter[index].Add(nodeHead);
                        }
                        foreach (var list in resultSorter)
                        {
                            if (list != null)
                            {
                                list.Sort((x, y) => string.Compare(x.Path, y.Path, StringComparison.Ordinal));
                                foreach (var nodeHead in list)
                                    result.Add(nodeHead);
                            }
                        }
                        return result;
                    }).ConfigureAwait(false);
                }
                else
                {
                    sql = GetAppModelScript(pathList, false, resolveChildren);
                    heads = await ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<NodeHead>();
                        if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                            result.Add(NodeHead.Get(reader.GetInt32(0)));
                        return result;
                    }).ConfigureAwait(false);
                }
            }
            return heads;
        }
        protected abstract string GetAppModelScript(IEnumerable<string> paths, bool resolveAll, bool resolveChildren);

        /* =============================================================================================== NodeQuery */

        public override async Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            var sql = string.Format(InstanceCountScript,
                string.Join(", ", Enumerable.Range(0, nodeTypeIds.Length).Select(i => "@Id" + i)));

            using (var ctx = CreateDataContext(cancellationToken))
            {
                return (int)await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    var index = 0;
                    cmd.Parameters.AddRange(nodeTypeIds.Select(i => ctx.CreateParameter("@Id" + index++, DbType.Int32, i)).ToArray());
                }).ConfigureAwait(false);
            }
        }
        protected abstract string InstanceCountScript { get; }

        public override async Task<IEnumerable<int>> GetChildrenIdentifiersAsync(int parentId, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                return await ctx.ExecuteReaderAsync(
                    GetChildrenIdentfiersScript,
                    cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@ParentNodeId", DbType.Int32, parentId));
                    },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<int>();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            result.Add(reader.GetInt32(0));
                        }
                        return result.ToArray();
                    }).ConfigureAwait(false);
        }
        protected abstract string GetChildrenIdentfiersScript { get; }

        public override async Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds,
            CancellationToken cancellationToken)
        {
            if (referenceName == null)
                throw new ArgumentNullException(nameof(referenceName));
            if (referenceName.Length == 0)
                throw new ArgumentException("Argument referenceName cannot be empty.", nameof(referenceName));
            var referenceProperty = ActiveSchema.PropertyTypes[referenceName];
            if (referenceProperty == null)
                throw new ArgumentException("PropertyType is not found: " + referenceName, nameof(referenceName));
            var referencePropertyId = referenceProperty.Id;

            using (var ctx = CreateDataContext(cancellationToken))
            {
                string sql;
                var parameters = new List<DbParameter>
                {
                    ctx.CreateParameter("@PropertyTypeId", DbType.Int32, referencePropertyId),
                    ctx.CreateParameter("@ReferredNodeId", DbType.Int32, referredNodeId)
                };

                if (nodeTypeIds == null || nodeTypeIds.Length == 0)
                {
                    sql = QueryNodesByReferenceScript;
                }
                else
                {
                    const string prefix = "@NtId";
                    sql = string.Format(QueryNodesByReferenceAndTypeScript, string.Join(",",
                        Enumerable.Range(0, nodeTypeIds.Length).Select(i => prefix + i)));
                    for (var i = 0; i < nodeTypeIds.Length; i++)
                        parameters.Add(ctx.CreateParameter(prefix + i, DbType.Int32, nodeTypeIds[i]));
                }

                return await ctx.ExecuteReaderAsync(sql, cmd => { cmd.Parameters.AddRange(parameters.ToArray()); },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<int>();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            result.Add(reader.GetInt32(0));
                        }
                        return result.ToArray();
                    }).ConfigureAwait(false);
            }

        }
        protected abstract string QueryNodesByReferenceScript { get; }
        protected abstract string QueryNodesByReferenceAndTypeScript { get; }

        /* =============================================================================================== Tree */

        public override async Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                return await ctx.ExecuteReaderAsync(LoadChildTypesToAllowScript, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@NodeId", DbType.Int32, nodeId));
                    },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<NodeType>();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            var name = (string) reader[0];
                            var nt = ActiveSchema.NodeTypes[name];
                            if (nt != null)
                                result.Add(nt);
                        }
                        return result;
                    }).ConfigureAwait(false);
        }
        protected abstract string LoadChildTypesToAllowScript { get; }

        public override async Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(GetContentListTypesInTreeScript, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, path));
                    },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<ContentListType>();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            var id = reader.GetInt32(0);
                            var t = NodeTypeManager.Current.ContentListTypes.GetItemById(id);
                            result.Add(t);
                        }
                        return result;
                    }).ConfigureAwait(false);
            }
        }
        protected abstract string GetContentListTypesInTreeScript { get; }

        /* =============================================================================================== TreeLock */

        public override async Task<int> AcquireTreeLockAsync(string path, DateTime timeLimit,
            CancellationToken cancellationToken)
        {
            var parentChain =  GetParentChain(path);
            var sql = string.Format(AcquireTreeLockScript,
                string.Join(", ", Enumerable.Range(0, parentChain.Length).Select(i => "@Path" + i)));

            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeMin", DbType.DateTime2, GetObsoleteLimitTime()));
                    for (var i = 0; i < parentChain.Length; i++)
                        cmd.Parameters.Add(
                            ctx.CreateParameter("@Path" + i, DbType.String, DataStore.PathMaxLength, parentChain[i]));
                }).ConfigureAwait(false);
                return (result == null || result == DBNull.Value) ? 0 : (int)result;
            }
        }
        protected abstract string AcquireTreeLockScript { get; }

        /// <inheritdoc />
        public override async Task<bool> IsTreeLockedAsync(string path, DateTime timeLimit, CancellationToken cancellationToken)
        {
            RepositoryPath.CheckValidPath(path);
            var parentChain = GetParentChain(path);

            var sql = string.Format(IsTreeLockedScript,
                string.Join(", ", Enumerable.Range(0, parentChain.Length).Select(i => "@Path" + i)));

            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeLimit", DbType.DateTime, timeLimit));
                    for (int i = 0; i < parentChain.Length; i++)
                        cmd.Parameters.Add(ctx.CreateParameter("@Path" + i, DbType.String, 450, parentChain[i]));
                }).ConfigureAwait(false);
                return result != null && result != DBNull.Value;
            }
        }
        protected abstract string IsTreeLockedScript { get; }

        public override async Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken)
        {
            var sql = string.Format(ReleaseTreeLockScript,
                string.Join(", ", Enumerable.Range(0, lockIds.Length).Select(i => "@Id" + i)));

            using (var ctx = CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(sql, cmd =>
                {
                    var index = 0;
                    cmd.Parameters.AddRange(lockIds.Select(i => ctx.CreateParameter("@Id" + index++, DbType.Int32, i)).ToArray());
                }).ConfigureAwait(false);
            }

            await DeleteUnusedLocksAsync(cancellationToken).ConfigureAwait(false);
        }
        protected abstract string ReleaseTreeLockScript { get; }

        public override async Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadAllTreeLocksScript, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new Dictionary<int, string>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(reader.GetInt32(0), reader.GetString(1));
                    }
                    return result;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadAllTreeLocksScript { get; }

        protected async Task DeleteUnusedLocksAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(DeleteUnusedLocksScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@TimeMin", DbType.DateTime2, GetObsoleteLimitTime()));
                }).ConfigureAwait(false);
            }
        }
        protected abstract string DeleteUnusedLocksScript { get; }

        protected string[] GetParentChain(string path)
        {
            var paths = path.Split(RepositoryPath.PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            paths[0] = "/" + paths[0];
            for (int i = 1; i < paths.Length; i++)
                paths[i] = paths[i - 1] + "/" + paths[i];
            return paths.Reverse().ToArray();
        }
        protected DateTime GetObsoleteLimitTime()
        {
            return DateTime.Now.AddHours(-8.0);
        }

        /* =============================================================================================== IndexDocument */

        /// <inheritdoc />
        public override async Task<long> SaveIndexDocumentAsync(int versionId, string indexDoc, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(SaveIndexDocumentScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@VersionId", DbType.Int32, versionId),
                        ctx.CreateParameter("@IndexDocument", DbType.String, int.MaxValue, indexDoc),
                    });
                }).ConfigureAwait(false);
                return ConvertTimestampToInt64(result);
            }
        }
        protected abstract string SaveIndexDocumentScript { get; }

        public override async Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadIndexDocumentsByVersionIdScript, cmd =>
                    {
                        cmd.Parameters.Add(ctx.CreateParameter("@VersionIds", DbType.String, string.Join(",", versionIds.Select(x => x.ToString()))));
                    },
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var result = new List<IndexDocumentData>();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            result.Add(GetIndexDocumentDataFromReader(reader));
                        }
                        return result;
                    }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadIndexDocumentsByVersionIdScript { get; }

        public override IEnumerable<IndexDocumentData> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes)
        {
            var offset = 0;
            var blockSize = IndexBlockSize;

            while (LoadNextIndexDocumentBlock(offset, blockSize, path, excludedNodeTypes, out var buffer))
            {
                foreach (var indexDocData in buffer)
                    yield return indexDocData;
                offset += blockSize;
            }
        }
        private bool LoadNextIndexDocumentBlock(int offset, int blockSize, string path, int[] excludedNodeTypes, out List<IndexDocumentData> buffer)
        {
            var sql = excludedNodeTypes.Any()
                ? string.Format(LoadIndexDocumentCollectionBlockByPathAndTypeScript, string.Join(", ", excludedNodeTypes))
                : LoadIndexDocumentCollectionBlockByPathScript;
            using (var ctx = CreateDataContext(CancellationToken.None))
            {
                try
                {
                    buffer = ctx.ExecuteReaderAsync(sql, cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, path),
                            ctx.CreateParameter("@Offset", DbType.Int32, DataStore.PathMaxLength, offset),
                            ctx.CreateParameter("@Count", DbType.Int32, DataStore.PathMaxLength, blockSize),
                        });
                    }, (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        var block = new List<IndexDocumentData>(blockSize);
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                cancel.ThrowIfCancellationRequested();
                                block.Add(GetIndexDocumentDataFromReader(reader));
                            }
                        }
                        return Task.FromResult(block);
                    }).GetAwaiter().GetResult();
                    return buffer.Count > 0;
                }
                catch (Exception ex) // logged, rethrown
                {
                    SnLog.WriteException(ex, $"Loading index document block failed. Offset: {offset}, Path: {path}");
                    throw;
                }
            }
        }
        protected IndexDocumentData GetIndexDocumentDataFromReader(DbDataReader reader)
        {
            var versionId = reader.GetSafeInt32("VersionId");
            var approved = Convert.ToInt32(reader.GetInt16("Status")) == (int)VersionStatus.Approved;
            var isLastMajor = reader.GetSafeInt32("LastMajorVersionId") == versionId;

            var stringData = reader.GetSafeString("IndexDocument");
            return new IndexDocumentData(null, stringData)
            {
                NodeTypeId = reader.GetSafeInt32("NodeTypeId"),
                VersionId = versionId,
                NodeId = reader.GetSafeInt32("NodeId"),
                ParentId = reader.GetSafeInt32("ParentNodeId"),
                Path = reader.GetSafeString("Path"),
                IsSystem = reader.GetSafeBooleanFromByte("IsSystem"),
                IsLastDraft = reader.GetSafeInt32("LastMinorVersionId") == versionId,
                IsLastPublic = approved && isLastMajor,
                NodeTimestamp = ConvertTimestampToInt64(reader.GetSafeByteArray("NodeTimestamp")),
                VersionTimestamp = ConvertTimestampToInt64(reader.GetSafeByteArray("VersionTimestamp")),
            };
        }
        protected abstract string LoadIndexDocumentCollectionBlockByPathScript { get; }
        protected abstract string LoadIndexDocumentCollectionBlockByPathAndTypeScript { get; }

        public override async Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNotIndexedNodeIdsScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@FromId", DbType.Int32, fromId),
                        ctx.CreateParameter("@ToId", DbType.Int32, toId),
                    });
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var idSet = new List<int>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        idSet.Add(reader.GetSafeInt32(0));
                    }
                    return idSet;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadNotIndexedNodeIdsScript { get; }

        /* =============================================================================================== IndexingActivity */

        /// <inheritdoc />
        public override async Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(GetLastIndexingActivityIdScript).ConfigureAwait(false);
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
        protected abstract string GetLastIndexingActivityIdScript { get; }

        /// <inheritdoc />
        public override async Task DeleteRestorePointsAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                await ctx.ExecuteNonQueryAsync(DeleteRestorePointsScript).ConfigureAwait(false);
        }
        protected abstract string DeleteRestorePointsScript { get; }

        /// <inheritdoc />
        public override async Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(GetCurrentIndexingActivityStatusScript,
                    async (reader, cancel) =>
                    {
                        var states = new List<(int Id, string State)>();
                        while (await reader.ReadAsync(cancel))
                            states.Add((Id: reader.GetInt32(0), State: reader.GetString(1)));

                        if (states.Count == 0)
                            return IndexingActivityStatus.Startup;

                        return new IndexingActivityStatus
                        {
                            LastActivityId = states[0].Id,
                            Gaps = states.Skip(1).Select(x => x.Id).ToArray()
                        };
                    }).ConfigureAwait(false);
            }
        }
        protected abstract string GetCurrentIndexingActivityStatusScript { get; }

        /// <inheritdoc />
        public override async Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(IndexingActivityStatus status, CancellationToken cancellationToken)
        {
            IndexingActivityStatusRestoreResult result;

            var gaps = string.Join(",", status.Gaps.Select(x => x.ToString()));
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var rawResult = await ctx.ExecuteScalarAsync(RestoreIndexingActivityStatusScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@LastActivityId", DbType.Int32, status.LastActivityId),
                        ctx.CreateParameter("@Gaps", DbType.String, int.MaxValue, gaps)
                    });
                }).ConfigureAwait(false);
                var stringResult = rawResult == DBNull.Value ? string.Empty : (string)rawResult;
                result = (IndexingActivityStatusRestoreResult)Enum.Parse(typeof(IndexingActivityStatusRestoreResult), stringResult, true);
            }

            return result;
        }
        protected abstract string RestoreIndexingActivityStatusScript { get; }

        public override async Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadIndexingActivitiesPageScript,
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@From", DbType.Int32, fromId),
                            ctx.CreateParameter("@To", DbType.Int32, toId),
                            ctx.CreateParameter("@Top", DbType.Int32, count)
                        });
                    },
                    async (reader, cancel) =>
                        await GetIndexingActivitiesFromReaderAsync(reader, executingUnprocessedActivities, activityFactory, cancel).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
        protected abstract string LoadIndexingActivitiesPageScript { get; }

        public override async Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadIndexingActivitiyGapsScript,
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Gaps", DbType.String, string.Join(",", gaps)),
                            ctx.CreateParameter("@Top", DbType.Int32, gaps.Length),
                        });
                    },
                    async (reader, cancel) =>
                        await GetIndexingActivitiesFromReaderAsync(reader, executingUnprocessedActivities, activityFactory, cancel).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
        protected abstract string LoadIndexingActivitiyGapsScript { get; }

        public override async Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount,
            int runningTimeoutInSeconds, int[] waitingActivityIds,
            CancellationToken cancellationToken)
        {
            //if (waitingActivityIds == null || waitingActivityIds.Length == 0)
            //    return await LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds, cancellationToken).ConfigureAwait(false);
            string waitingActivityIdParam = null;
            if (waitingActivityIds != null && waitingActivityIds.Length > 0)
                waitingActivityIdParam = string.Join(",", waitingActivityIds.Select(x => x.ToString()));

            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(
                    waitingActivityIdParam == null
                        ? LoadExecutableIndexingActivitiesScript
                        : LoadExecutableAndFinishedIndexingActivitiesScript, cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@Top", DbType.Int32, maxCount),
                            ctx.CreateParameter("@TimeLimit", DbType.DateTime2,
                                DateTime.UtcNow.AddSeconds(-runningTimeoutInSeconds))
                        });
                        if (waitingActivityIdParam != null)
                            cmd.Parameters.Add(ctx.CreateParameter("@WaitingIds", DbType.String, waitingActivityIdParam));
                    }, async (reader, cancel) =>
                    {
                        var activities = await GetIndexingActivitiesFromReaderAsync(reader, false, activityFactory, cancel).ConfigureAwait(false);

                        var finishedIds = new List<int>();
                        if (waitingActivityIdParam != null)
                        {
                            cancel.ThrowIfCancellationRequested();
                            await reader.NextResultAsync(cancel).ConfigureAwait(false);
                            while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                            {
                                cancel.ThrowIfCancellationRequested();
                                finishedIds.Add(reader.GetInt32(0));
                            }
                        }

                        return new ExecutableIndexingActivitiesResult
                        {
                            Activities = activities,
                            FinishedActivitiyIds = finishedIds.ToArray()
                        };
                    }).ConfigureAwait(false);
            }

            /*
            var ids = string.Join(", ", waitingActivityIds.Select(x => x.ToString()).ToArray());

            var sql = $"{StartIndexingActivitiesScript}\r\nSELECT IndexingActivityId FROM IndexingActivities" +
                      $" WHERE RunningState = 'Done' AND IndexingActivityId IN ({ids})";
            using (var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text })
            {
                cmd.Parameters.Add("@Top", SqlDbType.Int).Value = maxCount;
                cmd.Parameters.Add("@TimeLimit", SqlDbType.DateTime).Value = DateTime.UtcNow.AddSeconds(-runningTimeoutInSeconds);
                var result = LoadIndexingActivitiesAndWaitingStates(cmd, false, activityFactory);
                finishedActivitiyIds = result.Item2;
                return result.Item1;
            }
            */
        }
        protected abstract string LoadExecutableIndexingActivitiesScript { get; }
        protected abstract string LoadExecutableAndFinishedIndexingActivitiesScript { get; }


        public override async Task RegisterIndexingActivityAsync(IIndexingActivity activity,
            CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                activity.CreationDate = DateTime.UtcNow;
                var rawActivityId = await ctx.ExecuteScalarAsync(RegisterIndexingActivityScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ActivityType", DbType.String, 50, activity.ActivityType.ToString()),
                        ctx.CreateParameter("@CreationDate", DbType.DateTime2, activity.CreationDate),
                        ctx.CreateParameter("@RunningState", DbType.AnsiString, 10, activity.RunningState.ToString()),
                        ctx.CreateParameter("@LockTime", DbType.DateTime2, (object)activity.LockTime ?? DBNull.Value),
                        ctx.CreateParameter("@NodeId", DbType.Int32, activity.NodeId),
                        ctx.CreateParameter("@VersionId", DbType.Int32, activity.VersionId),
                        ctx.CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, (object)activity.Path ?? DBNull.Value),
                        ctx.CreateParameter("@VersionTimestamp", DbType.Int64, (object)activity.VersionTimestamp ?? DBNull.Value),
                        ctx.CreateParameter("@Extension", DbType.String, int.MaxValue, (object)activity.Extension ?? DBNull.Value),
                    });
                }).ConfigureAwait(false);
                activity.Id = Convert.ToInt32(rawActivityId);
            }
        }
        protected abstract string RegisterIndexingActivityScript { get; }

        public override async Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState,
            CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                await ctx.ExecuteNonQueryAsync(UpdateIndexingActivityRunningStateScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@IndexingActivityId", DbType.Int32, indexingActivityId),
                        ctx.CreateParameter("@RunningState", DbType.AnsiString, runningState.ToString())
                    });
                }).ConfigureAwait(false);
        }
        protected abstract string UpdateIndexingActivityRunningStateScript { get; }

        public override async Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds,
            CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            await ctx.ExecuteNonQueryAsync(RefreshIndexingActivityLockTimeScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Ids", DbType.String, string.Join(",", waitingIds.Select(x => x.ToString()))),
                    ctx.CreateParameter("@LockTime", DbType.DateTime2, DateTime.UtcNow)
                });
            }).ConfigureAwait(false);
        }
        protected abstract string RefreshIndexingActivityLockTimeScript { get; }

        public override async Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                await ctx.ExecuteNonQueryAsync(DeleteFinishedIndexingActivitiesScript).ConfigureAwait(false);
        }
        protected abstract string DeleteFinishedIndexingActivitiesScript { get; }

        public override async Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                await ctx.ExecuteNonQueryAsync(DeleteAllIndexingActivitiesScript).ConfigureAwait(false);
        }
        protected abstract string DeleteAllIndexingActivitiesScript { get; }

        private async Task<IIndexingActivity[]> GetIndexingActivitiesFromReaderAsync(DbDataReader reader, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();

            var result = new List<IIndexingActivity>();

            var indexingActivityIdColumn = reader.GetOrdinal("IndexingActivityId");
            var activityTypeColumn = reader.GetOrdinal("ActivityType");
            var creationDateColumn = reader.GetOrdinal("CreationDate");
            var stateColumn = reader.GetOrdinal("RunningState");
            var startDateDateColumn = reader.GetOrdinal("LockTime");
            var nodeIdColumn = reader.GetOrdinal("NodeId");
            var versionIdColumn = reader.GetOrdinal("VersionId");
            var pathColumn = reader.GetOrdinal("Path");
            var versionTimestampColumn = reader.GetOrdinal("VersionTimestamp");
            var indexDocumentColumn = reader.GetOrdinal("IndexDocument");
            var nodeTypeIdColumn = reader.GetOrdinal("NodeTypeId");
            var parentNodeIdColumn = reader.GetOrdinal("ParentNodeId");
            var isSystemColumn = reader.GetOrdinal("IsSystem");
            var lastMinorVersionIdColumn = reader.GetOrdinal("LastMinorVersionId");
            var lastMajorVersionIdColumn = reader.GetOrdinal("LastMajorVersionId");
            var statusColumn = reader.GetOrdinal("Status");
            var nodeTimestampColumn = reader.GetOrdinal("NodeTimestamp");
            var extensionColumn = reader.GetOrdinal("Extension");

            while (await reader.ReadAsync(cancel).ConfigureAwait(false))
            {
                cancel.ThrowIfCancellationRequested();

                var type = (IndexingActivityType)Enum.Parse(typeof(IndexingActivityType), reader.GetSafeString(activityTypeColumn));
                var activity = activityFactory.CreateActivity(type);
                activity.Id = reader.GetSafeInt32(indexingActivityIdColumn);
                activity.ActivityType = type;
                activity.CreationDate = reader.GetDateTime(creationDateColumn);
                activity.RunningState = (IndexingActivityRunningState)Enum.Parse(typeof(IndexingActivityRunningState), reader.GetSafeString(stateColumn) ?? "Wait");
                activity.LockTime = reader.GetSafeDateTime(startDateDateColumn);
                activity.NodeId = reader.GetSafeInt32(nodeIdColumn);
                activity.VersionId = reader.GetSafeInt32(versionIdColumn);
                activity.Path = reader.GetSafeString(pathColumn);
                activity.FromDatabase = true;
                activity.IsUnprocessedActivity = executingUnprocessedActivities;
                activity.Extension = reader.GetSafeString(extensionColumn);

                var nodeTypeId = reader.GetSafeInt32(nodeTypeIdColumn);
                var parentNodeId = reader.GetSafeInt32(parentNodeIdColumn);
                var isSystem = reader.GetSafeBooleanFromByte(isSystemColumn);
                var lastMinorVersionId = reader.GetSafeInt32(lastMinorVersionIdColumn);
                var lastMajorVersionId = reader.GetSafeInt32(lastMajorVersionIdColumn);
                var status = reader.GetSafeInt16(statusColumn);
                var nodeTimeStamp = reader.GetSafeLongFromBytes(nodeTimestampColumn);
                var versionTimestamp = reader.GetSafeLongFromBytes(versionTimestampColumn);

                var approved = status == (int)VersionStatus.Approved;
                var isLastMajor = lastMajorVersionId == activity.VersionId;

                var stringData = reader.GetSafeString(indexDocumentColumn);
                //var stringData = Encoding.ASCII.GetString(indexDocumentBytes);
                if (stringData != null)
                {
                    activity.IndexDocumentData = new IndexDocumentData(null, stringData)
                    {
                        NodeTypeId = nodeTypeId,
                        VersionId = activity.VersionId,
                        NodeId = activity.NodeId,
                        ParentId = parentNodeId,
                        Path = activity.Path,
                        IsSystem = isSystem,
                        IsLastDraft = lastMinorVersionId == activity.VersionId,
                        IsLastPublic = approved && isLastMajor,
                        NodeTimestamp = nodeTimeStamp,
                        VersionTimestamp = versionTimestamp,
                    };
                }
                result.Add(activity);
            }
            return result.ToArray();
        }

        /* =============================================================================================== Schema */

        /// <inheritdoc />
        public override async Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadSchemaScript, async (reader, cancel) =>
                {
                    var schema = new RepositorySchemaData();

                    cancel.ThrowIfCancellationRequested();
                    if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        schema.Timestamp = reader.GetSafeLongFromBytes("Timestamp");

                    // PropertyTypes
                    cancel.ThrowIfCancellationRequested();
                    await reader.NextResultAsync(cancel).ConfigureAwait(false);
                    var propertyTypes = new List<PropertyTypeData>();
                    schema.PropertyTypes = propertyTypes;
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        propertyTypes.Add(new PropertyTypeData
                        {
                            Id = reader.GetInt32("PropertyTypeId"),
                            Name = reader.GetString("Name"),
                            DataType = reader.GetEnumValueByName<DataType>("DataType"),
                            Mapping = reader.GetInt32("Mapping"),
                            IsContentListProperty = reader.GetSafeBooleanFromByte("IsContentListProperty")
                        });
                    }

                    // NodeTypes
                    cancel.ThrowIfCancellationRequested();
                    await reader.NextResultAsync(cancel).ConfigureAwait(false);
                    var nodeTypes = new List<NodeTypeData>();
                    schema.NodeTypes = nodeTypes;
                    var tree = new List<(NodeTypeData Data, int ParentId)>(); // data, parentId
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        var data = new NodeTypeData
                        {
                            Id = reader.GetInt32("NodeTypeId"),
                            Name = reader.GetString("Name"),
                            ClassName = reader.GetString("ClassName"),
                            Properties = new List<string>(
                                reader.GetSafeString("Properties")
                                    ?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0])
                        };
                        var parentId = reader.GetSafeInt32("ParentId");
                        tree.Add((data, parentId));
                        nodeTypes.Add(data);
                    }
                    foreach (var item in tree)
                    {
                        var parent = tree.FirstOrDefault(x => x.Data.Id == item.ParentId);
                        item.Data.ParentName = parent.Data?.Name;
                    }

                    // ContentListTypes
                    var contentListTypes = new List<ContentListTypeData>();
                    schema.ContentListTypes = contentListTypes;
                    cancel.ThrowIfCancellationRequested();
                    await reader.NextResultAsync(cancel).ConfigureAwait(false);
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        contentListTypes.Add(new ContentListTypeData
                        {
                            Id=reader.GetInt32("ContentListTypeId"),
                            Name=reader.GetString("Name"),
                            Properties = new List<string>(
                                reader.GetSafeString("Properties")
                                    ?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0])
                        });
                    }

                    return schema;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadSchemaScript { get; }
       
        /// <inheritdoc />
        public override async Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken)
        {
            var lockToken = Guid.NewGuid().ToString();
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(StartSchemaUpdateScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Timestamp", DbType.Binary, ConvertInt64ToTimestamp(schemaTimestamp)),
                        ctx.CreateParameter("@LockToken", DbType.AnsiString, 50, lockToken)
                    });
                }).ConfigureAwait(false);
                var resultCode = result == DBNull.Value ? 0 : (int)result;

                if (resultCode == -1)
                    throw new DataException("Storage schema is out of date.");
                if (resultCode == -2)
                    throw new DataException("Schema is locked by someone else.");
            }
            return lockToken;
        }
        protected abstract string StartSchemaUpdateScript { get; }

        /// <inheritdoc />
        public override async Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(FinishSchemaUpdateScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@LockToken", DbType.AnsiString, 50, schemaLock)); 
                }).ConfigureAwait(false);

                var timestamp = ConvertTimestampToInt64(result);
                if(timestamp == 0L)
                    throw new DataException("Schema is locked by someone else.");

                return timestamp;
            }
        }
        protected abstract string FinishSchemaUpdateScript { get; }

        /* =============================================================================================== Logging */

        /// <inheritdoc />
        public override async Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                var unused = await ctx.ExecuteScalarAsync(WriteAuditEventScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@EventID", DbType.Int32, auditEvent.EventId),
                        ctx.CreateParameter("@Category", DbType.String, 50, (object)auditEvent.Category ?? DBNull.Value),
                        ctx.CreateParameter("@Priority", DbType.Int32, auditEvent.Priority),
                        ctx.CreateParameter("@Severity", DbType.AnsiString, 30, auditEvent.Severity),
                        ctx.CreateParameter("@Title", DbType.String, 256, (object)auditEvent.Title ?? DBNull.Value),
                        ctx.CreateParameter("@ContentId", DbType.Int32, auditEvent.ContentId),
                        ctx.CreateParameter("@ContentPath", DbType.String, PathMaxLength, (object)auditEvent.ContentPath ?? DBNull.Value),
                        ctx.CreateParameter("@UserName", DbType.String, 450, (object)auditEvent.UserName ?? DBNull.Value),
                        ctx.CreateParameter("@LogDate", DbType.DateTime, auditEvent.Timestamp),
                        ctx.CreateParameter("@MachineName", DbType.AnsiString, 32, (object)auditEvent.MachineName ?? DBNull.Value),
                        ctx.CreateParameter("@AppDomainName", DbType.AnsiString, 512, (object)auditEvent.AppDomainName ?? DBNull.Value),
                        ctx.CreateParameter("@ProcessID", DbType.AnsiString, 256, auditEvent.ProcessId),
                        ctx.CreateParameter("@ProcessName", DbType.AnsiString, 512, (object)auditEvent.ProcessName ?? DBNull.Value),
                        ctx.CreateParameter("@ThreadName", DbType.AnsiString, 512, (object)auditEvent.ThreadName ?? DBNull.Value),
                        ctx.CreateParameter("@Win32ThreadId", DbType.AnsiString, 128, auditEvent.ThreadId),
                        ctx.CreateParameter("@Message", DbType.String, 1500, (object)auditEvent.Message ?? DBNull.Value),
                        ctx.CreateParameter("@Formattedmessage", DbType.String, int.MaxValue, (object)auditEvent.FormattedMessage ?? DBNull.Value),
                    });
                }).ConfigureAwait(false);
            }
        }
        protected abstract string WriteAuditEventScript { get; }

        public override async Task<IEnumerable<AuditLogEntry>> LoadLastAuditEventsAsync(int count, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadLastAuditEventsScript, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@Top", DbType.Int32, count));
                }, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<AuditLogEntry>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(new AuditLogEntry
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("LogId")),
                            EventId = reader.GetSafeInt32(reader.GetOrdinal("EventId")),
                            Title = reader.GetSafeString(reader.GetOrdinal("Title")),
                            ContentId = reader.GetSafeInt32(reader.GetOrdinal("ContentId")),
                            ContentPath = reader.GetSafeString(reader.GetOrdinal("ContentPath")),
                            UserName = reader.GetSafeString(reader.GetOrdinal("UserName")),
                            LogDate = reader.GetDateTime(reader.GetOrdinal("LogDate")),
                            Message = reader.GetSafeString(reader.GetOrdinal("Message")),
                            FormattedMessage = reader.GetSafeString(reader.GetOrdinal("FormattedMessage")),
                        });
                    }
                    return result;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadLastAuditEventsScript { get; }

        /* =============================================================================================== Provider Tools */

        public override bool IsCacheableText(string text)
        {
            return text?.Length < DataStore.TextAlternationSizeLimit;
        }

        public override async Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return (string)await ctx.ExecuteScalarAsync(GetNameOfLastNodeWithNameBaseScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ParentId", DbType.Int32, parentId),
                        ctx.CreateParameter("@Name", DbType.String, namebase),
                        ctx.CreateParameter("@Extension", DbType.String, extension),
                    });
                }).ConfigureAwait(false);
            }
        }
        protected abstract string GetNameOfLastNodeWithNameBaseScript { get; }

        /// <inheritdoc />
        public override async Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken)
        {
            RepositoryPath.CheckValidPath(path);
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return (long)await ctx.ExecuteScalarAsync(GetTreeSizeScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@IncludeChildren", DbType.Byte, includeChildren ? (byte) 1 : 0),
                        ctx.CreateParameter("@NodePath", DbType.String, PathMaxLength, path),
                    });
                }).ConfigureAwait(false);
            }
        }
        protected abstract string GetTreeSizeScript { get; }

        public override async Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                return (int)await ctx.ExecuteScalarAsync(
                    path == null ? GetNodeCountScript : GetNodeCountInSubtreeScript,
                    cmd =>
                    {
                        if (path != null)
                            cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, path));
                    }).ConfigureAwait(false);
        }
        protected abstract string GetNodeCountScript { get; }
        protected abstract string GetNodeCountInSubtreeScript { get; }

        public override async Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
                return (int)await ctx.ExecuteScalarAsync(
                    path == null ? GetVersionCountScript : GetVersionCountInSubtreeScript,
                    cmd =>
                    {
                        if (path != null)
                            cmd.Parameters.Add(ctx.CreateParameter("@Path", DbType.String, path));
                    }).ConfigureAwait(false);
        }
        protected abstract string GetVersionCountScript { get; }
        protected abstract string GetVersionCountInSubtreeScript { get; }

        /* =============================================================================================== Installation */

        /// <inheritdoc />
        public override async Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken)
        {
            using (var ctx = CreateDataContext(cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadEntityTreeScript, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    var result = new List<EntityTreeNodeData>();
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        cancel.ThrowIfCancellationRequested();
                        result.Add(new EntityTreeNodeData
                        {
                            Id = reader.GetInt32("NodeId"),
                            ParentId = reader.GetSafeInt32("ParentNodeId"),
                            OwnerId = reader.GetSafeInt32("OwnerId")
                        });
                    }
                    return result;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadEntityTreeScript { get; }

        /// <inheritdoc />
        public override async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
        {
            const string schemaCheckSql = @"
SELECT CASE WHEN EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Nodes'
)
THEN CAST(1 AS BIT)
ELSE CAST(0 AS BIT) END";

            using (var ctx = CreateDataContext(cancellationToken))
            {
                // make sure that database tables exist
                var result = await ctx.ExecuteScalarAsync(schemaCheckSql).ConfigureAwait(false);
                return Convert.ToBoolean(result);
            }
        }

        /* =============================================================================================== Usage */

        public override async Task LoadDatabaseUsageAsync(
            Action<NodeModel> nodeVersionCallback,
            Action<LongTextModel> longTextPropertyCallback,
            Action<BinaryPropertyModel> binaryPropertyCallback,
            Action<FileModel> fileCallback,
            Action<LogEntriesTableModel> logEntriesTableCallback,
            CancellationToken cancellation)
        {
            using (var ctx = CreateDataContext(cancellation))
            {
                await ctx.ExecuteReaderAsync(LoadDatabaseUsageScript, async (reader, cancel) =>
                {
                    // PROCESS NODE+VERSION ROWS

                    var nodeIdIndex = reader.GetOrdinal("NodeId");
                    var versionIdIndex = reader.GetOrdinal("VersionId");
                    var parentNodeIdIndex = reader.GetOrdinal("ParentNodeId");
                    var nodeTypeIdIndex = reader.GetOrdinal("NodeTypeId");
                    var majorNumberIndex = reader.GetOrdinal("MajorNumber");
                    var minorNumberIndex = reader.GetOrdinal("MinorNumber");
                    var statusIndex = reader.GetOrdinal("Status");
                    var isLastPublicIndex = reader.GetOrdinal("LastPub");
                    var isLastDraftIndex = reader.GetOrdinal("LastWork");
                    var ownerIdIndex = reader.GetOrdinal("OwnerId");
                    var dynamicPropertiesSizeIndex = reader.GetOrdinal("DynamicPropertiesSize");
                    var contentListPropertiesSizeIndex = reader.GetOrdinal("ContentListPropertiesSize");
                    var changedDataSizeIndex = reader.GetOrdinal("ChangedDataSize");
                    var indexSizeIndex = reader.GetOrdinal("IndexSize");
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        var node = new NodeModel
                        {
                            NodeId = reader.GetInt32(nodeIdIndex),
                            VersionId = reader.GetInt32(versionIdIndex),
                            ParentNodeId = reader.GetSafeInt32(parentNodeIdIndex),
                            NodeTypeId = reader.GetInt32(nodeTypeIdIndex),
                            Version = ParseVersion(reader.GetInt16(majorNumberIndex),
                                reader.GetInt16(minorNumberIndex),
                                reader.GetInt16(statusIndex)),
                            IsLastPublic = reader.GetInt32(isLastPublicIndex) > 0,
                            IsLastDraft = reader.GetInt32(isLastDraftIndex) > 0,
                            OwnerId = reader.GetInt32(ownerIdIndex),
                            DynamicPropertiesSize = reader.GetInt64(dynamicPropertiesSizeIndex),
                            ContentListPropertiesSize = reader.GetInt64(contentListPropertiesSizeIndex),
                            ChangedDataSize = reader.GetInt64(changedDataSizeIndex),
                            IndexSize = reader.GetInt64(indexSizeIndex),
                        };
                        nodeVersionCallback(node);
                        cancel.ThrowIfCancellationRequested();
                    }

                    if (!(await reader.NextResultAsync(cancel).ConfigureAwait(false)))
                        throw new ApplicationException("Missing result set: LongTextModels.");

                    // PROCESS LONGTEXT ROWS

                    versionIdIndex = reader.GetOrdinal("VersionId");
                    var sizeIndex = reader.GetOrdinal("Size");
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        var longText = new LongTextModel
                        {
                            VersionId = reader.GetInt32(versionIdIndex),
                            Size = reader.GetInt64(sizeIndex),
                        };
                        longTextPropertyCallback(longText);
                        cancel.ThrowIfCancellationRequested();
                    }

                    // PROCESS BINARY PROPERTY ROWS

                    if (!(await reader.NextResultAsync(cancel).ConfigureAwait(false)))
                        throw new ApplicationException("Missing result set: BinaryPropertyModels.");

                    versionIdIndex = reader.GetOrdinal("VersionId");
                    var fileIdIndex = reader.GetOrdinal("FileId");
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        var binaryProperty = new BinaryPropertyModel
                        {
                            VersionId = reader.GetInt32(versionIdIndex),
                            FileId = reader.GetInt32(fileIdIndex),
                        };
                        binaryPropertyCallback(binaryProperty);
                        cancel.ThrowIfCancellationRequested();
                    }

                    // PROCESS FILE ROWS

                    if (!(await reader.NextResultAsync(cancel).ConfigureAwait(false)))
                        throw new ApplicationException("Missing result set: FileModels.");

                    fileIdIndex = reader.GetOrdinal("FileId");
                    sizeIndex = reader.GetOrdinal("Size");
                    var streamSizeIndex = reader.GetOrdinal("StreamSize");
                    while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        var fileModel = new FileModel
                        {
                            FileId = reader.GetInt32(fileIdIndex),
                            Size = reader.GetInt64(sizeIndex),
                            StreamSize = reader.GetInt64(streamSizeIndex)
                        };
                        fileCallback(fileModel);
                        cancel.ThrowIfCancellationRequested();
                    }

                    // PROCESS LOGENTRIES TABLE

                    if (!(await reader.NextResultAsync(cancel).ConfigureAwait(false)))
                        throw new ApplicationException("Missing result set: LogEntriesTableModel.");

                    var logEntriesTableModel = new LogEntriesTableModel();
                    if (await reader.ReadAsync(cancel).ConfigureAwait(false))
                    {
                        logEntriesTableModel.Count = reader.GetInt32(reader.GetOrdinal("Rows"));
                        logEntriesTableModel.Metadata = reader.GetInt64(reader.GetOrdinal("Metadata"));
                        logEntriesTableModel.Text = reader.GetInt64(reader.GetOrdinal("Text"));
                    }
                    logEntriesTableCallback(logEntriesTableModel);

                    return true;
                }).ConfigureAwait(false);
            }
        }
        protected abstract string LoadDatabaseUsageScript { get; }

        private string ParseVersion(int major, int minor, short status)
        {
            char c;
            switch (status)
            {
                case 1: c = 'A'; break;
                case 2: c = 'L'; break;
                case 4: c = 'D'; break;
                case 8: c = 'R'; break;
                case 16: c = 'P'; break;
                default: throw new ArgumentException("Unknown VersionStatus: {status}");
            }
            return $"V{major}.{minor}.{c}";
        }


        /* =============================================================================================== Tools */

        protected abstract long ConvertTimestampToInt64(object timestamp);
        protected abstract object ConvertInt64ToTimestamp(long timestamp);
    }
}
