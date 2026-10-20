using Microsoft.IdentityModel.Tokens;

namespace SenseNet.Authentication.Local;

public enum LocalAuthenticationMode { Disabled, Secondary, InternalOnly }

/// <summary>Local authentication is disabled unless explicitly configured by the host.</summary>
public sealed class LocalAuthenticationOptions
{
    public const string Scheme = "SenseNet.Local";
    public const string PolicyScheme = "SenseNet.Composite";
    public const string EndpointPrefix = "/authentication/local";
    public LocalAuthenticationMode Mode { get; set; }
    public LocalLoginAppearance Appearance { get; set; } = new();
    public LocalPasswordRecoveryOptions PasswordRecovery { get; set; } = new();
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "sensenet";
    public string ExternalScheme { get; set; } = "Bearer";
    public string? ExternalAuthority { get; set; }
    /// <summary>Supply an RSA private key from a secret store. Never store it in appsettings.</summary>
    public RsaSecurityKey? SigningKey { get; set; }
    /// <summary>Previous public keys retained during rotation. Every key must have a unique kid.</summary>
    public List<RsaSecurityKey> PreviousSigningKeys { get; set; } = new();
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(8);
    public string[] LoginNetworks { get; set; } = Array.Empty<string>();
    /// <summary>Null uses LoginNetworks. An empty array denies all local token usage.</summary>
    public string[]? TokenNetworks { get; set; }
    public string[] KnownProxies { get; set; } = Array.Empty<string>();
    public string[] KnownNetworks { get; set; } = Array.Empty<string>();
    public int ForwardLimit { get; set; } = 1;
    public int[] AllowedUserIds { get; set; } = Array.Empty<int>();
    public int[] AllowedGroupIds { get; set; } = Array.Empty<int>();
    public bool RequireMultiFactor { get; set; } = true;
    public int AttemptsPerMinutePerIp { get; set; } = 30;
    public int AttemptsPerMinutePerAccount { get; set; } = 5;
}
