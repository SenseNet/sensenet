using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Storage.DataModel
{
    /// <summary>
    /// Represents a version of a <see cref="Node"/> in the database usage profile.
    /// </summary>
    public class NodeModel
    {
        public int NodeId { get; set; }
        public int VersionId { get; set; }
        public int ParentNodeId { get; set; }
        public int NodeTypeId { get; set; }
        /// <summary>
        /// Version in the <c>V{major}.{minor}.{status}</c> format (e.g. V1.2.D).
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// True if the VersionId equals with the id of the last public version.
        /// </summary>
        public bool IsLastPublic { get; set; }
        /// <summary>
        /// True if the VersionId equals with the id of the last version.
        /// </summary>
        public bool IsLastDraft { get; set; }
        public int OwnerId { get; set; }
        /// <summary>
        /// Size of the dynamic properties in bytes.
        /// </summary>
        public long DynamicPropertiesSize { get; set; }
        /// <summary>
        /// Size of the content-list properties in bytes.
        /// </summary>
        public long ContentListPropertiesSize { get; set; }
        /// <summary>
        /// Size of the changed data properties in bytes.
        /// </summary>
        public long ChangedDataSize { get; set; }
        /// <summary>
        /// Size of the precompiled index document in bytes.
        /// </summary>
        public long IndexSize { get; set; }
    }
    /// <summary>
    /// Represents a long text property in the database usage profile.
    /// </summary>
    public class LongTextModel
    {
        public int VersionId { get; set; }
        /// <summary>
        /// Size of the text value in bytes.
        /// </summary>
        public long Size { get; set; }
    }
    /// <summary>
    /// Represents a binary property in the database usage profile.
    /// </summary>
    /// <remarks>
    /// The binary property is a linker object between a <see cref="Node"/> and a <c>File</c> representation.
    /// </remarks>
    public class BinaryPropertyModel
    {
        public int VersionId { get; set; }
        public int FileId { get; set; }
    }
    /// <summary>
    /// Represents a blob in the database usage profile.
    /// </summary>
    /// <remarks>
    /// It is connected to a <see cref="Node"/> through a binary property.
    /// If the connection is broken, the file is deletable (orphaned).
    /// </remarks>
    public class FileModel
    {
        public int FileId { get; set; }
        /// <summary>
        /// Size of the stream.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// Size of the stream if it is stored in the built in table otherwise 0.
        /// </summary>
        public long StreamSize { get; set; }
    }

    /// <summary>
    /// Represents aggregated counters.
    /// </summary>
    public class Dimensions
    {
        /// <summary>
        /// Count of rows.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Size of blobs in bytes.
        /// </summary>
        public long Blob { get; set; }
        /// <summary>
        /// Size of metadata in bytes.
        /// </summary>
        /// <remarks>
        /// Summary of the <c>DynamicPropertiesSize</c>, <c>ContentListPropertiesSize</c> and <c>ChangedDataSize</c>
        /// of the related <see cref="NodeModel"/>s.
        /// </remarks>
        public long Metadata { get; set; }
        /// <summary>
        /// Size of long text properties in bytes.
        /// </summary>
        public long Text { get; set; }
        /// <summary>
        /// Size of the precompiled index documents in bytes.
        /// </summary>
        public long Index { get; set; }

        /// <summary>
        /// Combines the given <see cref="Dimensions"/>s and returns a new one. 
        /// </summary>
        public static Dimensions Sum(params Dimensions[] items)
        {
            var d = new Dimensions();
            foreach (var item in items)
            {
                d.Count += item.Count;
                d.Blob += item.Blob;
                d.Metadata += item.Metadata;
                d.Text += item.Text;
                d.Index += item.Index;
            }
            return d;
        }

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        public Dimensions Clone()
        {
            return new()
            {
                Count = Count,
                Blob = Blob,
                Metadata = Metadata,
                Text = Text,
                Index = Index
            };
        }
    }

    public class DatabaseUsageProfile
    {
        public Dimensions System { get; private set; }
        public Dimensions Preview { get; private set; }
        public Dimensions Content { get; private set; }
        public Dimensions OldVersions { get; private set; }
        public long SizeOfOrphanedBlobs { get; private set; }

        private readonly DataProvider _dataProvider;
        public DatabaseUsageProfile(DataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public Task BuildProfileAsync(CancellationToken cancel = default)
        {
            var previewImageTypeId = NodeType.GetByName("PreviewImage").Id;

            var sizeOfOrphanedBlobs = 0L;

            var previewImage = 0;
            var systemLast = 1;
            var contentLast = 2;
            var systemOld = 3;
            var contentOld = 4;

            var db = new (List<int> VersionIds, List<int> FileIds, Dimensions Dimensions)[5];
            for (int i = 0; i < db.Length; i++)
                db[i] = (new List<int>(), new List<int>(), new Dimensions());

            bool ProcessNode(NodeModel node)
            {
                var category = -1;
                if (node.NodeTypeId == previewImageTypeId)
                {
                    category = previewImage;
                }
                else
                {
                    var isSystem = node.OwnerId == 1;
                    var isLast = node.IsLastDraft || node.IsLastPublic;

                    category = isSystem
                        ? isLast ? systemLast : systemOld
                        : isLast ? contentLast : contentOld;
                }

                db[category].VersionIds.Add(node.VersionId);
                var dimensions = db[category].Dimensions;
                dimensions.Count++;
                dimensions.Metadata += node.DynamicPropertiesSize + node.ContentListPropertiesSize + node.ChangedDataSize;
                dimensions.Index += node.IndexSize;

                return false;
            }
            bool ProcessLongText(LongTextModel model)
            {
                for (int i = 0; i < db.Length; i++)
                {
                    if (db[i].VersionIds.Contains(model.VersionId))
                    {
                        db[i].Dimensions.Text += model.Size;
                        break;
                    }
                }
                return false;
            }
            bool ProcessBinary(BinaryPropertyModel model)
            {
                for (int i = 0; i < db.Length; i++)
                {
                    if (db[i].VersionIds.Contains(model.VersionId))
                    {
                        db[i].FileIds.Add(model.FileId);
                        break;
                    }
                }
                return false;
            }
            bool ProcessFile(FileModel model)
            {
                var found = false;
                var size = Math.Max(model.Size, model.StreamSize);
                for (int i = 0; i < db.Length; i++)
                {
                    if (db[i].FileIds.Contains(model.FileId))
                    {
                        db[i].Dimensions.Blob += size;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    sizeOfOrphanedBlobs += size;

                return false;
            }

            _dataProvider.LoadDatabaseUsageProfile(ProcessNode, ProcessLongText, ProcessBinary, ProcessFile);

            this.Preview = db[previewImage].Dimensions.Clone();
            this.System = db[systemLast].Dimensions.Clone();
            this.Content = db[contentLast].Dimensions.Clone();
            this.OldVersions = Dimensions.Sum(db[systemOld].Dimensions, db[contentOld].Dimensions);
            this.SizeOfOrphanedBlobs = sizeOfOrphanedBlobs;

            return Task.CompletedTask;
        }
    }
}
