using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.Storage.DataModel.Usage
{
    public class DatabaseUsageLoader
    {
        private const int PreviewImage = 0;
        private const int SystemLast = 1;
        private const int ContentLast = 2;
        private const int SystemOld = 3;
        private const int ContentOld = 4;
        private int _previewImageTypeId;

        private long _orphanedBlobs;
        private (List<int> VersionIds, List<int> FileIds, Dimensions Dimensions)[] _db;

        private readonly DataProvider _dataProvider;

        public DatabaseUsageLoader(DataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        private void ProcessNode(NodeModel node)
        {
            int category;
            if (node.NodeTypeId == _previewImageTypeId)
            {
                category = PreviewImage;
            }
            else
            {
                var isSystem = node.OwnerId == 1;
                var isLast = node.IsLastDraft || node.IsLastPublic;

                category = isSystem
                    ? isLast ? SystemLast : SystemOld
                    : isLast ? ContentLast : ContentOld;
            }

            _db[category].VersionIds.Add(node.VersionId);
            var dimensions = _db[category].Dimensions;
            dimensions.Count++;
            dimensions.Metadata += node.DynamicPropertiesSize + node.ContentListPropertiesSize + node.ChangedDataSize;
            dimensions.Index += node.IndexSize;
        }
        private void ProcessLongText(LongTextModel model)
        {
            for (int i = 0; i < _db.Length; i++)
            {
                if (_db[i].VersionIds.Contains(model.VersionId))
                {
                    _db[i].Dimensions.Text += model.Size;
                    break;
                }
            }
        }
        private void ProcessBinary(BinaryPropertyModel model)
        {
            for (int i = 0; i < _db.Length; i++)
            {
                if (_db[i].VersionIds.Contains(model.VersionId))
                {
                    _db[i].FileIds.Add(model.FileId);
                    break;
                }
            }
        }
        private void ProcessFile(FileModel model)
        {
            var found = false;
            var size = Math.Max(model.Size, model.StreamSize);
            for (int i = 0; i < _db.Length; i++)
            {
                if (_db[i].FileIds.Contains(model.FileId))
                {
                    _db[i].Dimensions.Blob += size;
                    found = true;
                    break;
                }
            }

            if (!found)
                _orphanedBlobs += size;
        }

        public async Task<DatabaseUsage> LoadAsync(CancellationToken cancel = default)
        {
            using (var op = SnTrace.System.StartOperation("Load DatabaseUsage"))
            {
                var start = DateTime.UtcNow;

                _previewImageTypeId = NodeType.GetByName("PreviewImage").Id;

                _db = new (List<int> VersionIds, List<int> FileIds, Dimensions Dimensions)[5];
                for (int i = 0; i < _db.Length; i++)
                    _db[i] = (new List<int>(), new List<int>(), new Dimensions());

                await _dataProvider.LoadDatabaseUsageAsync(ProcessNode, ProcessLongText, ProcessBinary, ProcessFile,
                    cancel);

                var now = DateTime.UtcNow;
                var result = new DatabaseUsage
                {
                    Preview = _db[PreviewImage].Dimensions.Clone(),
                    System = _db[SystemLast].Dimensions.Clone(),
                    Content = _db[ContentLast].Dimensions.Clone(),
                    OldVersions = Dimensions.Sum(_db[SystemOld].Dimensions, _db[ContentOld].Dimensions),
                    //UNDONE:<?usage: AuditLog = 
                    OrphanedBlobs = _orphanedBlobs,
                    Executed = now,
                    ExecutionTime = now - start,
                };

                op.Successful = true;
                return result;
            }
        }

    }
}
