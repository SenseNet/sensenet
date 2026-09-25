using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace SenseNet.Authentication.Local;

internal static class LocalAuthenticationForwarding
{
    public static void UseTrustedForwarding(IApplicationBuilder app, LocalAuthenticationOptions options)
    {
        if (options.KnownProxies.Length + options.KnownNetworks.Length == 0)
            return;
        var forwarding = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = options.ForwardLimit,
            RequireHeaderSymmetry = true
        };
        // Do not silently trust ASP.NET's default loopback proxy.
        forwarding.KnownProxies.Clear();
        forwarding.KnownNetworks.Clear();
        foreach (var proxy in options.KnownProxies)
            forwarding.KnownProxies.Add(IPAddress.Parse(proxy));
        foreach (var network in options.KnownNetworks.Select(System.Net.IPNetwork.Parse))
            forwarding.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(network.BaseAddress, network.PrefixLength));
        app.UseForwardedHeaders(forwarding);
    }
}
