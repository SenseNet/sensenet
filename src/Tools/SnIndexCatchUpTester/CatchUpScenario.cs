using System.Text.Json;

sealed record CatchUpScenario(
    ContentState Root, ContentState Untouched, ContentState Update, ContentState Deleted, ContentState DeletedChild,
    ContentState Replaced, ContentState ReplacedChild, ContentState Source, ContentState Moved, ContentState MovedChild,
    ContentState Published, ContentState Draft)
{
    private const int RepeatedUpdateCount = 24;
    private const int DiscardedDraftUpdateCount = 12;
    private const int TemporaryChildCount = 8;
    private const int BulkMoveChildCount = 210;

    public IEnumerable<int> BaselineVersions => new[]
    {
        Root, Untouched, Update, Deleted, DeletedChild, Replaced, ReplacedChild, Source, Moved, MovedChild, Published, Draft
    }.Select(item => item.VersionId);

    public static async Task<CatchUpScenario> PrepareAsync(RepositoryClient client, ContentState root)
    {
        var untouched = await client.CreateFolderAsync(root.Path, "untouched");
        var update = await client.CreateFolderAsync(root.Path, "update-me", "CatchupBaseline");
        var deleted = await client.CreateFolderAsync(root.Path, "delete-me");
        var deletedChild = await client.CreateFolderAsync(deleted.Path, "old-child");
        var replaced = await client.CreateFolderAsync(root.Path, "replace-me");
        var replacedChild = await client.CreateFolderAsync(replaced.Path, "old-child");
        var source = await client.CreateFolderAsync(root.Path, "source");
        var moved = await client.CreateFolderAsync(source.Path, "replace-me");
        var movedChild = await client.CreateFolderAsync(moved.Path, "new-child");
        var published = await client.CreateFileAsync(root.Path, "versioned.txt", "CatchupPublic");
        var draft = await client.CheckOutAsync(published);

        return new CatchUpScenario(root, untouched, update, deleted, deletedChild, replaced, replacedChild, source,
            moved, movedChild, published, draft);
    }

    public async Task<ExpectedIndex> ApplyChangesAsync(RepositoryClient client)
    {
        var created = await client.CreateFolderAsync(Root.Path, "created-after-backup");
        for (var i = 1; i <= RepeatedUpdateCount; i++)
        {
            var displayName = i == RepeatedUpdateCount ? "CatchupFinal" : $"CatchupIntermediate{i:D2}";
            await client.PatchAsync(Update, "DisplayName", displayName);
        }
        Console.WriteLine($"Expected catch-up upsert for VersionId {Update.VersionId} after {RepeatedUpdateCount} updates.");

        for (var i = 1; i <= DiscardedDraftUpdateCount; i++)
            await client.PatchAsync(Draft, "Description", $"CatchupDiscarded{i:D2}");
        await client.UndoCheckOutAsync(Draft, Published);
        Console.WriteLine($"Expected catch-up delete for VersionId {Draft.VersionId} after {DiscardedDraftUpdateCount} draft updates.");

        var temporaryChildren = new List<ContentState>(TemporaryChildCount);
        for (var i = 1; i <= TemporaryChildCount; i++)
            temporaryChildren.Add(await client.CreateFolderAsync(Deleted.Path, $"temporary-{i:D2}"));
        Console.WriteLine($"Temporary versions to delete during catch-up [{string.Join(", ", temporaryChildren.Select(child => child.VersionId))}].");

        // The final AddTree covers more versions than fit in one catch-up write batch.
        var movedChildren = new List<IndexDocumentCheck>(BulkMoveChildCount);
        Console.WriteLine($"Creating {BulkMoveChildCount} children for the subtree move.");
        for (var i = 1; i <= BulkMoveChildCount; i++)
        {
            var name = $"batch-child-{i:D3}";
            var child = await client.CreateFolderAsync(Moved.Path, name);
            movedChildren.Add(new IndexDocumentCheck(child.VersionId, $"{Replaced.Path}/{name}"));
        }

        await client.DeleteTreeAsync(Deleted);
        await client.DeleteTreeAsync(Replaced);
        await client.MoveTreeAsync(Moved, Root.Path, Replaced.Path);
        Console.WriteLine($"The final AddTree covers {movedChildren.Count + 2} versions at {Replaced.Path}.");

        return new ExpectedIndex(
            [
                new(Untouched.VersionId, Untouched.Path), new(created.VersionId, created.Path),
                new(Update.VersionId, Update.Path, "catchupfinal"),
                new(Moved.VersionId, Replaced.Path), new(MovedChild.VersionId, $"{Replaced.Path}/new-child"),
                new(Published.VersionId, Published.Path, "catchuppublic", IsPublicAndDraft: true),
                .. movedChildren
            ],
            [Deleted, DeletedChild, Replaced, ReplacedChild, Draft, .. temporaryChildren]);
    }
}

sealed record ExpectedIndex(IndexDocumentCheck[] Present, ContentState[] Absent)
{
    public async Task<bool> VerifyAsync(RepositoryClient client, string fixturePath, int minimumActivityId)
    {
        var index = await client.ReadIndexAsync();
        var passed = Report(index.LastActivityId >= minimumActivityId && index.Gaps.Length == 0,
            $"Index checkpoint A{index.LastActivityId}, expected minimum A{minimumActivityId}, gaps [{string.Join(", ", index.Gaps)}].");

        foreach (var check in Present)
        {
            if (!index.VersionIds.Contains(check.VersionId))
            {
                passed &= Report(false, $"Version {check.VersionId} at {check.Path} is missing from the index.");
                continue;
            }

            var document = await client.ReadIndexDocumentAsync(fixturePath, check.VersionId);
            var differences = new List<string>();
            CompareField(document, "VersionId", check.VersionId.ToString(System.Globalization.CultureInfo.InvariantCulture), differences);
            CompareField(document, "Path", check.Path.ToLowerInvariant(), differences);
            if (check.DisplayName is not null)
                CompareField(document, "DisplayName", check.DisplayName, differences);
            if (check.IsPublicAndDraft)
            {
                CompareField(document, "IsLastPublic", "yes", differences);
                CompareField(document, "IsLastDraft", "yes", differences);
            }

            var details = differences.Count == 0 ? "field values match" : string.Join(", ", differences);
            passed &= Report(differences.Count == 0, $"Indexed version {check.VersionId} at {check.Path} ({details}).");
        }

        foreach (var item in Absent)
        {
            var missing = !index.VersionIds.Contains(item.VersionId);
            passed &= Report(missing,
                $"Deleted version {item.VersionId} at {item.Path} ({(missing ? "absent from the index" : "still in the index")}).");
        }

        return passed;
    }

    private static void CompareField(JsonElement document, string field, string expected, List<string> differences)
    {
        var actual = document.TryGetProperty(field, out var value) ? value.ToString() : "<missing>";
        if (actual != expected)
            differences.Add($"{field}={actual} (expected {expected})");
    }

    private static bool Report(bool passed, string message)
    {
        Console.WriteLine($"{(passed ? "OK" : "FAIL")} {message}");
        return passed;
    }
}

sealed record IndexDocumentCheck(int VersionId, string Path, string? DisplayName = null, bool IsPublicAndDraft = false);
