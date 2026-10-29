using System;

namespace SenseNet.Services.Core.Diagnostics;

/// <summary>Background health collection settings. Changes take effect after application restart.</summary>
public sealed class HealthMonitoringOptions
{
    public ComponentHealthOptions Database { get; set; } = new();
    public ComponentHealthOptions BlobStorage { get; set; } = new();
    public ComponentHealthOptions Search { get; set; } = new();
    public ComponentHealthOptions Identity { get; set; } = new()
    {
        Interval = TimeSpan.FromSeconds(30), Timeout = TimeSpan.FromSeconds(4), MaxAge = TimeSpan.FromSeconds(90)
    };
    public TimeSpan PublishInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan PublisherMaxAge { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan StartupGracePeriod { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan InitialJitter { get; set; } = TimeSpan.FromSeconds(1);
    public double JitterRatio { get; set; } = 0.1;
    public TimeSpan DetailsInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan DetailsMaxAge { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan DetailsTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Exact origins allowed on the early health branch. Empty means no cross-origin access.</summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    internal ComponentHealthOptions For(string name) => name switch
    {
        "Database" => Database, "BlobStorage" => BlobStorage, "Search" => Search, "Identity" => Identity,
        _ => throw new ArgumentException("Unknown health component: " + name)
    };

    internal void Validate()
    {
        static void Positive(TimeSpan value, string name)
        {
            if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
                throw new ArgumentException("HealthMonitoring: invalid duration: " + name);
        }
        Positive(PublishInterval, nameof(PublishInterval));
        Positive(PublisherMaxAge, nameof(PublisherMaxAge));
        Positive(StartupGracePeriod, nameof(StartupGracePeriod));
        Positive(DetailsInterval, nameof(DetailsInterval));
        Positive(DetailsMaxAge, nameof(DetailsMaxAge));
        Positive(DetailsTimeout, nameof(DetailsTimeout));
        Positive(ShutdownTimeout, nameof(ShutdownTimeout));
        if (InitialJitter < TimeSpan.Zero || InitialJitter.TotalMilliseconds > int.MaxValue ||
            double.IsNaN(JitterRatio) || JitterRatio < 0 || JitterRatio > 0.5 || PublisherMaxAge <= PublishInterval)
            throw new ArgumentException("HealthMonitoring: invalid jitter or publisher lifetime.");
        foreach (var name in HealthComponents.Names)
        {
            var component = For(name) ?? throw new ArgumentException("Missing health options: " + name);
            Positive(component.Interval, name + ".Interval");
            Positive(component.Timeout, name + ".Timeout");
            Positive(component.MaxAge, name + ".MaxAge");
            var cycle = component.Interval * (1 + JitterRatio) + component.Timeout + DetailsTimeout + PublishInterval;
            if (component.MaxAge <= cycle || DetailsMaxAge <= DetailsInterval + cycle)
                throw new ArgumentException("HealthMonitoring: MaxAge must allow a complete collection cycle: " + name);
        }
    }
}

/// <summary>One component's collection cadence, independent of HTTP traffic.</summary>
public sealed class ComponentHealthOptions
{
    public bool Enabled { get; set; } = true;
    public bool Required { get; set; } = true;
    /// <summary>Delay after the preceding collection, with bounded jitter and no failure backoff.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromSeconds(30);
}
