using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace SenseNet.Authentication.Local;

internal sealed class LocalAuthenticationPolicy
{
    private readonly System.Net.IPNetwork[] _loginNetworks;
    private readonly System.Net.IPNetwork[] _tokenNetworks;
    private readonly LocalAuthenticationOptions _options;
    private readonly Dictionary<string, (DateTimeOffset Until, int Count)> _attempts = new();
    private readonly object _lock = new();

    public LocalAuthenticationPolicy(LocalAuthenticationOptions options)
    {
        _options = options;
        _loginNetworks = options.LoginNetworks.Select(System.Net.IPNetwork.Parse).ToArray();
        _tokenNetworks = (options.TokenNetworks ?? options.LoginNetworks).Select(System.Net.IPNetwork.Parse).ToArray();
    }

    public bool Allows(HttpContext context, bool token = false)
    {
        // Trusted forwarded headers must already have been consumed by the host's
        // ForwardedHeaders middleware. Never interpret raw forwarded headers here.
        if (!context.Request.IsHttps || context.Connection.RemoteIpAddress is not { } address ||
            context.Request.Headers.ContainsKey("X-Forwarded-For") ||
            context.Request.Headers.ContainsKey("X-Forwarded-Proto") ||
            context.Request.Headers.ContainsKey("Forwarded"))
            return false;
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        return (token ? _tokenNetworks : _loginNetworks).Any(network => network.Contains(address));
    }

    public bool TryAttempt(HttpContext context, string? account = null)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_lock)
        {
            foreach (var key in _attempts.Where(p => p.Value.Until <= now).Select(p => p.Key).ToArray())
                _attempts.Remove(key);
            // Bound attacker-controlled state; deny new partitions when full.
            if (_attempts.Count >= 10000)
                return false;
            var ip = context.Connection.RemoteIpAddress!;
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();
            var allowed = account != null || Take("ip:" + ip, _options.AttemptsPerMinutePerIp, now);
            if (account != null)
                allowed &= Take("account:" + Hash(account.Trim().ToUpperInvariant()),
                    _options.AttemptsPerMinutePerAccount, now);
            return allowed;
        }
    }

    private bool Take(string key, int limit, DateTimeOffset now)
    {
        var state = _attempts.GetValueOrDefault(key, (now.AddMinutes(1), 0));
        _attempts[key] = (state.Item1, Math.Min(state.Item2 + 1, limit + 1));
        return state.Item2 < limit;
    }

    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
