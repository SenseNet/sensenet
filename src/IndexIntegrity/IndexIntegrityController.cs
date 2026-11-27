#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.OData.Operations.IndexIntegrity;
using SenseNet.Search.Indexing.Integrity;

namespace SenseNet.OData.Operations;

internal sealed class IndexIntegrityController(IndexIntegrityFileStore files, IndexIntegrityWorker worker,
    IIndexIntegrityProvider provider, TimeProvider clock, IAntiforgery antiforgery,
    IOptions<AntiforgeryOptions> antiforgeryOptions) : ODataController
{
    internal const string RouteName = "IndexIntegrity";

    [ODataAction(Category = "Indexing")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.Administrators)]
    public Task<object> StartCheck() => StartRunAsync(null, null);

    [ODataFunction(Category = "Indexing")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.Administrators)]
    public object GetRuns()
    {
        HttpContext.Response.Headers.CacheControl = "no-store";
        return new
        {
            checks = files.ListRuns().Select(run => new
            {
                checkId = run.Check.Id,
                status = run.Check.Status,
                startedAtUtc = run.Check.StartedAtUtc,
                durationMs = run.Check.DurationMs,
                repairs = run.Repairs.Select(repair => new
                {
                    repairId = repair.Id,
                    status = repair.Status,
                    startedAtUtc = repair.StartedAtUtc,
                    durationMs = repair.DurationMs
                }).ToArray()
            }).ToArray()
        };
    }

    [ODataAction(Category = "Indexing")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.Administrators)]
    public async Task<object> Repair(string checkId, object[]? differenceIds = null,
        object? kinds = null)
    {
        if (!Guid.TryParse(checkId, out var check))
            return Reject(StatusCodes.Status400BadRequest, "invalid_check_id");
        if ((differenceIds == null) == (kinds == null))
            return Reject(StatusCodes.Status400BadRequest, "invalid_selection");

        // Bind uncoerced values so a fractional ID cannot be rounded to a different report row.
        if (differenceIds != null)
        {
            if (differenceIds.Length == 0 || differenceIds.Any(id => id is not (long and > 0)))
                return Reject(StatusCodes.Status400BadRequest, "invalid_selection");
            return await StartRunAsync(check, new(differenceIds.Cast<long>().Distinct().Order().ToArray(), null));
        }

        var keyword = kinds switch
        {
            string value => value,
            object[] { Length: 1 } values => values[0] as string,
            _ => null
        };
        if (string.Equals(keyword, nameof(IntegritySelectionRequest.All), StringComparison.OrdinalIgnoreCase))
            return await StartRunAsync(check, IntegritySelectionRequest.All);

        if (kinds is not object[] { Length: > 0 } kindValues)
            return Reject(StatusCodes.Status400BadRequest, "invalid_selection");

        var selectedKinds = new HashSet<DifferenceKind>();
        foreach (var value in kindValues)
        {
            if (value is not string name || !Enum.TryParse<DifferenceKind>(name, true, out var kind) ||
                !string.Equals(name, kind.ToString(), StringComparison.OrdinalIgnoreCase) || !Enum.IsDefined(kind))
                return Reject(StatusCodes.Status400BadRequest, "invalid_selection");
            selectedKinds.Add(kind);
        }
        return await StartRunAsync(check, new(null, selectedKinds.Order().ToArray()));
    }

    [ODataFunction(Category = "Indexing")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.Administrators)]
    public async Task<object?> GetReport(string checkId, string? view = null,
        string? repairId = null)
    {
        if (!Guid.TryParse(checkId, out var check))
            return Reject(StatusCodes.Status400BadRequest, "invalid_check_id");

        var id = check;
        if (repairId != null && (!Guid.TryParse(repairId, out id) || id == check || view != "status"))
            return Reject(StatusCodes.Status400BadRequest, "invalid_repair_id");

        var run = files.Read(check, id);
        if (run == null)
            return Reject(StatusCodes.Status404NotFound, "run_not_found");

        var response = HttpContext.Response;
        response.Headers.CacheControl = "no-store";
        if (view == "status")
            return State(run);
        if (run.Active)
        {
            response.StatusCode = StatusCodes.Status202Accepted;
            response.Headers.RetryAfter = "3";
            return State(run);
        }
        if (run.Status != "completed")
            return Reject(StatusCodes.Status409Conflict, "report_not_ready");

        using var lease = files.OpenReport(check);
        if (lease == null)
            return Reject(StatusCodes.Status404NotFound, "report_not_found");

        response.ContentType = "application/x-ndjson";
        var reportLength = lease.Stream.Length;
        response.ContentLength = reportLength;
        SetResponseSize(reportLength);
        response.Headers.ContentDisposition = $"attachment; filename=\"comparison-{check:D}.jsonl\"";
        await response.StartAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        await lease.Stream.CopyToAsync(response.Body, HttpContext.RequestAborted).ConfigureAwait(false);
        return null;
    }

    private async Task<object> StartRunAsync(Guid? checkId, IntegritySelectionRequest? selection)
    {
        if (UsesCookieAuthentication())
        {
            // OData has consumed the request body by this point. Read the antiforgery token from a header.
            var header = antiforgeryOptions.Value.HeaderName;
            if (header == null || string.IsNullOrEmpty(HttpContext.Request.Headers[header]) ||
                !await antiforgery.IsRequestValidAsync(HttpContext).ConfigureAwait(false))
                return Reject(StatusCodes.Status403Forbidden, "invalid_antiforgery_token");
        }

        var capability = provider.GetCapabilities();
        if (checkId.HasValue ? !capability.CanRepair : !capability.CanCheck)
            return Reject(StatusCodes.Status501NotImplemented, "unsupported_operation");

        var key = HttpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (key is { Length: > 200 } || key != null && string.IsNullOrWhiteSpace(key))
            return Reject(StatusCodes.Status400BadRequest, "invalid_idempotency_key");

        var id = Guid.NewGuid();
        var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new { checkId, selection }, IndexIntegrityFileStore.Json)));
        var reserved = files.Reserve(new(checkId ?? id, id, capability.SourceId, provider.ProviderId,
            provider.RepairFormatVersion, User.LoggedInUser.Id, key, fingerprint, clock.GetUtcNow()));
        if (reserved.Run == null)
            return Reject(reserved.Error == "check_not_found" ? StatusCodes.Status404NotFound :
                StatusCodes.Status409Conflict, reserved.Error!);

        var run = reserved.Run;
        var statusPath = ODataTools.GetODataOperationUrl(Content.Path, $"{RouteName}/{nameof(GetReport)}");
        var statusQuery = $"?checkId={run.CheckId:D}&view=status";
        if (run.IsRepair)
            statusQuery += $"&repairId={run.Id:D}";

        var response = HttpContext.Response;
        response.Headers.CacheControl = "no-store";
        response.Headers.Location = $"{HttpContext.Request.PathBase}{statusPath}{statusQuery}";
        response.StatusCode = run.Active ? StatusCodes.Status202Accepted : StatusCodes.Status200OK;
        var state = State(run);
        if (reserved.Created)
            worker.Enqueue(run, selection);
        return state;
    }

    private bool UsesCookieAuthentication()
    {
        var request = HttpContext.Request;
        var authorization = request.Headers.Authorization.ToString();
        // The sensenet JWT cookie reader copies the cookie verbatim to the Authorization header.
        return request.Cookies.TryGetValue(".sn.auth", out var jwtCookie) && jwtCookie == authorization ||
            HttpContext.User.Identity?.IsAuthenticated == true &&
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }

    private static object State(IntegrityRun run) => new
    {
        checkId = run.CheckId,
        repairId = run.IsRepair ? run.Id : (Guid?)null,
        status = run.Status,
        queuedAtUtc = run.QueuedAtUtc,
        startedAtUtc = run.StartedAtUtc,
        finishedAtUtc = run.FinishedAtUtc,
        durationMs = run.DurationMs,
        error = run.Error,
        summary = run.Summary is { } summary ? JToken.Parse(summary.GetRawText()) : null
    };

    private object Reject(int status, string error)
    {
        HttpContext.Response.StatusCode = status;
        HttpContext.Response.Headers.CacheControl = "no-store";
        return new { error };
    }
}
