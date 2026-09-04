using System.Text.Json;

namespace SnDbMigrator;

/// <summary>
/// Tracks migration progress so that a failed migration can be resumed
/// from the last completed table instead of starting over.
/// </summary>
public sealed class Checkpoint
{
    private string _filePath;

    public Dictionary<string, TableCheckpoint> Tables { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Checkpoint() : this("") { }

    public Checkpoint(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>Load checkpoint from disk, or create a new one.</summary>
    public static Checkpoint Load(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var cp = JsonSerializer.Deserialize<Checkpoint>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            cp._filePath = filePath;
            return cp;
        }

        return new Checkpoint(filePath) { StartedAt = DateTime.UtcNow };
    }

    public bool IsTableCompleted(string tableName)
        => Tables.TryGetValue(tableName, out var tc) && tc.Completed;

    public long GetLastId(string tableName)
        => Tables.TryGetValue(tableName, out var tc) ? tc.LastId : 0;

    public void MarkTableProgress(string tableName, long lastId, long rowsMigrated)
    {
        if (!Tables.TryGetValue(tableName, out var tc))
        {
            tc = new TableCheckpoint();
            Tables[tableName] = tc;
        }
        tc.LastId = lastId;
        tc.RowsMigrated = rowsMigrated;
        Save();
    }

    public void MarkTableCompleted(string tableName, long totalRows)
    {
        if (!Tables.TryGetValue(tableName, out var tc))
        {
            tc = new TableCheckpoint();
            Tables[tableName] = tc;
        }
        tc.Completed = true;
        tc.RowsMigrated = totalRows;
        tc.CompletedAt = DateTime.UtcNow;
        Save();
    }

    public void MarkMigrationCompleted()
    {
        CompletedAt = DateTime.UtcNow;
        Save();
    }

    public void Delete()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_filePath, json);
    }
}

public sealed class TableCheckpoint
{
    public bool Completed { get; set; }
    public long LastId { get; set; }
    public long RowsMigrated { get; set; }
    public DateTime? CompletedAt { get; set; }
}
