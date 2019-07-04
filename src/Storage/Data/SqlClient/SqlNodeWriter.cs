using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using SenseNet.ContentRepository.Storage.Schema;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    [Obsolete("##", true)]
    public class SqlNodeWriter : INodeWriter
    {
        private FlatPropertyWriter _flatWriter;
        private TextPropertyWriter _textWriter;

        public void Open()
        {
            // do nothing
        }
        public void Close()
        {
            if (_flatWriter != null)
                _flatWriter.Execute();
            if (_textWriter != null)
                _textWriter.Execute();
        }

        // ============================================================================ "less roundtrip methods"

        public virtual void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            using (var cmd = new SqlProcedure { CommandText = "proc_NodeAndVersion_Insert" })
            {
                cmd.Parameters.Add("@NodeTypeId", SqlDbType.Int).Value = nodeData.NodeTypeId;
                cmd.Parameters.Add("@ContentListTypeId", SqlDbType.Int).Value = (nodeData.ContentListTypeId != 0) ? (object)nodeData.ContentListTypeId : DBNull.Value;
                cmd.Parameters.Add("@ContentListId", SqlDbType.Int).Value = (nodeData.ContentListId != 0) ? (object)nodeData.ContentListId : DBNull.Value;
                cmd.Parameters.Add("@CreatingInProgress", SqlDbType.TinyInt).Value = nodeData.CreatingInProgress;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.TinyInt).Value = nodeData.IsDeleted ? 1 : 0;
                cmd.Parameters.Add("@IsInherited", SqlDbType.TinyInt).Value = 0;
                cmd.Parameters.Add("@ParentNodeId", SqlDbType.Int).Value = (nodeData.ParentId > 0) ? (object)nodeData.ParentId : DBNull.Value;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 450).Value = nodeData.Name;
                cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 450).Value = (object)nodeData.DisplayName ?? DBNull.Value;
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = nodeData.Path;
                cmd.Parameters.Add("@Index", SqlDbType.Int).Value = nodeData.Index;
                cmd.Parameters.Add("@Locked", SqlDbType.TinyInt).Value = nodeData.Locked ? 1 : 0;
                cmd.Parameters.Add("@LockedById", SqlDbType.Int).Value = (nodeData.LockedById > 0) ? (object)nodeData.LockedById : DBNull.Value;
                cmd.Parameters.Add("@ETag", SqlDbType.VarChar, 50).Value = nodeData.ETag ?? String.Empty;
                cmd.Parameters.Add("@LockType", SqlDbType.Int).Value = nodeData.LockType;
                cmd.Parameters.Add("@LockTimeout", SqlDbType.Int).Value = nodeData.LockTimeout;
                cmd.Parameters.Add("@LockDate", SqlDbType.DateTime).Value = nodeData.LockDate;
                cmd.Parameters.Add("@LockToken", SqlDbType.VarChar, 50).Value = nodeData.LockToken ?? String.Empty;
                cmd.Parameters.Add("@LastLockUpdate", SqlDbType.DateTime).Value = nodeData.LastLockUpdate;
                cmd.Parameters.Add("@NodeCreationDate", SqlDbType.DateTime).Value = nodeData.CreationDate;
                cmd.Parameters.Add("@NodeCreatedById", SqlDbType.Int).Value = nodeData.CreatedById;
                cmd.Parameters.Add("@NodeModificationDate", SqlDbType.DateTime).Value = nodeData.ModificationDate;
                cmd.Parameters.Add("@NodeModifiedById", SqlDbType.Int).Value = nodeData.ModifiedById;

                cmd.Parameters.Add("@IsSystem", SqlDbType.TinyInt).Value = nodeData.IsSystem ? 1 : 0;
                cmd.Parameters.Add("@OwnerId", SqlDbType.Int).Value = nodeData.OwnerId;
                cmd.Parameters.Add("@SavingState", SqlDbType.Int).Value = (int)nodeData.SavingState;
                cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
                cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
                cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.VersionCreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.VersionCreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.VersionModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.VersionModifiedById;

                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    nodeData.Id = Convert.ToInt32(reader[0]);
                    nodeData.VersionId = Convert.ToInt32(reader[1]);
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[2]);
                    nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[3]);

                    lastMajorVersionId = reader.GetSafeInt32(4);
                    lastMinorVersionId = reader.GetSafeInt32(5);

                    nodeData.Path = reader.GetSafeString(6);
                }
            }
        }

        /*============================================================================ Node Insert/Update */

        public virtual void UpdateSubTreePath(string oldPath, string newPath)
        {
            if (oldPath == null)
                throw new ArgumentNullException("oldPath");
            if (newPath == null)
                throw new ArgumentNullException("newPath");

            if (oldPath.Length == 0)
                throw new ArgumentException("Old path cannot be empty.", "oldPath");
            if (newPath.Length == 0)
                throw new ArgumentException("New path cannot be empty.", "newPath");

            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_UpdateSubTreePath" };
                cmd.Parameters.Add("@OldPath", SqlDbType.NVarChar, 450).Value = oldPath;
                cmd.Parameters.Add("@NewPath", SqlDbType.NVarChar, 450).Value = newPath;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }
        public virtual void UpdateNodeRow(NodeData nodeData)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_Update" };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
                cmd.Parameters.Add("@NodeTypeId", SqlDbType.Int).Value = nodeData.NodeTypeId;
                cmd.Parameters.Add("@ContentListTypeId", SqlDbType.Int).Value = (nodeData.ContentListTypeId != 0) ? (object)nodeData.ContentListTypeId : DBNull.Value;
                cmd.Parameters.Add("@ContentListId", SqlDbType.Int).Value = (nodeData.ContentListId != 0) ? (object)nodeData.ContentListId : DBNull.Value;
                cmd.Parameters.Add("@CreatingInProgress", SqlDbType.TinyInt).Value = nodeData.CreatingInProgress ? 1 : 0;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.TinyInt).Value = nodeData.IsDeleted ? 1 : 0;
                cmd.Parameters.Add("@IsInherited", SqlDbType.TinyInt).Value = 0;
                cmd.Parameters.Add("@ParentNodeId", SqlDbType.Int).Value = (nodeData.ParentId > 0) ? (object)nodeData.ParentId : DBNull.Value;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 450).Value = nodeData.Name;
                cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 450).Value = (object)nodeData.DisplayName ?? DBNull.Value;
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = nodeData.Path;
                cmd.Parameters.Add("@Index", SqlDbType.Int).Value = nodeData.Index;
                cmd.Parameters.Add("@Locked", SqlDbType.TinyInt).Value = nodeData.Locked ? 1 : 0;
                cmd.Parameters.Add("@LockedById", SqlDbType.Int).Value = (nodeData.LockedById > 0) ? (object)nodeData.LockedById : DBNull.Value;
                cmd.Parameters.Add("@ETag", SqlDbType.VarChar, 50).Value = nodeData.ETag ?? String.Empty;
                cmd.Parameters.Add("@LockType", SqlDbType.Int).Value = nodeData.LockType;
                cmd.Parameters.Add("@LockTimeout", SqlDbType.Int).Value = nodeData.LockTimeout;
                cmd.Parameters.Add("@LockDate", SqlDbType.DateTime).Value = nodeData.LockDate;
                cmd.Parameters.Add("@LockToken", SqlDbType.VarChar, 50).Value = nodeData.LockToken ?? String.Empty;
                cmd.Parameters.Add("@LastLockUpdate", SqlDbType.DateTime).Value = nodeData.LastLockUpdate;

                cmd.Parameters.Add("@IsSystem", SqlDbType.TinyInt).Value = nodeData.IsSystem ? 1 : 0;
                cmd.Parameters.Add("@OwnerId", SqlDbType.Int).Value = nodeData.OwnerId;
                cmd.Parameters.Add("@SavingState", SqlDbType.Int).Value = (int)nodeData.SavingState;

                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.CreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.CreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.ModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.ModifiedById;
                cmd.Parameters.Add("@NodeTimestamp", SqlDbType.Timestamp).Value = SqlProvider.GetBytesFromLong(nodeData.NodeTimestamp);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                	// SELECT [Path], [Timestamp] FROM Nodes WHERE NodeId = @NodeId
                    nodeData.Path = reader.GetSafeString(0);
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[1]);
                }
            }
            catch (SqlException sex) // rethrow
            {
                if (sex.Message.StartsWith("Node is out of date"))
                {
                    StorageContext.L2Cache.Clear();
                    throw new NodeIsOutOfDateException(nodeData.Id, nodeData.Path, nodeData.VersionId, nodeData.Version, sex, nodeData.NodeTimestamp);
                }
                else if (sex.Message.StartsWith("Cannot update a deleted Node"))
                {
                    StorageContext.L2Cache.Clear();
                }
                else
                {
                    throw new DataException(sex.Message, sex);
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }
        }

        /*============================================================================ Version Insert/Update */

        public virtual void UpdateVersionRow(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;

            lastMajorVersionId = 0;
            lastMinorVersionId = 0;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Version_Update" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = nodeData.VersionId;
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
                if (nodeData.IsPropertyChanged("Version"))
                {
                    cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
                    cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
                    cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
                }
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.VersionCreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.VersionCreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.VersionModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.VersionModifiedById;
                cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // SELECT [Timestamp] FROM Versions WHERE VersionId = @VersionId
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[0]);
                    nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[1]);

                    lastMajorVersionId = reader.GetSafeInt32(2);
                    lastMinorVersionId = reader.GetSafeInt32(3);
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }
        }
        public virtual void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            CopyAndUpdateVersion(nodeData, previousVersionId, 0, out lastMajorVersionId, out lastMinorVersionId);
        }
        public virtual void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, int destinationVersionId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            lastMajorVersionId = 0;
            lastMinorVersionId = 0;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Version_CopyAndUpdate" };
                cmd.Parameters.Add("@PreviousVersionId", SqlDbType.Int).Value = previousVersionId;
                cmd.Parameters.Add("@DestinationVersionId", SqlDbType.Int).Value = (destinationVersionId != 0) ? (object)destinationVersionId : DBNull.Value;
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
                cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
                cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
                cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.VersionCreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.VersionCreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.VersionModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.VersionModifiedById;
                cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // SELECT VersionId, [Timestamp] FROM Versions WHERE VersionId = @NewVersionId
                    nodeData.VersionId = Convert.ToInt32(reader[0]);
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[1]);
                    nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[2]);

                    lastMajorVersionId = reader.GetSafeInt32(3);
                    lastMinorVersionId = reader.GetSafeInt32(4);
                }
                if (reader.NextResult())
                {
                    // SELECT BinaryPropertyId, PropertyTypeId FROM BinaryProperties WHERE VersionId = @NewVersionId
                    while (reader.Read())
                    {
                        var binId = Convert.ToInt32(reader[0]);
                        var propId = Convert.ToInt32(reader[1]);
                        var binaryData = (BinaryDataValue)nodeData.GetDynamicRawData(propId);
                        binaryData.Id = binId;
                    }
                }

            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();

                cmd.Dispose();
            }
        }

        // ============================================================================ Property Insert/Update

        public virtual void SaveStringProperty(int versionId, PropertyType propertyType, string value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteStringProperty(value, propertyType);
        }
        public virtual void SaveDateTimeProperty(int versionId, PropertyType propertyType, DateTime value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteDateTimeProperty(value, propertyType);
        }
        public virtual void SaveIntProperty(int versionId, PropertyType propertyType, int value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteIntProperty(value, propertyType);
        }
        public virtual void SaveCurrencyProperty(int versionId, PropertyType propertyType, decimal value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteCurrencyProperty(value, propertyType);
        }
        public virtual void SaveTextProperty(int versionId, PropertyType propertyType, bool isLoaded, string value)
        {
            if (_textWriter == null)
                _textWriter = new TextPropertyWriter(versionId);

            _textWriter.Write(value, propertyType, isLoaded);
        }
        public virtual void SaveReferenceProperty(int versionId, PropertyType propertyType, IEnumerable<int> value)
        {
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");

            for (short tryAgain = 3; tryAgain > 0; tryAgain --)
            {
                // Optimistic approach: try to save the value as is, without checking and compensate if it fails
                try
                {
                    // Create XML
                    var referredListXml = SqlProvider.CreateIdXmlForReferencePropertyUpdate(value);

                    // Execute SQL
                    using (var cmd = new SqlProcedure { CommandText = "proc_ReferenceProperty_Update" })
                    {
                        cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                        cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyType.Id;
                        cmd.Parameters.Add("@ReferredNodeIdListXml", SqlDbType.Xml).Value = referredListXml;
                        cmd.ExecuteNonQuery();

                        // Success, don't try again
                        tryAgain = 0;
                    }
                }
                catch (SqlException exc)
                {
                    // This was the last try and it failed, throw
                    if (tryAgain == 1)
                        throw;

                    // The value contains a node ID which no longer exists in the database, let's compensate for that
                    if (exc.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_ReferenceProperties_Nodes\"."))
                    {
                        // Get node heads for the IDs
                        var heads = DataProvider.Current.LoadNodeHeads(value); //DB:ok
                        // Select the IDs of the existing node heads
                        value = heads.Where(h => h != null).Select(h => h.Id);
                    }
                    else
                        // If the error is something else, just throw it up
                        throw;
                }
            }
        }


        public virtual void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            BlobStorage.InsertBinaryProperty(value, versionId, propertyTypeId, isNewNode);
        }
        public virtual void UpdateBinaryProperty(BinaryDataValue value)
        {
            BlobStorage.UpdateBinaryProperty(value);
        }
        public virtual void DeleteBinaryProperty(int versionId, PropertyType propertyType)
        {
            BlobStorage.DeleteBinaryProperty(versionId, propertyType.Id);
        }

    }
}
