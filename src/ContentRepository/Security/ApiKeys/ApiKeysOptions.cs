using SenseNet.Tools.Configuration;

namespace SenseNet.ContentRepository.Security.ApiKeys;

/// <summary>
/// Provides predefined API keys.
/// </summary>
[OptionsClass(sectionName: "sensenet:ApiKeys")]
public class ApiKeysOptions
{
    /// <summary>
    /// Gets or sets the API key for the HealthChecker user.
    /// This API key is required for using the HealthMiddleware.
    /// </summary>
    public string HealthCheckerUser { get; set; }
}