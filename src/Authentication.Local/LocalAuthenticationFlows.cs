using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Email;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Operations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local;

public sealed record LocalMfaChallenge(string ChallengeToken, int ExpiresIn,
    string? ManualEntryKey, string? QrCodeSetupImageUrl);

/// <summary>Interactive login and recovery; all usable tokens remain outside persistent storage.</summary>
internal sealed class LocalAuthenticationFlows(LocalAuthenticationOptions options, LocalAuthenticationPolicy policy,
    IAccessTokenDataProvider storage, ILocalAuthenticationUsers users, LocalAuthenticationSessions sessions,
    ILogger<LocalAuthenticationFlows> logger, ILocalAuthenticationUserLock locks)
{
    private const string MfaFeature = "local-auth-mfa";
    private const string ResetFeature = "local-auth-reset";
    private const string SessionFeature = "local-auth-session";
    private static readonly TimeSpan MfaLifetime = TimeSpan.FromMinutes(5);

    private bool Listed(User? user) => user is { Enabled: true } && user.Id > 0 && user.Id != User.Visitor.Id &&
        (options.AllowedUserIds.Contains(user.Id) || options.AllowedGroupIds.Any(user.IsInGroup));
    private bool RequiresMfa(User user) => options.RequireMultiFactor ||
        (options.Mode == LocalAuthenticationMode.Secondary && (user.Id == 1 || user.IsInGroup(Group.Administrators)));

    public async Task<object?> LoginAsync(HttpContext context, string username, string password,
        string? code, bool challenge)
    {
        try
        {
            using var account = new SystemAccount();
            var candidate = User.Load(username);
            if (candidate == null) return null;
            return await InUserLock<object>(candidate.Id, context, async cancel =>
            {
                if (!challenge || !string.IsNullOrEmpty(code))
                {
                    var id = await users.ValidateAsync(context, username, password, code).ConfigureAwait(false);
                    return id.HasValue ? await sessions.CreateAsync(id.Value, cancel).ConfigureAwait(false) : null;
                }
                if (!policy.TryAttempt(context, "id:" + candidate.Id)) return null;
                var credentials = IdentityOperations.ValidateCredentials(null!, context, username, password);
                var user = await Node.LoadAsync<User>(credentials.Id, cancel).ConfigureAwait(false);
                if (!Listed(user) || (RequiresMfa(user) && !user.EffectiveMultiFactorEnabled)) return null;
                if (!user.EffectiveMultiFactorEnabled)
                    return await sessions.CreateAsync(user.Id, cancel).ConfigureAwait(false);
                // Emergency Secondary-mode access still requires prior MFA enrollment.
                if (options.Mode == LocalAuthenticationMode.Secondary && RequiresMfa(user) && !user.MultiFactorRegistered)
                    return null;
                var setup = user.MultiFactorRegistered ? (Url: (string?)null, EntryKey: (string?)null) :
                    Providers.Instance.MultiFactorAuthenticationProvider.GenerateSetupCode(
                        string.IsNullOrWhiteSpace(user.Email) ? user.LoginName : user.Email, user.TwoFactorKey);
                await DeleteFeatureTokens(user.Id, cancel, MfaFeature).ConfigureAwait(false);
                var token = await Issue(user.Id, MfaFeature, MfaLifetime, cancel).ConfigureAwait(false);
                return new LocalMfaChallenge(token, (int)MfaLifetime.TotalSeconds,
                    setup.EntryKey, setup.Url);
            }).ConfigureAwait(false);
        }
        catch (SenseNetSecurityException) { return null; }
        catch (MissingDomainException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public async Task<LocalTokenResponse?> CompleteMfaAsync(HttpContext context, string token, string code)
    {
        var record = await Load(token, MfaFeature, context.RequestAborted).ConfigureAwait(false);
        if (record == null) return null;
        return await InUserLock<LocalTokenResponse>(record.UserId, context, async cancel =>
        {
            record = await Load(token, MfaFeature, cancel).ConfigureAwait(false);
            if (record == null || !policy.TryAttempt(context, "id:" + record.UserId)) return null;
            using var account = new SystemAccount();
            var user = await Node.LoadAsync<User>(record.UserId, cancel).ConfigureAwait(false);
            if (!Listed(user) || !user.EffectiveMultiFactorEnabled) return null;
            try
            {
                await IdentityOperations.ValidateTwoFactorCode(Content.Create(user), context, code).ConfigureAwait(false);
            }
            catch (SenseNetSecurityException) { return null; }
            catch (InvalidOperationException) { return null; }
            if (!await users.IsAllowedAsync(user.Id, cancel).ConfigureAwait(false)) return null;
            await storage.DeleteAccessTokenAsync(record.Value, cancel).ConfigureAwait(false);
            return await sessions.CreateAsync(user.Id, cancel).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task ForgotPasswordAsync(HttpContext context, string email)
    {
        if (!options.PasswordRecovery.Enabled || !MailAddress.TryCreate(email, out var parsed) ||
            !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase)) return;
        string? token = null;
        try
        {
            using var account = new SystemAccount();
            // Ambiguous email addresses are not sufficient proof of an account identity.
            var matches = Content.All.Where(c => c.TypeIs("User") && (string)c["Email"] == email).Take(2).ToArray();
            if (matches.Length != 1 || matches[0].ContentHandler is not User user || !Listed(user)) return;
            token = await InUserLock<string>(user.Id, context, async cancel =>
            {
                await DeleteFeatureTokens(user.Id, cancel, ResetFeature).ConfigureAwait(false);
                return await Issue(user.Id, ResetFeature, options.PasswordRecovery.TokenLifetime, cancel).ConfigureAwait(false);
            }).ConfigureAwait(false);
            if (token == null) return;
            var url = new UriBuilder(options.PasswordRecovery.ResetUrl);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(url.Query);
            query["repoUrl"] = options.Issuer;
            url.Query = string.Join("&", query.SelectMany(pair => pair.Value.Select(value =>
                Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(value ?? ""))));
            // The token is a fragment: never sent in a page request or Referer header.
            url.Fragment = "localResetToken=" + token;
            var link = WebUtility.HtmlEncode(url.Uri.AbsoluteUri);
            await context.RequestServices.GetRequiredService<ILocalPasswordResetSender>().SendAsync(new EmailData
            {
                ToAddress = email,
                ToName = user.DisplayName,
                Subject = "Reset your sensenet password",
                Body = "<p>A password reset was requested for your repository account.</p>" +
                    "<p><a href=\"" + link + "\">Choose a new password</a></p>" +
                    "<p>This link expires in " + (int)options.PasswordRecovery.TokenLifetime.TotalMinutes +
                    " minutes and can be used once. If you did not request this, ignore this email.</p>"
            }, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            if (token != null)
                await storage.DeleteAccessTokenAsync(LocalAuthenticationPolicy.Hash(token), CancellationToken.None).ConfigureAwait(false);
            // Never log an email address, password, reset link or token.
            logger.LogWarning("Local password recovery could not be delivered.");
        }
    }

    public async Task<bool> ResetPasswordAsync(HttpContext context, string token, string password)
    {
        if (!options.PasswordRecovery.Enabled || password.Length < options.PasswordRecovery.MinimumPasswordLength ||
            password.Length > 4096) return false;
        var record = await Load(token, ResetFeature, context.RequestAborted).ConfigureAwait(false);
        if (record == null) return false;
        var result = await InUserLock<object>(record.UserId, context, async cancel =>
        {
            record = await Load(token, ResetFeature, cancel).ConfigureAwait(false);
            if (record == null) return null;
            using var account = new SystemAccount();
            var user = await Node.LoadAsync<User>(record.UserId, cancel).ConfigureAwait(false);
            if (!Listed(user)) return null;
            // Consume before changing the password; failures require a new email, never replay.
            await storage.DeleteAccessTokenAsync(record.Value, cancel).ConfigureAwait(false);
            try
            {
                await DeleteFeatureTokens(user.Id, cancel, ResetFeature, MfaFeature, SessionFeature).ConfigureAwait(false);
                await IdentityOperations.ChangePassword(Content.Create(user), context, password).ConfigureAwait(false);
                return new object();
            }
            catch (Exception) when (!cancel.IsCancellationRequested)
            {
                logger.LogWarning("Local password reset could not be completed.");
                return null;
            }
        }).ConfigureAwait(false);
        return result != null;
    }

    private async Task<string> Issue(int userId, string feature, TimeSpan lifetime, CancellationToken cancel)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        await storage.SaveAccessTokenAsync(new AccessToken
        {
            UserId = userId, Feature = feature, Value = LocalAuthenticationPolicy.Hash(token),
            CreationDate = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.Add(lifetime)
        }, cancel).ConfigureAwait(false);
        return token;
    }

    private async Task<AccessToken?> Load(string token, string feature, CancellationToken cancel)
    {
        if (token.Length != 64) return null;
        var record = await storage.LoadAccessTokenAsync(LocalAuthenticationPolicy.Hash(token), 0, feature, cancel).ConfigureAwait(false);
        return record is { UserId: > 0 } && record.ExpirationDate > DateTime.UtcNow ? record : null;
    }

    private async Task DeleteFeatureTokens(int userId, CancellationToken cancel, params string[] features)
    {
        foreach (var token in await storage.LoadAccessTokensAsync(userId, cancel).ConfigureAwait(false))
            if (features.Contains(token.Feature))
                await storage.DeleteAccessTokenAsync(token.Value, cancel).ConfigureAwait(false);
    }

    private async Task<T?> InUserLock<T>(int userId, HttpContext context,
        Func<CancellationToken, Task<T?>> action) where T : class
    {
        var original = context.RequestAborted;
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(original);
        bounded.CancelAfter(TimeSpan.FromSeconds(30));
        await using var held = await locks.TryAcquireAsync(userId, bounded.Token).ConfigureAwait(false);
        if (held == null) return null;
        context.RequestAborted = bounded.Token;
        try { return await action(bounded.Token).ConfigureAwait(false); }
        finally { context.RequestAborted = original; }
    }
}
