using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.Authentication.Local;

/// <summary>Configuration-based registration for repository web applications.</summary>
public static class LocalAuthenticationConfiguration
{
    public const string SectionName = "sensenet:Authentication:Local";

    public static IServiceCollection AddSenseNetLocalAuthentication(this IServiceCollection services,
        IConfiguration configuration) => services.AddSenseNetLocalAuthentication(options =>
    {
        var section = configuration.GetSection(SectionName);
        section.Bind(options);
        if (options.Mode == LocalAuthenticationMode.Disabled)
            return;
        options.ExternalAuthority ??= configuration["sensenet:Authentication:Authority"];
        options.SigningKey = LoadKey(section["SigningKeyPath"], section["SigningKeyId"], true);
        foreach (var key in section.GetSection("PreviousKeys").GetChildren())
            options.PreviousSigningKeys.Add(LoadKey(key["Path"], key["KeyId"], false));
    });

    private static RsaSecurityKey LoadKey(string? path, string? keyId, bool privateKey)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Local authentication requires an absolute PEM key path and a key id.");
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa.ExportParameters(privateKey)) { KeyId = keyId };
    }
}
