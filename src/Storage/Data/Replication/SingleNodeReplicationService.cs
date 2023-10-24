using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class SingleNodeReplicationService : IReplicationService
{
    private readonly DataProvider _dataProvider;
    private readonly IIndexManager _indexManager;
    private readonly ILogger<IReplicationService> _logger;

    //TODO: Consider getting CustomFieldGeneratorFactory from ServiceProvider
    private readonly ICustomFieldGeneratorFactory _customFieldGeneratorFactory = new CustomFieldGeneratorFactory();

    public SingleNodeReplicationService(DataProvider dataProvider, IIndexManager indexManager, ILogger<IReplicationService> logger)
    {
        _dataProvider = dataProvider;
        _indexManager = indexManager;
        _logger = logger;
    }

    public async Task ReplicateNodeAsync(Node source, Node target, ReplicationDescriptor replicationDescriptor, CancellationToken cancel)
    {
        try
        {
            replicationDescriptor.Initialize(source.NodeType, _customFieldGeneratorFactory);

            var sourceData = (await _dataProvider.LoadNodesAsync(new[] {source.VersionId}, cancel)).FirstOrDefault();
            var sourceIndexDoc = (await _dataProvider.LoadIndexDocumentsAsync(new[] {source.VersionId}, cancel))
                .FirstOrDefault();
            if (sourceData == null || sourceIndexDoc == null)
                throw new InvalidOperationException("Cannot replicate missing source");

            var targetData = (await _dataProvider.LoadNodesAsync(new[] {target.VersionId}, cancel)).FirstOrDefault();
            var targetIndexDoc = (await _dataProvider.LoadIndexDocumentsAsync(new[] {target.VersionId}, cancel))
                .FirstOrDefault();
            if (targetData == null || targetIndexDoc == null)
                throw new InvalidOperationException("Cannot replicate missing target");

            var timer = Stopwatch.StartNew();
            _logger.LogInformation(
                $"Replication started. Count: {replicationDescriptor.MaxCount} Source: {source.Path}, Target: {target.Path}");

            // Initialize folder generation
            var folderGenContext = new ReplicationContext(_dataProvider, _indexManager, _customFieldGeneratorFactory)
            {
                TypeName = target.NodeType.Name.ToLowerInvariant(),
                MaxCount = replicationDescriptor.MaxCount, // 
                ReplicationStart = DateTime.UtcNow,
                IsSystemContent =
                    target.IsSystem, //target.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder")),
                NodeHeadData = targetData.GetNodeHeadData(),
                VersionData = targetData.GetVersionData(),
                DynamicData = targetData.GetDynamicData(true),
                TargetId = target.Id,
                TargetPath = target.Path
            };
            folderGenContext.Initialize(new ReplicationDescriptor(), targetIndexDoc);
            var folderGenerator = new DefaultFolderGenerator(folderGenContext,
                replicationDescriptor.MaxCount,
                replicationDescriptor.FirstFolderIndex,
                replicationDescriptor.MaxItemsPerFolder, replicationDescriptor.MaxFoldersPerFolder);

            // Initialize content generation
            var context = new ReplicationContext(_dataProvider, _indexManager, _customFieldGeneratorFactory)
            {
                TypeName = source.NodeType.Name.ToLowerInvariant(),
                MaxCount = replicationDescriptor.MaxCount,
                ReplicationStart = DateTime.UtcNow,
                IsSystemContent =
                    source.IsSystem ||
                    target.IsSystem, //source.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder"));
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
            for (var i = 0; i < context.MaxCount; i++)
            {
                // stop generating in case of shutdown
                if (cancel.IsCancellationRequested)
                {
                    _logger.LogWarning("Replication cancelled after {count} generated items.", i);
                    break;
                }

                await folderGenerator.EnsureFolderAsync(cancel);
                context.TargetId = folderGenerator.CurrentFolderId;
                context.TargetPath = folderGenerator.CurrentFolderPath;

                await context.GenerateContentAsync(cancel);

                if (DateTime.UtcNow - lastLogTime > logPeriod)
                {
                    lastLogTime = DateTime.UtcNow;
                    var time = (i + 1) / timer.Elapsed.TotalSeconds;
                    _logger.LogInformation($"Replication in progress. " +
                                           $"time: {timer.Elapsed:hh\\:mm\\:ss} ({time:F0} CPS). " +
                                           $"Count: {i + 1}/{replicationDescriptor.MaxCount} ({(i + 1) * 100 / replicationDescriptor.MaxCount}%)" +
                                           $"Source: {source.Path}, Target: {target.Path}");
                }
            }

            await _indexManager.CommitAsync(cancel);

            timer.Stop();
            var cps = $"{1.0d * replicationDescriptor.MaxCount / timer.Elapsed.TotalSeconds:0}";
            _logger.LogInformation(
                $"Replication finished. Total time: {timer.Elapsed:hh\\:mm\\:ss} ({cps} CPS). Count: {replicationDescriptor.MaxCount} Source: {source.Path}, Target: {target.Path}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Replication error.");
        }
    }
}
