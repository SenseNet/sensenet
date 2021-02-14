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
    public class NodeModel
    {
        public int NodeId { get; set; }
        public int VersionId { get; set; }
        public int ParentNodeId { get; set; }
        public int NodeTypeId { get; set; }
        public string Version { get; set; }
        public bool IsLastPublic { get; set; }
        public bool IsLastDraft { get; set; }
        public int OwnerId { get; set; }
        public long DynamicPropertiesSize { get; set; }
        public long ContentListPropertiesSize { get; set; }
        public long ChangedDataSize { get; set; }
        public long IndexSize { get; set; }
    }
    public class LongTextModel
    {
        public int VersionId { get; set; }
        public long Size { get; set; }
    }
    public class BinaryPropertyModel
    {
        public int VersionId { get; set; }
        public int FileId { get; set; }
    }
    public class FileModel
    {
        public int FileId { get; set; }
        public long Size { get; set; }
        public long StreamSize { get; set; }
    }

    public class Dimensions
    {
        public int Count { get; set; }
        public long Blob { get; set; }
        public long Metadata { get; set; }
        public long Text { get; set; }
        public long Index { get; set; }

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
