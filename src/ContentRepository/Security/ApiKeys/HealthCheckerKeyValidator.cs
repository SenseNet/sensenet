using System;
using Microsoft.Extensions.Options;

namespace SenseNet.ContentRepository.Security.ApiKeys;

/// <summary>Validates the configured health key without accessing repository tokens or users.</summary>
public sealed class HealthCheckerKeyValidator
{
    private readonly string _key;
    private readonly DateTimeOffset _expires;
    private readonly TimeProvider _clock;
    public HealthCheckerKeyValidator(IOptions<ApiKeysOptions> options, TimeProvider clock = null)
    {
        _clock = clock ?? TimeProvider.System;
        _key = options?.Value.HealthCheckerUser;
        // Preserve the existing configured health token's one-year lifetime.
        _expires = _clock.GetUtcNow().AddYears(1);
    }
    public bool Matches(string key) => !string.IsNullOrEmpty(_key) && !string.IsNullOrEmpty(key) &&
        string.Equals(_key, key, StringComparison.Ordinal);
    public bool IsValid(string key) => Matches(key) && _clock.GetUtcNow() < _expires;
}
