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
    Console.Error.WriteLine("Set only one authentication environment variable.");
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
    Console.WriteLine($"Test repository {repositoryUrl} | Test folder {fixturePath}");
    await WaitForWebAppAsync(1, "Start the test web app.");

    WriteColoredLine("[2/11] Preparing test data.", ConsoleColor.Yellow);
    await client.EnsurePathIsFreeAsync(fixturePath);
    fixture = await client.CreateFolderAsync(parentPath, fixtureName, fixtureMarker);
    var scenario = await CatchUpScenario.PrepareAsync(client, fixture);
    var baseline = await client.WaitForCommittedIndexAsync(scenario.BaselineVersions, []);
    Console.WriteLine($"Index checkpoint before backup A{baseline.LastActivityId}, no gaps.");

    WaitForUser("[3/11] Shut down the web app gracefully.");
    WaitForUser("[4/11] Back up the entire index directory to a separate location. The sample hosts use App_Data/LocalIndex.");
    await WaitForWebAppAsync(5, "Start the same web app with the current index.");

    WriteColoredLine("[6/11] Applying test changes while the web app is running.", ConsoleColor.Yellow);
    var expected = await scenario.ApplyChangesAsync(client);
    var updated = await client.WaitForCommittedIndexAsync(expected.Present.Select(check => check.VersionId),
        expected.Absent.Select(content => content.VersionId));
    if (updated.LastActivityId <= baseline.LastActivityId)
        throw new InvalidOperationException("The index checkpoint did not advance after the online changes.");
    Console.WriteLine($"Index checkpoint after changes A{updated.LastActivityId}, no gaps.");

    WaitForUser("[7/11] Shut down the web app gracefully.");
    WaitForUser("[8/11] Set aside the current index directory and restore the backup from step 4 to its original location.");
    await WaitForWebAppAsync(9, "Start the web app with the restored index and the SQL database from the end of step 6.");

    WriteColoredLine("[10/11] Verifying the index after restore.", ConsoleColor.Yellow);
    var passed = await expected.VerifyAsync(client, fixture.Path, updated.LastActivityId);
    Console.WriteLine(passed ? "The catch-up test passed." : "The catch-up test failed.");

    WriteColoredLine("[11/11] Cleaning up.", ConsoleColor.Yellow);
    if (!passed)
    {
        WriteColoredLine("Type t and press Enter to delete the test data, or press Enter to keep it.", ConsoleColor.Cyan);
        if (!string.Equals(Console.ReadLine()?.Trim(), "t", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Kept test folder {fixture.Path}");
            return 1;
        }
    }

    await client.VerifyFixtureAsync(fixture);
    await client.DeleteTreeAsync(fixture);
    fixture = null;
    WaitForUser("Tidy up the index copies set aside during the test. The running web app uses the index in its original location.");
    return passed ? 0 : 1;
}
catch (WorkflowCancelledException)
{
    Console.WriteLine("The test was canceled.");
    ReportFixture();
    return 130;
}
catch (Exception error)
{
    Console.Error.WriteLine($"ERROR {error.Message}");
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
            Console.Error.WriteLine($"The web app is not available yet. {error.Message}");
        }
        catch (TaskCanceledException)
        {
            Console.Error.WriteLine("Timed out waiting for the web app to respond.");
        }
    }
}

void ReportFixture()
{
    if (fixture is not null)
        Console.WriteLine($"Test data kept at {fixture.Path}.");
}

static void WaitForUser(string instruction)
{
    WriteColoredLine(instruction, ConsoleColor.Cyan);
    while (true)
    {
        Console.WriteLine("Press <enter> to continue, or type <q> and press <enter> to cancel.");
        var input = Console.ReadLine()?.Trim();
        if (input == "")
            return;
        if (input is null || input.Equals("q", StringComparison.OrdinalIgnoreCase))
            throw new WorkflowCancelledException();
    }
}

static void WriteColoredLine(string message, ConsoleColor color)
{
    if (Console.IsOutputRedirected)
    {
        Console.WriteLine(message);
        return;
    }

    var previousColor = Console.ForegroundColor;
    try
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
    }
    finally
    {
        Console.ForegroundColor = previousColor;
    }
}

static void PrintUsage() => Console.WriteLine("""
    Usage: SnIndexCatchUpTester <repository-url> <run-name>
    Use 1 to 40 lowercase letters, digits, or hyphens for the run name. Start with a letter or digit.
    Test folder: /Root/Content/index-catchup-<run-name>
    Authentication: SN_INDEX_CATCHUP_API_KEY or SN_INDEX_CATCHUP_BEARER_TOKEN.
    """);

sealed class WorkflowCancelledException : Exception;
