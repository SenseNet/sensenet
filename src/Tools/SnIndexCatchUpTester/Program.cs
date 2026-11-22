using System.Net.Http.Headers;
using System.Text.RegularExpressions;

if (args is ["--help"] or ["-h"])
{
    PrintUsage();
    return 0;
}

if (args.Length != 2 || !Uri.TryCreate(args[0], UriKind.Absolute, out var repositoryUrl) ||
    repositoryUrl.Scheme is not ("http" or "https") || string.IsNullOrEmpty(repositoryUrl.Host) ||
    repositoryUrl.UserInfo.Length > 0 || repositoryUrl.Query.Length > 0 || repositoryUrl.Fragment.Length > 0 ||
    !Regex.IsMatch(args[1], @"\A[a-z0-9][a-z0-9-]{0,39}\z"))
{
    PrintUsage();
    return 2;
}

var apiKey = Environment.GetEnvironmentVariable("SN_INDEX_CATCHUP_API_KEY");
var token = Environment.GetEnvironmentVariable("SN_INDEX_CATCHUP_BEARER_TOKEN");
if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Állíts be egyetlen hitelesítési környezeti változót.");
    return 2;
}

using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
{
    BaseAddress = new Uri(repositoryUrl.AbsoluteUri.TrimEnd('/') + "/OData.svc/"),
    Timeout = TimeSpan.FromSeconds(30)
};
if (!string.IsNullOrWhiteSpace(apiKey))
    http.DefaultRequestHeaders.Add("apikey", apiKey);
if (!string.IsNullOrWhiteSpace(token))
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

const string parentPath = "/Root/Content";
const string fixtureMarker = "Index catch-up fixture";
var fixtureName = $"index-catchup-{args[1]}";
var fixturePath = $"{parentPath}/{fixtureName}";
var client = new RepositoryClient(http);
ContentState? fixture = null;

try
{
    Console.WriteLine($"Tesztpéldány {repositoryUrl} | Tesztmappa {fixturePath}");
    await WaitForWebAppAsync(1, "Indítsd el a teszt webappot.");

    Console.WriteLine("[2/11] Tesztadatok előkészítése.");
    await client.EnsurePathIsFreeAsync(fixturePath);
    fixture = await client.CreateFolderAsync(parentPath, fixtureName, fixtureMarker);
    var scenario = await CatchUpScenario.PrepareAsync(client, fixture);
    var baseline = await client.WaitForCommittedIndexAsync(scenario.BaselineVersions, []);
    Console.WriteLine($"Mentés előtti indexállapot A{baseline.LastActivityId}, üres hézaglista.");

    WaitForUser("[3/11] Állítsd le szabályosan a webappot.");
    WaitForUser("[4/11] Másold a teljes indexkönyvtárat külön mentési helyre. A mintahostokban ez az App_Data/LocalIndex mappa.");
    await WaitForWebAppAsync(5, "Indítsd el ugyanazt a webappot a jelenlegi indexszel.");

    Console.WriteLine("[6/11] Online tesztműveletek.");
    var expected = await scenario.ApplyChangesAsync(client);
    var updated = await client.WaitForCommittedIndexAsync(expected.Present.Select(check => check.VersionId),
        expected.Absent.Select(content => content.VersionId));
    if (updated.LastActivityId <= baseline.LastActivityId)
        throw new InvalidOperationException("Az online módosítások után az index aktivitásazonosítója nem lépett előre.");
    Console.WriteLine($"Módosítások utáni indexállapot A{updated.LastActivityId}, üres hézaglista.");

    WaitForUser("[7/11] Állítsd le szabályosan a webappot.");
    WaitForUser("[8/11] Tedd félre a jelenlegi indexkönyvtárat, és másold a 4. lépésben mentett indexkönyvtárat az eredeti helyére.");
    await WaitForWebAppAsync(9, "Indítsd el a webappot a visszamásolt indexszel és a 6. lépés utáni SQL-adatbázissal.");

    Console.WriteLine("[10/11] A visszaállítás utáni index ellenőrzése.");
    var passed = await expected.VerifyAsync(client, fixture.Path, updated.LastActivityId);
    Console.WriteLine(passed ? "A catch-up próba sikeres." : "A catch-up próba hibát talált.");

    Console.WriteLine("[11/11] Takarítás.");
    if (!passed)
    {
        Console.WriteLine("Írj t betűt és nyomj Entert a tesztadatok törléséhez, vagy nyomj Entert a megőrzésükhöz.");
        if (!string.Equals(Console.ReadLine()?.Trim(), "t", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Megőrzött tesztmappa {fixture.Path}");
            return 1;
        }
    }

    await client.VerifyFixtureAsync(fixture);
    await client.DeleteTreeAsync(fixture);
    fixture = null;
    WaitForUser("Rendezd a próba során félretett indexmásolatokat. A futó webapp az eredeti helyén lévő indexet használja.");
    return passed ? 0 : 1;
}
catch (WorkflowCancelledException)
{
    Console.WriteLine("A próba megszakadt.");
    ReportFixture();
    return 130;
}
catch (Exception error)
{
    Console.Error.WriteLine($"HIBA {error.Message}");
    ReportFixture();
    return 1;
}

async Task WaitForWebAppAsync(int step, string instruction)
{
    while (true)
    {
        WaitForUser($"[{step}/11] {instruction}");
        try
        {
            if (fixture is null)
                await client.ReadContentAsync(parentPath);
            else
                await client.VerifyFixtureAsync(fixture);
            return;
        }
        catch (HttpRequestException error) when (error.StatusCode is null or System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Console.Error.WriteLine($"A webapp még nem érhető el. {error.Message}");
        }
        catch (TaskCanceledException)
        {
            Console.Error.WriteLine("A webapp válaszára lejárt a várakozási idő.");
        }
    }
}

void ReportFixture()
{
    if (fixture is not null)
        Console.WriteLine($"A megőrzött tesztadatok helye {fixture.Path}.");
}

static void WaitForUser(string instruction)
{
    Console.WriteLine(instruction);
    while (true)
    {
        Console.WriteLine("Enter a folytatáshoz, q és Enter a megszakításhoz.");
        var input = Console.ReadLine()?.Trim();
        if (input == "")
            return;
        if (input is null || input.Equals("q", StringComparison.OrdinalIgnoreCase))
            throw new WorkflowCancelledException();
    }
}

static void PrintUsage() => Console.WriteLine("""
    Használat: SnIndexCatchUpTester <repository-url> <run-name>
    A futásnév 1–40 kisbetű, szám vagy kötőjel, az első karakter betű vagy szám.
    Tesztmappa: /Root/Content/index-catchup-<run-name>
    Hitelesítés: SN_INDEX_CATCHUP_API_KEY vagy SN_INDEX_CATCHUP_BEARER_TOKEN.
    """);

sealed class WorkflowCancelledException : Exception;
