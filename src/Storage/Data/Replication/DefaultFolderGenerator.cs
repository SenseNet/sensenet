using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

internal interface IFolderGenerator
{
    int CurrentFolderId { get; }
    string CurrentFolderPath { get; }
    Task EnsureFolderAsync(CancellationToken cancel);
}

internal class DefaultFolderGenerator : IFolderGenerator
{
    private readonly ReplicationContext _context;
    private readonly FolderGeneratorNameFieldGenerator _nameFieldGenerator;
    private readonly int _startIndex;

    public int RootFolderId { get; }
    public string RootFolderPath { get; }
    public int MaxItems { get; }
    public int MaxItemsPerFolder { get; }
    public int MaxFoldersPerFolder { get; }

    public int CurrentFolderId { get; private set; }
    public string CurrentFolderPath { get; private set; }

    public DefaultFolderGenerator(ReplicationContext context, int maxItems, int startIndex, int maxItemsPerFolder, int maxFoldersPerFolder)
    {
        _context = context;
        RootFolderId = context.TargetId;
        RootFolderPath = context.TargetPath;
        MaxItems = maxItems;
        MaxItemsPerFolder = maxItemsPerFolder;
        MaxFoldersPerFolder = maxFoldersPerFolder;
        _startIndex = startIndex;

        CurrentFolderId = RootFolderId;
        CurrentFolderPath = RootFolderPath;

        var maxLevels = Convert.ToInt32(Math.Ceiling(Math.Log(Math.Ceiling((0.0d + MaxItems) / MaxItemsPerFolder),
            maxFoldersPerFolder)));
        _ids = new int[maxLevels];
        _names = new string[maxLevels];
        _levels = new int[maxLevels];
        for (int i = 0; i < _levels.Length; i++)
            _levels[i] = maxFoldersPerFolder - 1;

        // Change default name generator
        var nameFieldGeneratorIndex = -1;
        for (int i = 0; i < _context.FieldGenerators.Count; i++)
        {
            if (_context.FieldGenerators[i].GetType() == typeof(NameFieldGenerator))
            {
                nameFieldGeneratorIndex = i;
                break;
            }
        }
        _nameFieldGenerator = new FolderGeneratorNameFieldGenerator();
        _context.FieldGenerators[nameFieldGeneratorIndex] = _nameFieldGenerator;
    }

    private readonly int[] _ids;
    private readonly string[] _names;
    private readonly int[] _levels;
    private int _itemIndex = -1;
    public async Task EnsureFolderAsync(CancellationToken cancel)
    {
        if (_levels.Length == 0)
            return;
        if (++_itemIndex % MaxItemsPerFolder != 0)
            return;
        await CreateLevelAsync(0, cancel);
    }

    private async Task CreateLevelAsync(int level, CancellationToken cancel)
    {
        if (++_levels[level] >= MaxFoldersPerFolder)
        {
            if (level < _levels.Length - 1)
                await CreateLevelAsync(level + 1, cancel);
            _levels[level] = 0;
        }
        await CreateFolderAsync(level, cancel);
    }

    private async Task CreateFolderAsync(int level, CancellationToken cancel)
    {
        var parentId = level == _levels.Length - 1 ? RootFolderId : _ids[level + 1];
        var parentPath = level == _levels.Length - 1 ? RootFolderPath : GetPath(level + 1);
        var name = (_itemIndex + _startIndex).ToString(_context.PaddingFormat);
        var nodeId = await GenerateDataAndIndexAsync(parentId, parentPath, name, cancel);

        _ids[level] = nodeId;
        _names[level] = name;
        CurrentFolderId = nodeId;
        CurrentFolderPath = parentPath + "/" + name;
    }

    private string GetPath(int level)
    {
        var path = RootFolderPath;
        for (var i = _levels.Length - 1; i >= level; i--)
            path = path + "/" + _names[i];
        return path;
    }

    private async Task<int> GenerateDataAndIndexAsync(int parentId, string parentPath, string name, CancellationToken cancel)
    {
        _context.TargetId = parentId;
        _context.TargetPath = parentPath;
        _nameFieldGenerator.Name = name;

        var newFolderId = await _context.GenerateContentAsync(cancel);
        return newFolderId;
    }
}
