#nullable enable
using Newtonsoft.Json.Converters;
using System;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics;

public enum HealthColor
{
    /// <summary>Means: the component works well.</summary>
    Green,
    /// <summary>Means: the component has a problem.</summary>
    Yellow,
    /// <summary>Means: the component is working incorrectly or not at all.</summary>
    Red
};

public interface IHealthResult
{
    /// <summary>
    /// Gets or sets the working status.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    HealthColor Color { get; set; }
    /// <summary>
    /// Gets or sets the measuring time if there is.
    /// </summary>
    TimeSpan? ResponseTime { get; set; }
    /// <summary>
    /// Gets or sets the cause of the malfunction or problem if the component is not working properly.
    /// </summary>
    string? Reason { get; set; }
    /// <summary>
    /// Gets or sets the brief description of the health measurement.
    /// </summary>
    string? Method { get; set; }
}

public class HealthResult : IHealthResult
{
    public HealthColor Color { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? Reason { get; set; }
    public string? Method { get; set; }
}