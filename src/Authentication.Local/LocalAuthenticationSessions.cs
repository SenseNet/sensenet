using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Authentication.Local;

public sealed record LocalTokenResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");

internal sealed class LocalAuthenticationSessions(
    IAccessTokenDataProvider storage, ILocalAuthenticationUsers users, LocalAuthenticationOptions options)
{
    private const string Feature = "local-auth-session";

    public async Task<LocalTokenResponse> CreateAsync(int userId, CancellationToken cancel)
    {
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        var now = DateTime.UtcNow;
        var session = new AccessToken
        {
            UserId = userId, Value = LocalAuthenticationPolicy.Hash(refreshToken), Feature = Feature,
            CreationDate = now, ExpirationDate = now.Add(options.SessionLifetime)
        };
        await storage.SaveAccessTokenAsync(session, cancel).ConfigureAwait(false);
        return Issue(session, refreshToken);
    }

    public async Task<LocalTokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancel)
    {
        var session = await LoadAsync(LocalAuthenticationPolicy.Hash(refreshToken), cancel).ConfigureAwait(false);
        return session == null ? null : Issue(session, refreshToken);
    }

    public async Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancel)
    {
        var sid = principal.FindFirst("sid")?.Value;
        if (sid == null || !int.TryParse(principal.FindFirst("sub")?.Value, out var userId))
            return false;
        var session = await LoadAsync(sid, cancel).ConfigureAwait(false);
        return session?.UserId == userId;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancel)
    {
        // A hash is persisted, never the usable refresh token. Idempotent deletion
        // does not disclose whether a session existed.
        await storage.DeleteAccessTokenAsync(LocalAuthenticationPolicy.Hash(refreshToken), cancel).ConfigureAwait(false);
    }

    private async Task<AccessToken?> LoadAsync(string hash, CancellationToken cancel)
    {
        var session = await storage.LoadAccessTokenAsync(hash, 0, Feature, cancel).ConfigureAwait(false);
        return session is { UserId: > 0 } && session.ExpirationDate > DateTime.UtcNow &&
            await users.IsAllowedAsync(session.UserId, cancel).ConfigureAwait(false) ? session : null;
    }

    private LocalTokenResponse Issue(AccessToken session, string refreshToken)
    {
        var now = DateTime.UtcNow;
        var expiration = new[] { now.Add(options.AccessTokenLifetime), session.ExpirationDate }.Min();
        var jwt = new JwtSecurityToken(options.Issuer, options.Audience,
            new[] { new Claim("sub", session.UserId.ToString(CultureInfo.InvariantCulture)),
                new Claim("sid", session.Value), new Claim("jti", Guid.NewGuid().ToString("N")) },
            now, expiration, new SigningCredentials(options.SigningKey, SecurityAlgorithms.RsaSha256));
        return new LocalTokenResponse(new JwtSecurityTokenHandler().WriteToken(jwt), refreshToken,
            Math.Max(0, (int)(expiration - now).TotalSeconds));
    }
}
