#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SenseNet.Search.Indexing.Integrity;

public static class IndexIntegrityComparer
{
    public static CheckCounts Compare(IEnumerable<DatabaseVersion> database, IEnumerable<SavedIndexVersion> index,
        DifferenceReporter reporter, Func<int, bool> markDraftPath, CancellationToken cancellationToken)
    {
        using var db = database.GetEnumerator();
        using var ix = index.GetEnumerator();
        var hasDb = db.MoveNext();
        var hasIx = ix.MoveNext();
        var lastDb = 0;
        var lastIx = 0;
        long dbCount = 0, ixCount = 0, matched = 0;
        while (hasDb || hasIx)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hasDb && db.Current.VersionId <= lastDb)
                throw new InvalidDataException("Database snapshot must be ordered by unique positive VersionId.");
            if (hasIx && (ix.Current.VersionId <= 0 || ix.Current.VersionId < lastIx))
                throw new InvalidDataException("Index snapshot must be ordered by positive VersionId.");
            if (hasDb && (!hasIx || db.Current.VersionId < ix.Current.VersionId))
            {
                var row = db.Current;
                reporter.Report(new(DifferenceKind.NotInIndex, row.VersionId, NodeId: row.NodeId) { Database = row });
                lastDb = row.VersionId;
                dbCount++;
                hasDb = db.MoveNext();
                continue;
            }

            var version = ix.Current.VersionId;
            var expected = hasDb && db.Current.VersionId == version ? db.Current : (DatabaseVersion?)null;
            var first = true;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = ix.Current;
                if (!first)
                    reporter.Report(new(DifferenceKind.MoreDocument, version, row.DocId, row.Metadata.NodeId)
                        { Database = expected, Index = row });
                first = false;
                if (expected is { } stored)
                {
                    CompareVersion(stored, row, reporter, markDraftPath);
                    matched++;
                }
                else
                {
                    reporter.Report(new(DifferenceKind.NotInDatabase, version, row.DocId, row.Metadata.NodeId)
                        { Index = row });
                }
                lastIx = version;
                ixCount++;
                hasIx = ix.MoveNext();
            } while (hasIx && ix.Current.VersionId == version);

            if (expected.HasValue)
            {
                lastDb = version;
                dbCount++;
                hasDb = db.MoveNext();
            }
        }
        return new(dbCount, ixCount, matched);
    }

    public static void CompareVersion(DatabaseVersion db, SavedIndexVersion row, DifferenceReporter reporter,
        Func<int, bool> markDraftPath)
    {
        var ix = row.Metadata;
        void Different(DifferenceKind kind, string field, object expected, object actual) =>
            reporter.Report(new(kind, row.VersionId, row.DocId, db.NodeId, field, expected, actual)
                { Database = db, Index = row });

        if ((ix.Present & Fields.NodeId) != 0 && ix.NodeId != db.NodeId)
            Different(DifferenceKind.DifferentNodeId, "Id", db.NodeId, ix.NodeId);
        if ((ix.Present & Fields.VersionTimestamp) != 0 && ix.VersionTimestamp != db.VersionTimestamp)
            Different(DifferenceKind.DifferentVersionTimestamp, "VersionTimestamp", db.VersionTimestamp, ix.VersionTimestamp);
        if ((ix.Present & Fields.IsLastPublic) != 0 && ix.IsLastPublic != (db.VersionId == db.LastMajorVersionId))
            Different(DifferenceKind.DifferentLastPublicFlag, "IsLastPublic", db.VersionId == db.LastMajorVersionId, ix.IsLastPublic);
        if ((ix.Present & Fields.IsLastDraft) != 0 && ix.IsLastDraft != (db.VersionId == db.LastMinorVersionId))
            Different(DifferenceKind.DifferentLastDraftFlag, "IsLastDraft", db.VersionId == db.LastMinorVersionId, ix.IsLastDraft);
        if ((ix.Present & Fields.NodeTimestamp) == 0 || ix.NodeTimestamp == db.NodeTimestamp)
            return;

        if (!ix.IsLastDraft)
        {
            if (ix.MissingDraftPath && markDraftPath(ix.DraftDocId))
                reporter.Report(new(DifferenceKind.MissingField,
                    VersionId: ix.DraftVersionId > 0 ? ix.DraftVersionId : null, DocId: ix.DraftDocId, Field: "Path"));
            if (ix.MissingOwnPath)
                reporter.Report(new(DifferenceKind.MissingField, DocId: row.DocId, Field: "Path")
                    { Database = db, Index = row });
        }
        if (ix.IsLastDraft || !ix.HistoricalPathMatches)
            Different(DifferenceKind.DifferentNodeTimestamp, "NodeTimestamp", db.NodeTimestamp, ix.NodeTimestamp);
    }
}
