using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class SingleNodeReplicationService : IReplicationService
{
    private readonly DataProvider _dataProvider;
    private readonly IIndexManager _indexManager;
    private readonly ILogger<IReplicationService> _logger;

    public SingleNodeReplicationService(DataProvider dataProvider, IIndexManager indexManager, ILogger<IReplicationService> logger)
    {
        _dataProvider = dataProvider;
        _indexManager = indexManager;
        _logger = logger;
    }

    public async Task ReplicateNodeAsync(Node source, Node target, ReplicationDescriptor replicationDescriptor, CancellationToken cancel)
    {
        var sourceData = (await _dataProvider.LoadNodesAsync(new[] { source.VersionId }, cancel)).FirstOrDefault();
        var sourceIndexDoc = (await _dataProvider.LoadIndexDocumentsAsync(new[] { source.VersionId }, cancel)).FirstOrDefault();
        if (sourceData == null || sourceIndexDoc == null)
            throw new InvalidOperationException("Cannot replicate missing source");

        var targetData = (await _dataProvider.LoadNodesAsync(new[] { target.VersionId }, cancel)).FirstOrDefault();
        var targetIndexDoc = (await _dataProvider.LoadIndexDocumentsAsync(new[] { target.VersionId }, cancel)).FirstOrDefault();
        if (targetData == null || targetIndexDoc == null)
            throw new InvalidOperationException("Cannot replicate missing target");

        var timer = Stopwatch.StartNew();
        _logger.LogInformation($"Start replication. Count: {replicationDescriptor.CountMax} Source: {source.Path}, Target: {target.Path}");
        
        // Initialize folder generation
        var folderGenContext = new ReplicationContext(_dataProvider, _indexManager)
        {
            TypeName = target.NodeType.Name.ToLowerInvariant(),
            CountMax = replicationDescriptor.CountMax, // 
            ReplicationStart = DateTime.UtcNow,
            IsSystemContent = target.IsSystem, //target.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder")),
            NodeHeadData = targetData.GetNodeHeadData(),
            VersionData = targetData.GetVersionData(),
            DynamicData = targetData.GetDynamicData(true),
            TargetId = target.Id,
            TargetPath = target.Path
        };
        folderGenContext.Initialize(new ReplicationDescriptor(), targetIndexDoc);
        var folderGenerator = new DefaultFolderGenerator(folderGenContext,
            replicationDescriptor.CountMax,
            replicationDescriptor.FirstFolderIndex,
            replicationDescriptor.MaxItemsPerFolder, replicationDescriptor.MaxFoldersPerFolder);

        // Initialize content generation
        var context = new ReplicationContext(_dataProvider, _indexManager)
        {
            TypeName = source.NodeType.Name.ToLowerInvariant(),
            CountMax = replicationDescriptor.CountMax,
            ReplicationStart = DateTime.UtcNow,
            IsSystemContent = source.IsSystem || target.IsSystem, //source.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder"));
            NodeHeadData = sourceData.GetNodeHeadData(),
            VersionData = sourceData.GetVersionData(),
            DynamicData = sourceData.GetDynamicData(true),
            TargetId = target.Id,
            TargetPath = target.Path
        };
        context.Initialize(replicationDescriptor, sourceIndexDoc);

        // REPLICATION MAIN ENUMERATION
        var lastLogTime = DateTime.UtcNow;
        var logPeriod = TimeSpan.FromSeconds(10.0);
        for (var i = 0; i < context.CountMax; i++)
        {
            await folderGenerator.EnsureFolderAsync(cancel);
            context.TargetId = folderGenerator.CurrentFolderId;
            context.TargetPath = folderGenerator.CurrentFolderPath;

            await context.GenerateContentAsync(cancel);

            if (DateTime.UtcNow - lastLogTime > logPeriod)
            {
                lastLogTime = DateTime.UtcNow;
                _logger.LogInformation($"Replication in progress. " +
                                       $"time: {timer.Elapsed} ({(i + 1) / timer.Elapsed.TotalSeconds} CPS). " +
                                       $"Count: {i + 1}/{replicationDescriptor.CountMax} ({(i + 1) * 100 / replicationDescriptor.CountMax}%)" +
                                       $"Source: {source.Path}, Target: {target.Path}");
            }
        }

        await _indexManager.CommitAsync(cancel);

        timer.Stop();
        var cps = $"{1.0d * replicationDescriptor.CountMax / timer.Elapsed.TotalSeconds:0}";
        _logger.LogInformation($"Replication finished. Total time: {timer.Elapsed} ({cps} CPS). Count: {replicationDescriptor.CountMax} Source: {source.Path}, Target: {target.Path}");
    }
}
