using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
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

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public abstract class RelationalDataProviderBase : DataProvider2
    {
        /* =============================================================================================== Factory methods */

        public abstract DbCommand CreateCommand();
        public abstract DbConnection CreateConnection();
        public abstract DbParameter CreateParameter();

        protected DbParameter CreateParameter(string name, DbType dbType, object value)
        {
            var prm = CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Value = value;
            return prm;
        }
        protected DbParameter CreateParameter(string name, DbType dbType, int size, object value)
        {
            var prm = CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Size = size;
            prm.Value = value;
            return prm;
        }

        /* =============================================================================================== Nodes */

        /// <inheritdoc />
        public override async Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using (var ctx = new SnDataContext(this, cancellationToken))
                {
                    using (var transaction = ctx.BeginTransaction())
                    {
                        // Insert new rows int Nodes and Versions tables
                        var ok = await ctx.ExecuteReaderAsync(InsertNodeAndVersionScript, cmd =>
                        {
                            cmd.Parameters.AddRange(new[]
                            {
                                #region CreateParameter("@NodeTypeId", DbType.Int32, ...
                                CreateParameter("@NodeTypeId", DbType.Int32, nodeHeadData.NodeTypeId),
                                CreateParameter("@ContentListTypeId", DbType.Int32, nodeHeadData.ContentListTypeId > 0 ? (object) nodeHeadData.ContentListTypeId : DBNull.Value),
                                CreateParameter("@ContentListId", DbType.Int32, nodeHeadData.ContentListId > 0 ? (object) nodeHeadData.ContentListId : DBNull.Value),
                                CreateParameter("@CreatingInProgress", DbType.Byte, nodeHeadData.CreatingInProgress ? (byte) 1 : 0),
                                CreateParameter("@IsDeleted", DbType.Byte, nodeHeadData.IsDeleted ? (byte) 1 : 0),
                                CreateParameter("@IsInherited", DbType.Byte, (byte) 0),
                                CreateParameter("@ParentNodeId", DbType.Int32, nodeHeadData.ParentNodeId),
                                CreateParameter("@Name", DbType.String, 450, nodeHeadData.Name),
                                CreateParameter("@DisplayName", DbType.String, 450, (object)nodeHeadData.DisplayName ?? DBNull.Value),
                                CreateParameter("@Path", DbType.String, DataStore.PathMaxLength, nodeHeadData.Path),
                                CreateParameter("@Index", DbType.Int32, nodeHeadData.Index),
                                CreateParameter("@Locked", DbType.Byte, nodeHeadData.Locked ? (byte) 1 : 0),
                                CreateParameter("@LockedById", DbType.Int32, nodeHeadData.LockedById > 0 ? (object) nodeHeadData.LockedById : DBNull.Value),
                                CreateParameter("@ETag", DbType.AnsiString, 50, nodeHeadData.ETag ?? string.Empty),
                                CreateParameter("@LockType", DbType.Int32, nodeHeadData.LockType),
                                CreateParameter("@LockTimeout", DbType.Int32, nodeHeadData.LockTimeout),
                                CreateParameter("@LockDate", DbType.DateTime2, nodeHeadData.LockDate),
                                CreateParameter("@LockToken", DbType.AnsiString, 50, nodeHeadData.LockToken ?? string.Empty),
                                CreateParameter("@LastLockUpdate", DbType.DateTime2, nodeHeadData.LastLockUpdate),
                                CreateParameter("@NodeCreationDate", DbType.DateTime2, nodeHeadData.CreationDate),
                                CreateParameter("@NodeCreatedById", DbType.Int32, nodeHeadData.CreatedById),
                                CreateParameter("@NodeModificationDate", DbType.DateTime2, nodeHeadData.ModificationDate),
                                CreateParameter("@NodeModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                                CreateParameter("@IsSystem", DbType.Byte, nodeHeadData.IsSystem ? (byte) 1 : 0),
                                CreateParameter("@OwnerId", DbType.Int32, nodeHeadData.OwnerId),
                                CreateParameter("@SavingState", DbType.Int32, (int)nodeHeadData.SavingState),
                                CreateParameter("@ChangedData", DbType.String, int.MaxValue, JsonConvert.SerializeObject(versionData.ChangedData)),
                                CreateParameter("@MajorNumber", DbType.Int16, (short)versionData.Version.Major),
                                CreateParameter("@MinorNumber", DbType.Int16, (short)versionData.Version.Minor),
                                CreateParameter("@Status", DbType.Int16, (short)versionData.Version.Status),
                                CreateParameter("@VersionCreationDate", DbType.DateTime2, versionData.CreationDate),
                                CreateParameter("@VersionCreatedById", DbType.Int32, (int)nodeHeadData.CreatedById),
                                CreateParameter("@VersionModificationDate", DbType.DateTime2, versionData.ModificationDate),
                                CreateParameter("@VersionModifiedById", DbType.Int32, (int)nodeHeadData.ModifiedById),
                                CreateParameter("@DynamicProperties", DbType.String, int.MaxValue, SerializeDynamicProperties(dynamicData.DynamicProperties)),
                                #endregion
                            });
                        }, async reader =>
                        {
                            if (await reader.ReadAsync(cancellationToken))
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
                        });

                        var versionId = versionData.VersionId;

                        // Manage ReferenceProperties
                        //UNDONE:DB: Insert ReferenceProperties

                        // Manage LongTextProperties
                        if (dynamicData.LongTextProperties.Any())
                            await InsertLongTextProperties(dynamicData.LongTextProperties, versionId, ctx);

                        // Manage BinaryProperties
                        foreach (var item in dynamicData.BinaryProperties)
                            BlobStorage.InsertBinaryProperty(item.Value, versionId, item.Key.Id, true);

                        transaction.Commit();
                    }
                }

            }
            catch (DataException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DataException("Node was not saved. For more details see the inner exception.", e);
            }
        }
        public virtual string SerializeDynamicProperties(IDictionary<PropertyType, object> properties)
        {
            var lines = properties
                .Select(x => SerializeDynamicProperty(x.Key, x.Value))
                .Where(x => x != null)
                .ToArray();
            return $"\r\n{string.Join("\r\n", lines)}\r\n";
        }
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
                    break;
                default:
                    value = Convert.ToString(propertyValue, CultureInfo.InvariantCulture);
                    break;
            }
            return $"{propertyType.Name}:{value}";
        }
        protected virtual async Task InsertLongTextProperties(IDictionary<PropertyType, string> longTextProperties, int versionId, SnDataContext ctx)
        {
            var longTextSqlBuilder = new StringBuilder();
            var longTextSqlParameters = new List<DbParameter>();
            var index = 0;
            longTextSqlBuilder.Append(InsertLongtextPropertiesHeadScript);
            longTextSqlParameters.Add(CreateParameter("@VersionId", DbType.Int32, versionId));
            foreach (var item in longTextProperties)
            {
                longTextSqlBuilder.AppendFormat(InsertLongtextPropertiesScript, ++index);
                longTextSqlParameters.Add(CreateParameter("@PropertyTypeId" + index, DbType.Int32, item.Key.Id));
                longTextSqlParameters.Add(CreateParameter("@Length" + index, DbType.Int32, item.Value.Length));
                longTextSqlParameters.Add(CreateParameter("@Value" + index, DbType.AnsiString, int.MaxValue, item.Value));
            }
            await ctx.ExecuteNonQueryAsync(longTextSqlBuilder.ToString(),
                cmd => { cmd.Parameters.AddRange(longTextSqlParameters.ToArray()); });
        }
        protected abstract string InsertNodeAndVersionScript { get; }
        protected abstract string InsertLongtextPropertiesHeadScript { get; }
        protected abstract string InsertLongtextPropertiesScript { get; }

        /// <inheritdoc />
        public override async Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete,
            string originalPath = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                using (var transaction = ctx.BeginTransaction())
                {
                    var versionId = versionData.VersionId;

                    // Update version
                    var rawVersionTimestamp = await ctx.ExecuteScalarAsync(UpdateVersionScript, cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            #region CreateParameter("@NodeId", DbType.Int32, ...
                            CreateParameter("@VersionId", DbType.Int32, versionData.VersionId),
                            CreateParameter("@NodeId", DbType.Int32, versionData.NodeId),
                            CreateParameter("@MajorNumber", DbType.Int16, versionData.Version.Major),
                            CreateParameter("@MinorNumber", DbType.Int16, versionData.Version.Minor),
                            CreateParameter("@Status", DbType.Int16, versionData.Version.Status),
                            CreateParameter("@CreationDate", DbType.DateTime2, versionData.CreationDate),
                            CreateParameter("@CreatedById", DbType.Int32, versionData.CreatedById),
                            CreateParameter("@ModificationDate", DbType.DateTime2, versionData.ModificationDate),
                            CreateParameter("@ModifiedById", DbType.Int32, versionData.ModifiedById),
                            CreateParameter("@ChangedData", DbType.String, int.MaxValue, JsonConvert.SerializeObject(versionData.ChangedData)),
                            CreateParameter("@DynamicProperties", DbType.String, int.MaxValue, SerializeDynamicProperties(dynamicData.DynamicProperties)),
                            #endregion
                        });
                    });
                    versionData.Timestamp = ConvertTimestampToInt64(rawVersionTimestamp);

                    // Update Node
                    var rawNodeTimestamp = await ctx.ExecuteScalarAsync(UpdateNodeScript, cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            #region CreateParameter("@NodeId", DbType.Int32, ...
                            CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                            CreateParameter("@NodeTypeId", DbType.Int32, nodeHeadData.NodeTypeId),
                            CreateParameter("@ContentListTypeId", DbType.Int32, nodeHeadData.ContentListTypeId > 0 ? (object) nodeHeadData.ContentListTypeId : DBNull.Value),
                            CreateParameter("@ContentListId", DbType.Int32, nodeHeadData.ContentListId > 0 ? (object) nodeHeadData.ContentListId : DBNull.Value),
                            CreateParameter("@CreatingInProgress", DbType.Byte, nodeHeadData.CreatingInProgress ? (byte) 1 : 0),
                            CreateParameter("@IsDeleted", DbType.Byte, nodeHeadData.IsDeleted ? (byte) 1 : 0),
                            CreateParameter("@IsInherited", DbType.Byte, (byte)0),
                            CreateParameter("@ParentNodeId", DbType.Int32, nodeHeadData.ParentNodeId == Identifiers.PortalRootId ? (object)DBNull.Value : nodeHeadData.ParentNodeId),
                            CreateParameter("@Name", DbType.String, 450, nodeHeadData.Name),
                            CreateParameter("@DisplayName", DbType.String, 450, (object)nodeHeadData.DisplayName ?? DBNull.Value),
                            CreateParameter("@Path", DbType.String, 450, nodeHeadData.Path),
                            CreateParameter("@Index", DbType.Int32, nodeHeadData.Index),
                            CreateParameter("@Locked", DbType.Byte, nodeHeadData.Locked ? (byte) 1 : 0),
                            CreateParameter("@LockedById", DbType.Int32, nodeHeadData.LockedById > 0 ? (object) nodeHeadData.LockedById : DBNull.Value),
                            CreateParameter("@ETag", DbType.AnsiString, 50, nodeHeadData.ETag ?? string.Empty),
                            CreateParameter("@LockType", DbType.Int32, nodeHeadData.LockType),
                            CreateParameter("@LockTimeout", DbType.Int32, nodeHeadData.LockTimeout),
                            CreateParameter("@LockDate", DbType.DateTime2, nodeHeadData.LockDate),
                            CreateParameter("@LockToken", DbType.AnsiString, 50, nodeHeadData.LockToken ?? string.Empty),
                            CreateParameter("@LastLockUpdate", DbType.DateTime2, nodeHeadData.LastLockUpdate),
                            CreateParameter("@CreationDate", DbType.DateTime2, nodeHeadData.CreationDate),
                            CreateParameter("@CreatedById", DbType.Int32, nodeHeadData.CreatedById),
                            CreateParameter("@ModificationDate", DbType.DateTime2, nodeHeadData.ModificationDate),
                            CreateParameter("@ModifiedById", DbType.Int32, nodeHeadData.ModifiedById),
                            CreateParameter("@IsSystem", DbType.Byte, nodeHeadData.IsSystem ? (byte) 1 : 0),
                            CreateParameter("@OwnerId", DbType.Int32, nodeHeadData.OwnerId),
                            CreateParameter("@SavingState", DbType.Int32, (int) nodeHeadData.SavingState),
                            CreateParameter("@NodeTimestamp", DbType.Binary, ConvertInt64ToTimestamp(nodeHeadData.Timestamp)),
                            #endregion
                        });
                    });
                    nodeHeadData.Timestamp = ConvertTimestampToInt64(rawNodeTimestamp);

                    // Update subtree if needed
                    if (originalPath != null)
                        await UpdateSubTreePath(originalPath, nodeHeadData.Path, ctx);

                    // Delete versions
                    await DeleteVersions(versionIdsToDelete, nodeHeadData, ctx);

                    // Manage ReferenceProperties
                    //UNDONE:DB: Update ReferenceProperties

                    // Manage LongTextProperties
                    if (dynamicData.LongTextProperties.Any())
                        await UpdateLongTextProperties(dynamicData.LongTextProperties, versionId, ctx);

                    // Manage BinaryProperties
                    foreach (var item in dynamicData.BinaryProperties)
                        SaveBinaryProperty(item.Value, versionId, item.Key.Id, false, false);

                    transaction.Commit();
                }
            }
        }
        protected async Task UpdateSubTreePath(string originalPath, string path, SnDataContext ctx)
        {
            await ctx.ExecuteNonQueryAsync(UpdateSubTreePathScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    CreateParameter("@OldPath", DbType.String, PathMaxLength, originalPath),
                    CreateParameter("@NewPath", DbType.String, PathMaxLength, path),
                });
            });
        }
        protected virtual async Task DeleteVersions(IEnumerable<int> versionIdsToDelete, NodeHeadData nodeHeadData, SnDataContext ctx)
        {
            if (versionIdsToDelete == null)
                return;

            var versionIds = versionIdsToDelete as int[] ?? versionIdsToDelete.ToArray();
            if (versionIds.Length == 0)
                return;

            //UNDONE:DB: Rewrite to async and pass ctx.
            BlobStorage.DeleteBinaryProperties(versionIds);

            await ctx.ExecuteReaderAsync(DeleteVersionsScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    CreateParameter("@NodeId", DbType.Int32, nodeHeadData.NodeId),
                    CreateParameter("@VersionIds", DbType.AnsiString, string.Join(",", versionIds.Select(x => x.ToString())))
                });
            }, async reader =>
            {
                if (await reader.ReadAsync(ctx.CancellationToken))
                {
                    nodeHeadData.Timestamp = reader.GetSafeLongFromBytes("NodeTimestamp");
                    nodeHeadData.LastMajorVersionId = reader.GetInt32("LastMajorVersionId");
                    nodeHeadData.LastMinorVersionId = reader.GetInt32("LastMinorVersionId");
                }
                return true;
            });
        }
        protected virtual void SaveBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, bool isNewProperty)
        {
            if (value == null || value.IsEmpty)
                BlobStorage.DeleteBinaryProperty(versionId, propertyTypeId);
            else if (value.Id == 0 || isNewProperty)
                BlobStorage.InsertBinaryProperty(value, versionId, propertyTypeId, isNewNode);
            else
                BlobStorage.UpdateBinaryProperty(value);
        }
        protected abstract string UpdateVersionScript { get; }
        protected abstract string UpdateNodeScript { get; }
        protected abstract string UpdateSubTreePathScript { get; }
        protected abstract string DeleteVersionsScript { get; }
        protected virtual async Task UpdateLongTextProperties(IDictionary<PropertyType, string> longTextProperties, int versionId, SnDataContext ctx)
        {
            var longTextSqlBuilder = new StringBuilder();
            var longTextSqlParameters = new List<DbParameter>();
            var index = 0;
            longTextSqlBuilder.Append(UpdateLongtextPropertiesHeadScript);
            longTextSqlParameters.Add(CreateParameter("@VersionId", DbType.Int32, versionId));
            foreach (var item in longTextProperties)
            {
                longTextSqlBuilder.AppendFormat(UpdateLongtextPropertiesScript, ++index);
                longTextSqlParameters.Add(CreateParameter("@PropertyTypeId" + index, DbType.Int32, item.Key.Id));
                longTextSqlParameters.Add(CreateParameter("@Length" + index, DbType.Int32, item.Value.Length));
                longTextSqlParameters.Add(CreateParameter("@Value" + index, DbType.AnsiString, int.MaxValue, item.Value));
            }
            await ctx.ExecuteNonQueryAsync(longTextSqlBuilder.ToString(),
                cmd => { cmd.Parameters.AddRange(longTextSqlParameters.ToArray()); });
        }
        protected abstract string UpdateLongtextPropertiesHeadScript { get; }
        protected abstract string UpdateLongtextPropertiesScript { get; }

        public override Task CopyAndUpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete, int expectedVersionId = 0, string originalPath = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ids = string.Join(",", versionIds.Select(x => x.ToString()));
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodesScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                        {
                            CreateParameter("@VersionIds", DbType.AnsiString, int.MaxValue, ids),
                            CreateParameter("@LongTextMaxSize", DbType.Int32, DataStore.TextAlternationSizeLimit)

                        });
                }, async reader =>
                {
                    var result = new Dictionary<int, NodeData>();

                    // Base data
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var versionId = reader.GetInt32("VersionId");
                        var nodeTypeId = reader.GetInt32("NodeTypeId");
                        var contentListTypeId = reader.GetSafeInt32("ContentListTypeId");

                        var nodeData = new NodeData(nodeTypeId, contentListTypeId)
                        {
                            Id = reader.GetInt32("NodeId"),
                            VersionId = versionId,
                            Version = new VersionNumber(reader.GetInt16("MajorNumber"), reader.GetInt16("MinorNumber"),
                                (VersionStatus)reader.GetInt16("Status")),
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
                        {
                            var dynamicProperties = DeserializeDynamiProperties(dynamicPropertySource);
                            foreach (var item in dynamicProperties)
                                nodeData.SetDynamicRawData(item.Key, item.Value);
                        }

                        result.Add(versionId, nodeData);
                    }

                    // BinaryProperties
                    reader.NextResult();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
                        var propertyTypeId = reader.GetInt32(reader.GetOrdinal("PropertyTypeId"));

                        var value = GetBinaryDataValueFromReader(reader);

                        var nodeData = result[versionId];
                        nodeData.SetDynamicRawData(propertyTypeId, value);
                    }

                    //// ReferenceProperties
                    //reader.NextResult();
                    //while (await reader.ReadAsync(cancellationToken))
                    //{
                    //    var versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
                    //}

                    // LongTextProperties
                    reader.NextResult();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
                        var propertyTypeId = reader.GetInt32("PropertyTypeId");
                        var value = reader.GetSafeString("Value");

                        var nodeData = result[versionId];
                        nodeData.SetDynamicRawData(propertyTypeId, value);
                    }

                    return result.Values;
                });
            }
        }
        protected abstract string LoadNodesScript { get; }
        protected virtual IDictionary<PropertyType, object> DeserializeDynamiProperties(string src)
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
            object value = null;
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
                    value = stringValue.Split(',').Select(x => int.Parse(x)).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (value == null)
                propertyType = null;
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

        public override Task DeleteNodeAsync(NodeHeadData nodeHeadData, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId, long targetTimestamp,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override async Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new Dictionary<int, string>();
            if (notLoadedPropertyTypeIds == null || notLoadedPropertyTypeIds.Length == 0)
                return result;

            var propParamPrefix = "@Prop";
            var sql = string.Format(LoadTextPropertyValuesScript, string.Join(", ",
                Enumerable.Range(0, notLoadedPropertyTypeIds.Length).Select(i => propParamPrefix + i)));

            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(CreateParameter("@VersionId", DbType.Int32, versionId));
                    for (int i = 0; i < notLoadedPropertyTypeIds.Length; i++)
                        cmd.Parameters.Add(CreateParameter(propParamPrefix + i, DbType.Int32, notLoadedPropertyTypeIds[i]));
                }, async reader =>
                {
                    while (await reader.ReadAsync(cancellationToken))
                        result.Add(reader.GetInt32("PropertyTypeId"), reader.GetSafeString("Value"));
                    return result;
                });
            }
        }
        protected abstract string LoadTextPropertyValuesScript { get; }

        public override Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(BlobStorage.LoadBinaryProperty(versionId, propertyTypeId));
        }

        public override Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /* =============================================================================================== NodeHead */

        /// <inheritdoc />
        public override async Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadByPathScript, cmd =>
                {
                    cmd.Parameters.Add(CreateParameter("@Path", DbType.String, PathMaxLength, path));
                }, async reader =>
                {
                    if (!await reader.ReadAsync(cancellationToken))
                        return null;
                    return GetNodeHeadFromReader(reader);
                });
            }
        }
        protected abstract string LoadNodeHeadByPathScript { get; }

        /// <inheritdoc />
        public override async Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadByIdScript, cmd =>
                {
                    cmd.Parameters.Add(CreateParameter("@NodeId", DbType.Int32, nodeId));
                }, async reader =>
                {
                    if (!await reader.ReadAsync(cancellationToken))
                        return null;
                    return GetNodeHeadFromReader(reader);
                });
            }
        }
        protected abstract string LoadNodeHeadByIdScript { get; }

        /// <inheritdoc />
        public override async Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sql = LoadNodeHeadByVersionIdScript;
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }
        protected abstract string LoadNodeHeadByVersionIdScript { get; }

        /// <inheritdoc />
        public override async Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ids = string.Join(",", nodeIds.Select(x => x.ToString()));
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadNodeHeadsByIdSetScript, cmd =>
                {
                    cmd.Parameters.Add(CreateParameter("@NodeIds", DbType.AnsiString, int.MaxValue, ids));
                }, async reader =>
                {
                    var result = new List<NodeHead>();

                    while (await reader.ReadAsync(cancellationToken))
                        result.Add(GetNodeHeadFromReader(reader));

                    return result;
                });
            }
        }
        protected abstract string LoadNodeHeadsByIdSetScript { get; }

        /// <inheritdoc />
        public override async Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(GetNodeVersionsScript, cmd =>
                {
                    cmd.Parameters.Add(CreateParameter("@NodeId", DbType.Int32, nodeId));
                }, async reader =>
                {
                    var result = new List<NodeHead.NodeVersion>();

                    while (await reader.ReadAsync(cancellationToken))
                        result.Add(new NodeHead.NodeVersion(
                            new VersionNumber(
                                reader.GetInt16("MajorNumber"),
                                reader.GetInt16("MinorNumber"),
                                (VersionStatus)reader.GetInt16("Status")),
                            reader.GetInt32("VersionId")));

                    return result.ToArray();
                });
            }
        }
        protected abstract string GetNodeVersionsScript { get; }

        public override Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }
        public override Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

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

        /* =============================================================================================== NodeQuery */

        public override Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        //public override Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name,
        //    CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        //}

        public override Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /* =============================================================================================== Tree */

        public override Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /* =============================================================================================== TreeLock */

        public override Task<int> AcquireTreeLockAsync(string path, DateTime timeLimit,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /// <inheritdoc />
        public override async Task<bool> IsTreeLockedAsync(string path, DateTime timeLimit, CancellationToken cancellationToken = default(CancellationToken))
        {
            RepositoryPath.CheckValidPath(path);
            var parentChain = GetParentChain(path);

            var sql = string.Format(IsTreeLockedScript,
                string.Join(", ", Enumerable.Range(0, parentChain.Length).Select(i => "@Path" + i)));

            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(CreateParameter("@TimeLimit", DbType.DateTime, timeLimit));
                    for (int i = 0; i < parentChain.Length; i++)
                        cmd.Parameters.Add(CreateParameter("@Path" + i, DbType.String, 450, parentChain[i]));
                });
                return result != null && result != DBNull.Value;
            }
        }
        protected abstract string IsTreeLockedScript { get; }

        public override Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        protected string[] GetParentChain(string path)
        {
            var paths = path.Split(RepositoryPath.PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            paths[0] = "/" + paths[0];
            for (int i = 1; i < paths.Length; i++)
                paths[i] = paths[i - 1] + "/" + paths[i];
            return paths.Reverse().ToArray();
        }

        /* =============================================================================================== IndexDocument */

        /// <inheritdoc />
        public override async Task<long> SaveIndexDocumentAsync(int versionId, string indexDoc, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(SaveIndexDocumentScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        CreateParameter("@VersionId", DbType.Int32, versionId),
                        CreateParameter("@IndexDocument", DbType.String, int.MaxValue, indexDoc),
                    });
                });
                return ConvertTimestampToInt64(result);
            }
        }
        protected abstract string SaveIndexDocumentScript { get; }

        public override Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /* =============================================================================================== IndexingActivity */

        /// <inheritdoc />
        public override async Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(GetLastIndexingActivityIdScript);
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
        protected abstract string GetLastIndexingActivityIdScript { get; }

        public override Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount,
            int runningTimeoutInSeconds, int[] waitingActivityIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task RegisterIndexingActivityAsync(IIndexingActivity activity,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }


        /* =============================================================================================== Schema */

        /// <inheritdoc />
        public override async Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadSchemaScript, async reader =>
                {
                    var schema = new RepositorySchemaData();

                    if (await reader.ReadAsync(cancellationToken))
                        schema.Timestamp = reader.GetSafeLongFromBytes("Timestamp");

                    // PropertyTypes
                    reader.NextResult();
                    var propertyTypes = new List<PropertyTypeData>();
                    schema.PropertyTypes = propertyTypes;
                    while (await reader.ReadAsync(cancellationToken))
                    {
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
                    reader.NextResult();
                    var nodeTypes = new List<NodeTypeData>();
                    schema.NodeTypes = nodeTypes;
                    var tree = new List<(NodeTypeData Data, int ParentId)>(); // data, parentId
                    while (await reader.ReadAsync(cancellationToken))
                    {
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
                    //UNDONE:DB: Load ContentListTypes
                    //reader.NextResult();
                    //while (await reader.ReadAsync(cancellationToken))
                    //{
                    //}

                    return schema;
                });
            }
        }
        protected abstract string LoadSchemaScript { get; }
        
        public override SchemaWriter CreateSchemaWriter()
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /* =============================================================================================== Logging */

        /// <inheritdoc />
        public override async Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                var unused = await ctx.ExecuteScalarAsync(WriteAuditEventScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        CreateParameter("@EventID", DbType.Int32, auditEvent.EventId),
                        CreateParameter("@Category", DbType.String, 50, (object)auditEvent.Category ?? DBNull.Value),
                        CreateParameter("@Priority", DbType.Int32, auditEvent.Priority),
                        CreateParameter("@Severity", DbType.AnsiString, 30, auditEvent.Severity),
                        CreateParameter("@Title", DbType.String, 256, (object)auditEvent.Title ?? DBNull.Value),
                        CreateParameter("@ContentId", DbType.Int32, auditEvent.ContentId),
                        CreateParameter("@ContentPath", DbType.String, PathMaxLength, (object)auditEvent.ContentPath ?? DBNull.Value),
                        CreateParameter("@UserName", DbType.String, 450, (object)auditEvent.UserName ?? DBNull.Value),
                        CreateParameter("@LogDate", DbType.DateTime, auditEvent.Timestamp),
                        CreateParameter("@MachineName", DbType.AnsiString, 32, (object)auditEvent.MachineName ?? DBNull.Value),
                        CreateParameter("@AppDomainName", DbType.AnsiString, 512, (object)auditEvent.AppDomainName ?? DBNull.Value),
                        CreateParameter("@ProcessID", DbType.AnsiString, 256, auditEvent.ProcessId),
                        CreateParameter("@ProcessName", DbType.AnsiString, 512, (object)auditEvent.ProcessName ?? DBNull.Value),
                        CreateParameter("@ThreadName", DbType.AnsiString, 512, (object)auditEvent.ThreadName ?? DBNull.Value),
                        CreateParameter("@Win32ThreadId", DbType.AnsiString, 128, auditEvent.ThreadId),
                        CreateParameter("@Message", DbType.String, 1500, (object)auditEvent.Message ?? DBNull.Value),
                        CreateParameter("@Formattedmessage", DbType.String, int.MaxValue, (object)auditEvent.FormattedMessage ?? DBNull.Value),
                    });
                });
            }
        }
        protected abstract string WriteAuditEventScript { get; }

        /* =============================================================================================== Provider Tools */

        //public override DateTime RoundDateTime(DateTime d)
        //{
        //    throw new NotImplementedException();
        //}

        public override bool IsCacheableText(string text)
        {
            //UNDONE:DB: Test this feature: unchanged longtextproperties are not preloaded before Update Node

            return text?.Length < DataStore.TextAlternationSizeLimit;
        }

        public override Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /// <inheritdoc />
        public override async Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken = default(CancellationToken))
        {
            RepositoryPath.CheckValidPath(path);
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return (long)await ctx.ExecuteScalarAsync(GetTreeSizeScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        CreateParameter("@IncludeChildren", DbType.Byte, includeChildren ? (byte) 1 : 0),
                        CreateParameter("@NodePath", DbType.String, PathMaxLength, path),
                    });
                });
            }
        }
        protected abstract string GetTreeSizeScript { get; }

        public override Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        public override Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException(new StackTrace().GetFrame(0).GetMethod().Name); //UNDONE:DB@ NotImplementedException
        }

        /* =============================================================================================== Installation */

        /// <inheritdoc />
        public override async Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var ctx = new SnDataContext(this, cancellationToken))
            {
                return await ctx.ExecuteReaderAsync(LoadEntityTreeScript, async reader =>
                {
                    var result = new List<EntityTreeNodeData>();
                    while (await reader.ReadAsync(cancellationToken))
                        result.Add(new EntityTreeNodeData
                        {
                            Id = reader.GetInt32("NodeId"),
                            ParentId = reader.GetSafeInt32("ParentNodeId"),
                            OwnerId = reader.GetSafeInt32("OwnerId")
                        });
                    return result;
                });
            }
        }
        protected abstract string LoadEntityTreeScript { get; }

        /* =============================================================================================== Tools */

        protected abstract long ConvertTimestampToInt64(object timestamp);
        protected abstract object ConvertInt64ToTimestamp(long timestamp);
    }
}
