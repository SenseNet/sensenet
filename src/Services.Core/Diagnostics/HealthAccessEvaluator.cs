using System;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Storage.Security;

namespace SenseNet.Services.Core.Diagnostics;

/// <summary>Selects a health visibility level for the authenticated request.</summary>
public interface IHealthAccessEvaluator
{
    bool CanReadDetails(HttpContext context);
}

internal sealed class HealthAccessEvaluator(ISnCache cache, SecurityHandler security, HealthRepositoryLifecycle lifecycle)
    : IHealthAccessEvaluator
{
    private const string DevelopersCacheKey = "SenseNet.Health.DevelopersGroup";
    private static readonly object AuthenticatedUserKey = new();

    // Capture before sample applications or other middleware elevate User.Current.
    internal static void SetAuthenticatedUser(HttpContext context, IUser user) => context.Items[AuthenticatedUserKey] = user;

    public bool CanReadDetails(HttpContext context)
    {
        if (!context.Items.TryGetValue(AuthenticatedUserKey, out var value) || value is not IUser user)
            return false;
        if (user == HealthCheckerUser.Instance) return true;
        if (!lifecycle.Ready || user.Id <= 0 || user.Id == Identifiers.VisitorUserId || user is User { Enabled: false })
            return false;
        try
        {
            var developersId = cache.Get(DevelopersCacheKey) as int? ?? 0;
            return security.IsInGroup(user.Id, Identifiers.AdministratorsGroupId) ||
                developersId > 0 && security.IsInGroup(user.Id, developersId);
        }
        catch { return false; } // Unavailable security state must never grant details.
    }

    // Background-only. Existing distributed cache dependencies invalidate rename, move and deletion.
    internal void RefreshDeveloperGroup()
    {
        if (!lifecycle.Ready || cache.Get(DevelopersCacheKey) != null) return;
        using var account = new SystemAccount();
        var head = NodeHead.Get(N.R.Developers);
        if (head != null)
            cache.Insert(DevelopersCacheKey, head.Id, CacheDependencyFactory.CreateNodeDependency(head));
    }
}
