using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

internal class IFolderGenerator
{
}

internal class DefaultFolderGenerator : IFolderGenerator
{
    private ReplicationContext _context;
    private CancellationToken _cancel;

    public int RootFolderId { get; }
    public string RootFolderPath { get; }
    public int MaxItems { get; }
    public int MaxItemsPerFolder { get; }
    public int MaxFoldersPerFolder { get; }

    public int CurrentFolderId { get; private set; }
    public string CurrentFolderPath { get; private set; }

    public DefaultFolderGenerator(ReplicationContext context, int maxItems, int maxItemsPerFolder, int maxFoldersPerFolder, CancellationToken cancel)
    {
        _context = context;
        RootFolderId = context.TargetId;
        RootFolderPath = context.TargetPath;
        MaxItems = maxItems;
        MaxItemsPerFolder = maxItemsPerFolder;
        MaxFoldersPerFolder = maxFoldersPerFolder;
        _cancel = cancel;

        var maxLevels = Convert.ToInt32(Math.Ceiling(Math.Log(Math.Ceiling(0.0d + MaxItems / MaxItemsPerFolder),
            maxFoldersPerFolder)));
        _ids = new int[maxLevels];
        _names = new string[maxLevels];
        _levels = new int[maxLevels];
        for (int i = 0; i < _levels.Length; i++)
            _levels[i] = maxFoldersPerFolder - 1;
    }

    private readonly int[] _ids;
    private readonly string[] _names;
    private readonly int[] _levels;
    private int _itemIndex = -1;
    public void EnsureFolder()
    {
        if (++_itemIndex % MaxItemsPerFolder != 0)
            return;
        CreateLevel(0);
    }

    private void CreateLevel(int level)
    {
        if (++_levels[level] >= MaxFoldersPerFolder)
        {
            if (level < _levels.Length - 1)
                CreateLevel(level + 1);
            _levels[level] = 0;
        }
        CreateFolder(level);
    }

    private void CreateFolder(int level)
    {
        var parentId = level == _levels.Length - 1 ? RootFolderId : _ids[level + 1];
        var parentPath = level == _levels.Length - 1 ? RootFolderPath : GetPath(level + 1);
        var nodeId = GenerateDataAndIndex(parentId, parentPath, _itemIndex.ToString());

        var name = _itemIndex.ToString();
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

    private int _lastId = 100_000_000;
    private int GenerateDataAndIndex(int parentId, string parentPath, string name)
    {

        return ++_lastId;
        throw new NotImplementedException();
    }
}
