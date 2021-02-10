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
    public class DatabaseUsageProfile
    {
        public class NodeModel
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public NodeType Type { get; set; }
            public int OwnerId { get; set; }
            public string Name { get; set; }
            public VersionModel LastPublicVersion { get; set; }
            public VersionModel LastDraftVersion { get; set; }
            public List<VersionModel> OldVersions { get; set; }

            public NodeModel Parent { get; set; }
            public List<NodeModel> Children { get; } = new List<NodeModel>();
        }

        public class VersionModel
        {
            public VersionNumber Version { get; set; }
            public long DynamicPropertiesSize { get; set; }
            public long ContentListPropertiesSize { get; set; }
            public long ChangedDataSize { get; set; }
            public long IndexSize { get; set; }
            public long LongTextSizes { get; set; }
            public long BlobSizes { get; set; }
        }

        public class DatabaseUsageModel
        {
            public NodeModel Root { get; set; }
            public NodeModel[] Nodes { get; set; }
            public long SizeOfOrphanedBlobs { get; set; }
        }

        public class Dimensions
        {
            public long Blob { get; set; }
            public long Metadata { get; set; }
            public long Text { get; set; }
            public long Index { get; set; }
        }

        public Dimensions System { get; private set; }
        public Dimensions Preview { get; private set; }
        public Dimensions Content { get; private set; }
        public long SizeOfOrphanedBlobs { get; private set; }

        private readonly DataProvider _dataProvider;
        public DatabaseUsageProfile(DataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public async Task BuildProfileAsync(CancellationToken cancel = default)
        {
            var model = await LoadDatabaseUsageModelAsync(cancel);

            SizeOfOrphanedBlobs = model.SizeOfOrphanedBlobs;
            Preview = BuildDimensions(GetPreviewNodes(model));

            throw new NotImplementedException();
        }

        private async Task<DatabaseUsageModel> LoadDatabaseUsageModelAsync(CancellationToken cancel)
        {
            var model = await _dataProvider.GetUsageModelAsync(cancel);
            BuildTree(model);
            return model;
        }

        private void BuildTree(DatabaseUsageModel model)
        {
            throw new NotImplementedException(); //UNDONE:<?usage: NotImplementedException
        }

        private Dimensions BuildDimensions(NodeModel[] nodes)
        {
            throw new NotImplementedException(); //UNDONE:<?usage: NotImplementedException
        }

        private NodeModel[] GetPreviewNodes(DatabaseUsageModel model)
        {
            var previewImageType = NodeType.GetByName("PreviewImage");
            var folders = new Dictionary<int, NodeModel>();
            var images = model.Nodes.Where(x => x.Type == previewImageType).ToArray();
            foreach (var node in images)
            {
                var grandParent = node.Parent?.Parent;
                if(grandParent == null)
                    continue;
                
                if (!folders.ContainsKey(grandParent.Id))
                {
                    folders.Add(grandParent.Id, grandParent);
                    foreach(var child in grandParent.Children)
                        folders.Add(child.Id, child);
                }
            }

            return folders.Values.Union(images).ToArray();
        }
    }

}
