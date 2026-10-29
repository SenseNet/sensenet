using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics;

internal static class HealthComponents
{
    internal static readonly string[] Names = { "Database", "BlobStorage", "Search", "Identity" };
}

public enum HealthCheckState { Unknown, Available, Error, TimedOut, Missing, NotApplicable, Disabled, Stale }

/// <summary>A detached, immutable result of one component's background collection.</summary>
public sealed record ComponentHealthSnapshot(
    string ComponentName, HealthCheckState State, HealthColor Color,
    TimeSpan? ResponseTime = null, string Reason = null, string Method = null,
    DateTimeOffset? CheckedAtUtc = null, DateTimeOffset? LastSuccessAtUtc = null,
    DateTimeOffset? ValidUntilUtc = null)
{
    [JsonIgnore] internal long MeasuredAt { get; init; }
    [JsonIgnore] internal long DetailsAt { get; init; }
    [JsonIgnore] internal string DetailsJson { get; init; }
    [JsonIgnore] internal HealthColor DetailsColor { get; init; }
    [JsonIgnore] internal string DetailsError { get; init; }
    public DateTimeOffset? DetailsCheckedAtUtc { get; init; }
    public DateTimeOffset? DetailsValidUntilUtc { get; init; }
}

public interface IComponentHealthCollector
{
    string ComponentName { get; }
    Task<HealthResult> CollectAsync(CancellationToken cancellationToken);
}

public interface IComponentHealthDetailsCollector
{
    Task<HealthDetailsResult> CollectDetailsAsync(CancellationToken cancellationToken);
}

public sealed record HealthDetailsResult(object Data, HealthColor Color = HealthColor.Green, string Reason = null);

public interface IComponentHealthSource
{
    string ComponentName { get; }
    ComponentHealthSnapshot Current { get; }
}

/// <summary>Contains only completed results. Reading it never refreshes data.</summary>
public interface IHealthSnapshotStore
{
    HealthResponseSnapshot Current { get; }
}

public sealed class RepositoryHealthSnapshot
{
    public HealthColor Color { get; }
    public IReadOnlyDictionary<string, ComponentHealthSnapshot> Components { get; }
    public bool Running { get; }
    public string Status { get; }
    public IReadOnlyList<string> StatusHistory { get; }
    internal RepositoryHealthSnapshot(HealthColor color, IEnumerable<ComponentHealthSnapshot> components,
        bool running, string status, IEnumerable<string> history)
    {
        Color = color;
        Components = new ReadOnlyDictionary<string, ComponentHealthSnapshot>(components.ToDictionary(c => c.ComponentName));
        Running = running;
        Status = status;
        StatusHistory = Array.AsReadOnly(history.ToArray());
    }
}

// Separate public DTOs prevent future detailed fields from leaking into anonymous responses.
internal sealed record PublicHealthComponent(HealthColor Color);
internal sealed record PublicHealthColors(HealthColor Color, PublicHealthComponent Database,
    PublicHealthComponent BlobStorage, PublicHealthComponent Search, PublicHealthComponent Identity);
internal sealed record PublicHealthResponse(PublicHealthColors Health);

/// <summary>Both visibility levels and their expiry responses are serialized before publication.</summary>
public sealed class HealthResponseSnapshot
{
    private readonly byte[] _publicBytes;
    private readonly byte[] _detailsBytes;
    private readonly byte[] _expiredPublicBytes;
    private readonly byte[] _expiredDetailsBytes;
    private readonly long _publishedAt;
    private readonly TimeSpan _lifetime;
    public RepositoryHealthSnapshot Repository { get; }
    public DateTimeOffset PublishedAtUtc { get; }
    internal long PublicationSequence { get; init; }

    internal HealthResponseSnapshot(RepositoryHealthSnapshot repository, TimeProvider clock, TimeSpan lifetime)
    {
        Repository = repository;
        PublishedAtUtc = clock.GetUtcNow();
        _publishedAt = clock.GetTimestamp();
        _lifetime = lifetime;
        _publicBytes = Serialize(Public(repository, false));
        _detailsBytes = Serialize(Details(repository, false));
        _expiredPublicBytes = Serialize(Public(repository, true));
        _expiredDetailsBytes = Serialize(Details(repository, true));
    }

    internal bool IsExpired(TimeProvider clock) => clock.GetElapsedTime(_publishedAt) >= _lifetime;

    // Compatibility access returns detached JSON, never provider-owned objects.
    internal object ReadResponse(bool details, TimeProvider clock) =>
        JToken.Parse(Encoding.UTF8.GetString(Bytes(details, IsExpired(clock))));

    internal Task WriteAsync(HttpContext context, bool details, TimeProvider clock)
    {
        var bytes = Bytes(details, IsExpired(clock));
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentLength = bytes.Length;
        return HttpMethods.IsHead(context.Request.Method) ? Task.CompletedTask :
            context.Response.Body.WriteAsync(bytes.AsMemory(), context.RequestAborted).AsTask();
    }

    private byte[] Bytes(bool details, bool expired) => expired
        ? (details ? _expiredDetailsBytes : _expiredPublicBytes) : (details ? _detailsBytes : _publicBytes);

    private static PublicHealthResponse Public(RepositoryHealthSnapshot snapshot, bool expired)
    {
        PublicHealthComponent Component(string name) => new(expired ? HealthColor.Red : snapshot.Components[name].Color);
        return new(new(expired ? HealthColor.Red : snapshot.Color, Component("Database"), Component("BlobStorage"),
            Component("Search"), Component("Identity")));
    }

    private object Details(RepositoryHealthSnapshot snapshot, bool expired)
    {
        var health = new Dictionary<string, object> { ["Color"] = expired ? HealthColor.Red : snapshot.Color };
        var details = new Dictionary<string, object>
        {
            ["HealthServiceStatus"] = expired ? "SnapshotExpired" : "Ready",
            ["Repository_StatusHistory"] = snapshot.StatusHistory,
            ["PublishedAtUtc"] = PublishedAtUtc
        };
        foreach (var component in snapshot.Components.Values)
        {
            health[component.ComponentName] = expired ? component with
            {
                Color = HealthColor.Red, State = HealthCheckState.Stale,
                Reason = "The published snapshot has expired. Current health is unknown."
            } : component;
            var data = component.DetailsJson == null ? new JObject() : JObject.Parse(component.DetailsJson);
            data["CheckedAtUtc"] = component.DetailsCheckedAtUtc;
            data["ValidUntilUtc"] = component.DetailsValidUntilUtc;
            data["Error"] = component.DetailsError;
            details[component.ComponentName] = data;
        }
        return new
        {
            Repository_Status = new { Running = snapshot.Running, Status = snapshot.Status },
            Health = health, Details = details
        };
    }

    internal static byte[] Serialize(object value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value,
        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = { new StringEnumConverter() } }));
}
