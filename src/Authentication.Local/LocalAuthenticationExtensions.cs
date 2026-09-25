using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace SenseNet.Authentication.Local;

public static class LocalAuthenticationExtensions
{
    /// <summary>Register after external authentication. Disabled leaves existing schemes unchanged.</summary>
    public static IServiceCollection AddSenseNetLocalAuthentication(this IServiceCollection services,
        Action<LocalAuthenticationOptions> configure)
    {
        var options = new LocalAuthenticationOptions();
        configure(options);
        services.AddSingleton(options);
        if (options.Mode == LocalAuthenticationMode.Disabled)
            return services;
        Validate(options);
        services.AddSingleton<LocalAuthenticationPolicy>();
        services.TryAddSingleton<ILocalAuthenticationUsers, RepositoryLocalUsers>();
        services.AddTransient<LocalAuthenticationSessions>();
        services.AddTransient<LocalAuthenticationFlows>();
        services.TryAddTransient<ILocalPasswordResetSender, LocalPasswordResetSender>();
        services.AddAuthentication(authentication =>
            {
                authentication.DefaultScheme = LocalAuthenticationOptions.PolicyScheme;
                authentication.DefaultAuthenticateScheme = LocalAuthenticationOptions.PolicyScheme;
                authentication.DefaultChallengeScheme = LocalAuthenticationOptions.PolicyScheme;
            })
            .AddPolicyScheme(LocalAuthenticationOptions.PolicyScheme, null, policy =>
            {
                policy.ForwardDefaultSelector = context =>
                {
                    if (options.Mode == LocalAuthenticationMode.InternalOnly)
                        return LocalAuthenticationOptions.Scheme;
                    var header = context.Request.Headers.Authorization.ToString();
                    if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = header[7..];
                        var handler = new JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token))
                        {
                            try
                            {
                                // Routing only. The selected scheme always performs full validation.
                                if (handler.ReadJwtToken(token).Issuer == options.Issuer)
                                    return LocalAuthenticationOptions.Scheme;
                            }
                            catch (ArgumentException) { }
                        }
                    }
                    return options.ExternalScheme;
                };
            })
            .AddJwtBearer(LocalAuthenticationOptions.Scheme, jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.IncludeErrorDetails = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = options.Issuer,
                    ValidateAudience = true, ValidAudience = options.Audience,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    RequireSignedTokens = true, ValidateIssuerSigningKey = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                    IssuerSigningKeys = options.PreviousSigningKeys.Prepend(options.SigningKey!),
                    ClockSkew = TimeSpan.Zero
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var policy = context.HttpContext.RequestServices.GetRequiredService<LocalAuthenticationPolicy>();
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<LocalAuthenticationSessions>();
                        if (!policy.Allows(context.HttpContext, token: true) ||
                            !await sessions.ValidateAsync(context.Principal!, context.HttpContext.RequestAborted).ConfigureAwait(false))
                            context.Fail("Invalid local session.");
                    }
                };
            });
        return services;
    }

    /// <summary>Call after trusted forwarded headers and CORS, before repository authentication/OData.</summary>
    public static IApplicationBuilder UseSenseNetLocalAuthentication(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<LocalAuthenticationOptions>();
        if (options.Mode != LocalAuthenticationMode.Disabled)
        {
            if (app.ApplicationServices.GetService<ILocalAuthenticationUserLock>() == null)
                throw new InvalidOperationException("Register a repository-wide ILocalAuthenticationUserLock before enabling local authentication.");
            var repositoryAuth = app.ApplicationServices.GetService<Microsoft.Extensions.Options.IOptions<
                Services.Core.Authentication.AuthenticationOptions>>()?.Value;
            if (repositoryAuth?.AddJwtCookie == true)
                throw new InvalidOperationException("Local authentication requires bearer transport. Disable AddJwtCookie.");
            LocalAuthenticationForwarding.UseTrustedForwarding(app, options);
            app.UseMiddleware<LocalAuthenticationMiddleware>();
        }
        return app;
    }

    private static void Validate(LocalAuthenticationOptions options)
    {
        if (!Enum.IsDefined(options.Mode) || !Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) ||
            issuer.Scheme != "https" || string.IsNullOrWhiteSpace(options.Audience) ||
            options.SigningKey == null || options.SigningKey.KeySize < 2048 ||
            string.IsNullOrWhiteSpace(options.SigningKey.KeyId) ||
            options.AccessTokenLifetime <= TimeSpan.Zero || options.AccessTokenLifetime > TimeSpan.FromMinutes(15) ||
            options.SessionLifetime < options.AccessTokenLifetime || options.SessionLifetime > TimeSpan.FromDays(1) ||
            options.LoginNetworks.Length == 0 ||
            options.AllowedUserIds.Length + options.AllowedGroupIds.Length == 0 ||
            options.AllowedUserIds.Concat(options.AllowedGroupIds).Any(id => id <= 0) ||
            options.AttemptsPerMinutePerIp <= 0 || options.AttemptsPerMinutePerAccount <= 0 || options.ForwardLimit is < 1 or > 10)
            throw new ArgumentException("Local authentication requires an HTTPS issuer, RSA key with kid, explicit networks/users/groups and bounded lifetimes/limits.");
        var appearance = options.Appearance;
        foreach (var color in new[] { appearance.BackgroundColor, appearance.BrandColor, appearance.ButtonColor,
                     appearance.ButtonTextColor, appearance.TextColor, appearance.PanelColor })
            if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$"))
                throw new ArgumentException("Login colors must be six- or eight-digit hexadecimal colors.");
        if (appearance.Title.Length is 0 or > 120)
            throw new ArgumentException("Login title must contain 1-120 characters.");
        foreach (var image in new[] { appearance.BackgroundImageUrl, appearance.LogoUrl })
            if (!string.IsNullOrEmpty(image) && (!Uri.TryCreate(image, UriKind.Absolute, out var imageUri) ||
                imageUri.Scheme != "https" || !string.IsNullOrEmpty(imageUri.UserInfo)))
                throw new ArgumentException("Branding images must use absolute HTTPS URLs without credentials.");
        var recovery = options.PasswordRecovery;
        if (recovery.Enabled && (!Uri.TryCreate(recovery.ResetUrl, UriKind.Absolute, out var resetUri) ||
            (resetUri.Scheme != "https" && !(resetUri.Scheme == "http" && resetUri.IsLoopback)) ||
            !string.IsNullOrEmpty(resetUri.UserInfo) || !string.IsNullOrEmpty(resetUri.Fragment) ||
            recovery.TokenLifetime <= TimeSpan.Zero || recovery.TokenLifetime > TimeSpan.FromHours(1) ||
            recovery.MinimumPasswordLength is < 12 or > 128))
            throw new ArgumentException("Recovery requires a trusted HTTPS UI URL (HTTP loopback is allowed), bounded lifetime and password length.");
        var keys = options.PreviousSigningKeys.Prepend(options.SigningKey).ToArray();
        if (keys.Any(k => k.KeySize < 2048 || string.IsNullOrWhiteSpace(k.KeyId)) ||
            keys.Select(k => k.KeyId).Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new ArgumentException("Local signing keys must have distinct kid values and at least 2048 bits.");
        if (options.Mode == LocalAuthenticationMode.Secondary &&
            (string.IsNullOrWhiteSpace(options.ExternalScheme) ||
             options.ExternalScheme is LocalAuthenticationOptions.Scheme or LocalAuthenticationOptions.PolicyScheme))
            throw new ArgumentException("Secondary mode requires a distinct external authentication scheme.");
        foreach (var proxy in options.KnownProxies)
            _ = System.Net.IPAddress.Parse(proxy);
        foreach (var network in options.KnownNetworks)
            _ = System.Net.IPNetwork.Parse(network);
        var signer = options.SigningKey.CryptoProviderFactory.CreateForSigning(options.SigningKey, SecurityAlgorithms.RsaSha256);
        try { _ = signer.Sign(new byte[] { 0 }); }
        finally { options.SigningKey.CryptoProviderFactory.ReleaseSignatureProvider(signer); }
        _ = new LocalAuthenticationPolicy(options); // Validate CIDRs at registration, not on first login.
    }
}
