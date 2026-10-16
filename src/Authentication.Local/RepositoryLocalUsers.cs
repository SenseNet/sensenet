using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Operations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local;

/// <summary>Repository credentials and authorization policy used by the local issuer.</summary>
public interface ILocalAuthenticationUsers
{
    Task<int?> ValidateAsync(HttpContext context, string username, string password, string? twoFactorCode);
    Task<bool> IsAllowedAsync(int userId, CancellationToken cancellationToken);
}

internal sealed class RepositoryLocalUsers(LocalAuthenticationOptions options, LocalAuthenticationPolicy policy) : ILocalAuthenticationUsers
{
    public async Task<int?> ValidateAsync(HttpContext context, string username, string password, string? twoFactorCode)
    {
        try
        {
            using (new SystemAccount())
            {
                var candidate = User.Load(username);
                if (candidate != null && !policy.TryAttempt(context, "id:" + candidate.Id))
                    return null;
            }
            var result = IdentityOperations.ValidateCredentials(null!, context, username, password);
            return await SystemAccount.ExecuteAsync(async () =>
            {
                var user = await Node.LoadAsync<User>(result.Id, context.RequestAborted).ConfigureAwait(false);
                if (!IsAllowed(user))
                    return (int?)null;
                if (RequiresMfa(user) || user.EffectiveMultiFactorEnabled)
                {
                    if (string.IsNullOrWhiteSpace(twoFactorCode))
                        return null;
                    await IdentityOperations.ValidateTwoFactorCode(Content.Create(user), context, twoFactorCode)
                        .ConfigureAwait(false);
                }
                return (int?)user.Id;
            }).ConfigureAwait(false);
        }
        catch (SenseNetSecurityException) { return null; }
        catch (MissingDomainException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public Task<bool> IsAllowedAsync(int userId, CancellationToken cancellationToken) =>
        SystemAccount.ExecuteAsync(async () =>
            IsAllowed(await Node.LoadAsync<User>(userId, cancellationToken).ConfigureAwait(false)));

    private bool RequiresMfa(User user) => options.RequireMultiFactor ||
        (options.Mode == LocalAuthenticationMode.Secondary &&
         (user.Id == 1 || user.IsInGroup(Group.Administrators)));

    private bool IsAllowed(User? user) => user is { Enabled: true } && user.Id > 0 &&
        user.Id != User.Visitor.Id &&
        (options.AllowedUserIds.Contains(user.Id) || options.AllowedGroupIds.Any(user.IsInGroup)) &&
        (!RequiresMfa(user) || (user.EffectiveMultiFactorEnabled && user.MultiFactorRegistered));
}
