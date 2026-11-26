#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SenseNet.Search.Indexing.Integrity;

public readonly record struct DatabaseVersion(int VersionId, long VersionTimestamp, int NodeId,
    long NodeTimestamp, int? LastMajorVersionId, int? LastMinorVersionId,
    [property: JsonIgnore] DateTime? NodeModificationDateUtc = null,
    [property: JsonIgnore] DateTime? VersionModificationDateUtc = null,
    [property: JsonIgnore] bool IndexingEnabled = true);

public readonly record struct IndexVersion(int VersionId, int DocId);
public readonly record struct SavedIndexVersion(int VersionId, int DocId, IndexMetadata Metadata);
public sealed record CheckCounts(long DatabaseVersions, long IndexDocuments, long MatchedDocuments);

public enum DifferenceKind
{
    NotInIndex, NotInDatabase, MoreDocument, DifferentNodeId, DifferentNodeTimestamp,
    DifferentVersionTimestamp, DifferentLastPublicFlag, DifferentLastDraftFlag,
    MissingField, InvalidField, MultipleFieldValues
}

public sealed record Difference(DifferenceKind Kind, int? VersionId = null, int? DocId = null,
    int? NodeId = null, string? Field = null, object? Expected = null, object? Actual = null)
{
    [JsonIgnore] public DatabaseVersion? Database { get; init; }
    [JsonIgnore] public SavedIndexVersion? Index { get; init; }
}

public sealed class DifferenceReporter(Action<Difference> write)
{
    public long Count { get; private set; }
    public Dictionary<DifferenceKind, long> Counts { get; } = new();

    public void Report(Difference difference)
    {
        Count++;
        Counts[difference.Kind] = Counts.GetValueOrDefault(difference.Kind) + 1;
        write(difference);
    }
}

[Flags]
public enum Fields : byte
{
    None = 0, NodeId = 1, NodeTimestamp = 2, VersionTimestamp = 4,
    IsLastPublic = 8, IsLastDraft = 16, VersionId = 32,
    Metadata = NodeId | NodeTimestamp | VersionTimestamp | IsLastPublic | IsLastDraft
}

public struct IndexMetadata
{
    public int NodeId, UniqueVersionId;
    public long NodeTimestamp, VersionTimestamp;
    public Fields Present;
    public bool IsLastPublic, IsLastDraft;
    public bool IsCommit, HistoricalPathMatches, MissingOwnPath, MissingDraftPath, DraftPathReported;
    public bool AmbiguousVersionId;
    public int DraftDocId, DraftVersionId;
}
