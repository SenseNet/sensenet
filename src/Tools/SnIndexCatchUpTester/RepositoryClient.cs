using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

sealed class RepositoryClient(HttpClient http)
{
    private const string Fields = "metadata=no&$select=Id,Path,Type,VersionId,DisplayName,Locked";
    private const string IndexRoot = "('Root')";

    public async Task<ContentState> ReadContentAsync(string path) =>
        ReadContent(await SendAsync(HttpMethod.Get, $"{Entity(path)}?{Fields}"), path);

    public async Task EnsurePathIsFreeAsync(string path)
    {
        try
        {
            await ReadContentAsync(path);
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        throw new InvalidOperationException($"A tesztmappa már létezik. Válassz új futásnevet. {path}");
    }

    public async Task VerifyFixtureAsync(ContentState fixture)
    {
        var current = await ReadContentAsync(fixture.Path);
        if (current.Id != fixture.Id || current.Type != "Folder" || current.DisplayName != fixture.DisplayName)
            throw new InvalidOperationException($"A tesztmappa eltér az előkészített példánytól. {fixture.Path}");
    }

    public async Task<ContentState> CreateFolderAsync(string parent, string name, string? displayName = null)
    {
        var folder = await CreateAsync(parent, name, new
        {
            __ContentType = "Folder", Name = name, DisplayName = displayName ?? name,
            InheritableVersioningMode = "1", InheritableApprovingMode = "1"
        });
        Console.WriteLine($"Mappa létrehozva {folder.Path}");
        return folder;
    }

    public async Task<ContentState> CreateFileAsync(string parent, string name, string displayName)
    {
        var file = await CreateAsync(parent, name, new
        {
            __ContentType = "File", Name = name, DisplayName = displayName,
            VersioningMode = "1", ApprovingMode = "1"
        });
        Console.WriteLine($"Fájl létrehozva {file.Path}");
        return file;
    }

    public async Task<ContentState> CheckOutAsync(ContentState file)
    {
        var result = await SendAsync(HttpMethod.Post, $"{Entity(file.Id)}/CheckOut?{Fields}", new { });
        var draft = ReadContent(result, file.Path);
        if (!draft.Locked || draft.VersionId == file.VersionId)
            throw new InvalidOperationException($"A checkout nem hozott létre külön, zárolt verziót. {file.Path}");

        Console.WriteLine($"Fájl kivéve szerkesztésre {file.Path} (piszkozat {draft.VersionId})");
        return draft;
    }

    public async Task UndoCheckOutAsync(ContentState draft, ContentState published)
    {
        await SendAsync(HttpMethod.Post, $"{Entity(draft.Id)}/UndoCheckOut?metadata=no", new { });
        var current = await ReadContentAsync(draft.Path);
        if (current.Locked || current.VersionId != published.VersionId || current.DisplayName != published.DisplayName)
            throw new InvalidOperationException($"A checkout visszavonása után eltér a publikus verzió. {draft.Path}");

        Console.WriteLine($"Checkout visszavonva {draft.Path} (eldobott verzió {draft.VersionId})");
    }

    public async Task PatchAsync(ContentState item, string field, string value)
    {
        var response = await SendAsync(HttpMethod.Patch, $"{Entity(item.Id)}?metadata=no&$select=VersionId,{field}",
            new Dictionary<string, string> { [field] = value });
        var updated = response.GetProperty("d");
        if (updated.GetProperty("VersionId").GetInt32() != item.VersionId || updated.GetProperty(field).GetString() != value)
            throw new InvalidOperationException($"A módosított mező vagy verzió eltér a várttól. {item.Path} ({field})");

        Console.WriteLine($"Tartalom módosítva {item.Path} ({field}={value})");
    }

    public async Task DeleteTreeAsync(ContentState folder)
    {
        await SendAsync(HttpMethod.Post, $"{Entity(folder.Id)}/Delete", new { permanent = true });
        Console.WriteLine($"Részfa végleg törölve {folder.Path}");
    }

    public async Task MoveTreeAsync(ContentState folder, string targetParent, string expectedPath)
    {
        var result = (await SendAsync(HttpMethod.Post, $"{Entity(targetParent)}/MoveBatch", new
        {
            targetPath = targetParent, paths = new[] { folder.Id }
        })).GetProperty("d");
        var errors = result.GetProperty("errors");
        if (errors.GetArrayLength() != 0)
            throw new InvalidOperationException($"MoveBatch hiba {folder.Path}: {JsonSerializer.Serialize(errors)}");

        var moved = result.GetProperty("results");
        if (moved.GetArrayLength() != 1 || moved[0].GetProperty("Id").GetInt32() != folder.Id ||
            moved[0].GetProperty("Path").GetString() != expectedPath)
            throw new InvalidOperationException($"A mozgatás eredménye eltér a várt célútvonaltól. {expectedPath}");

        Console.WriteLine($"Részfa áthelyezve {folder.Path} → {expectedPath}");
    }

    public async Task<IndexState> WaitForCommittedIndexAsync(IEnumerable<int> present, IEnumerable<int> absent)
    {
        var activities = await SendAsync(HttpMethod.Get, $"{IndexRoot}/GetRecentIndexingActivities");
        var lastQueued = activities.GetProperty("State").GetProperty("Serializer").GetProperty("LastQueued").GetInt32();
        var elapsed = Stopwatch.StartNew();
        IndexState index;
        do
        {
            index = await ReadIndexAsync();
            if (index.LastActivityId >= lastQueued && index.Gaps.Length == 0 &&
                present.All(index.VersionIds.Contains) && !absent.Any(index.VersionIds.Contains))
                return index;

            await Task.Delay(TimeSpan.FromSeconds(1));
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(30));

        throw new InvalidOperationException($"Az index 30 másodpercen belül nem érte el a várt állapotot. " +
            $"Utolsó sorba állított aktivitás A{lastQueued}, mentett aktivitás A{index.LastActivityId}.");
    }

    public async Task<IndexState> ReadIndexAsync()
    {
        var index = await SendAsync(HttpMethod.Get, $"{IndexRoot}/GetIndexProperties");
        var checkpoint = index.GetProperty("IndexingActivityStatus");
        return new IndexState(checkpoint.GetProperty("LastActivityId").GetInt32(),
            checkpoint.GetProperty("Gaps").Deserialize<int[]>() ?? [],
            index.GetProperty("VersionIds").EnumerateArray().Select(value => value.GetInt32()).ToHashSet());
    }

    public Task<JsonElement> ReadIndexDocumentAsync(string fixturePath, int versionId) =>
        SendAsync(HttpMethod.Get, $"{Entity(fixturePath)}/GetIndexDocument?versionId={versionId}");

    private async Task<ContentState> CreateAsync(string parent, string name, object body) =>
        ReadContent(await SendAsync(HttpMethod.Post, $"{Entity(parent)}?{Fields}", body), $"{parent}/{name}");

    private static string Entity(string path)
    {
        var split = path.LastIndexOf('/');
        return $"{path[1..split]}('{path[(split + 1)..]}')";
    }

    private static string Entity(int id) => $"content({id})";

    private static ContentState ReadContent(JsonElement data, string path)
    {
        var item = data.GetProperty("d").Deserialize<ContentState>();
        if (item is null || item.Path != path || item.Id <= 0 || item.VersionId <= 0)
            throw new InvalidOperationException($"A repository érvénytelen tartalmi adatot adott vissza. {path}");

        return item;
    }

    private async Task<JsonElement> SendAsync(HttpMethod method, string route, object? body = null)
    {
        using var request = new HttpRequestMessage(method, route);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonSerializerOptions.Default);

        using var response = await http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{method} {route} HTTP {(int)response.StatusCode}. {text.ReplaceLineEndings(" ").Trim()}",
                null, response.StatusCode);

        if (string.IsNullOrWhiteSpace(text))
            return default;
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }
}

sealed record ContentState(int Id, string Path, string Type, int VersionId, string DisplayName, bool Locked);
sealed record IndexState(int LastActivityId, int[] Gaps, HashSet<int> VersionIds);
