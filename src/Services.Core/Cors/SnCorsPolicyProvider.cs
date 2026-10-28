using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository;
using SenseNet.Portal;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Cors
{
    /// <summary>
    /// Provides a CORS policy for sensenet, based on the allowed domains 
    /// and other settings in PortalSettings in the repository.
    /// </summary>
    internal class SnCorsPolicyProvider : ICorsPolicyProvider
    {
        public const string DefaultSenseNetCorsPolicyName = "sensenet";

        private readonly CorsOptions _options;

        public SnCorsPolicyProvider(IOptions<CorsOptions> options)
        {
            _options = options?.Value ?? new CorsOptions();
        }

        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            var originHeader = context.Request.Headers["Origin"].FirstOrDefault();

            // unknown policy name or origin header not present: default behavior
            if (string.IsNullOrEmpty(policyName) || 
                string.IsNullOrEmpty(originHeader) ||
                !string.Equals(policyName, DefaultSenseNetCorsPolicyName, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(originHeader, "null", StringComparison.InvariantCultureIgnoreCase))
                return Task.FromResult(_options.GetPolicy(policyName ?? _options.DefaultPolicyName));
            
            var policyBuilder = new CorsPolicyBuilder();

            // Load current CORS settings from the repository. This must not be cached here,
            // because settings may change at runtime, anytime.
            var corsSettings =
                Settings.GetValue<IEnumerable<string>>(PortalSettings.SETTINGSNAME,
                    PortalSettings.SETTINGS_ALLOWEDORIGINDOMAINS, null,
                    SnCorsConstants.DefaultAllowedDomains);

            // get a configured domain (or template) that matches the origin sent by the client
            var allowedDomain = GetAllowedDomain(originHeader, corsSettings);

            if (!string.IsNullOrEmpty(allowedDomain))
            {
                // template match: set the allowed origin
                policyBuilder.WithOrigins(originHeader);

                // any origin ('*') and credentials are mutually exclusive
                if (!string.Equals(originHeader, CorsConstants.AnyOrigin))
                    policyBuilder.AllowCredentials();
                        
                var allowedMethods = Settings.GetValue(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_ALLOWEDMETHODS, null,
                    SnCorsConstants.AccessControlAllowMethodsDefault);
                var allowedHeaders = Settings.GetValue(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_ALLOWEDHEADERS, null,
                    SnCorsConstants.AccessControlAllowHeadersDefault);

                policyBuilder.WithMethods(allowedMethods);
                policyBuilder.WithHeaders(allowedHeaders);
            }

            return Task.FromResult(policyBuilder.Build());
        }

        internal static string GetAllowedDomain(string originDomain, IEnumerable<string> allowedDomains)
        {
            if (!TrySplitDomain(originDomain, false, out var originScheme, out var host, out var originPort))
                return null;

            bool TemplateMatch(string template)
            {
                if (template == CorsConstants.AnyOrigin)
                    return true;

                if (!TrySplitDomain(template, true, out var scheme, out var templateHost, out var port))
                    return false;

                return (scheme == null || string.Equals(scheme, originScheme, StringComparison.OrdinalIgnoreCase)) &&
                       (port == "*" || port == originPort) &&
                       MatchesDomain(templateHost, host);
            }

            return allowedDomains?.FirstOrDefault(TemplateMatch);
        }

        private static bool TrySplitDomain(string value, bool allowWildcards, out string scheme,
            out string[] hostParts, out string port)
        {
            scheme = null;
            hostParts = null;
            port = null;
            if (string.IsNullOrEmpty(value) || value.Any(char.IsWhiteSpace) ||
                (!allowWildcards && string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)))
                return false;

            // Preserve scheme-less domain settings and callers; explicit schemes must be HTTP(S).
            var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0)
            {
                scheme = value.Substring(0, schemeEnd);
                if (!string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    return false;
                value = value.Substring(schemeEnd + 3);
            }

            // A bracketed IPv6 address may contain colons; its port follows the closing bracket.
            var ipv6 = value.StartsWith("[", StringComparison.Ordinal);
            var hostEnd = ipv6 ? value.IndexOf(']') + 1 : value.IndexOf(':');
            if (hostEnd == 0)
                return false;
            if (hostEnd < 0)
                hostEnd = value.Length;

            var host = value.Substring(0, hostEnd);
            if (host.Length == 0 || host.Length > 253)
                return false;
            if (hostEnd < value.Length)
            {
                if (value[hostEnd] != ':')
                    return false;
                port = value.Substring(hostEnd + 1);
                if (!(allowWildcards && port == "*") &&
                    (!int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var portNumber) ||
                     portNumber > 65535))
                    return false;
            }

            // Keep explicit ports distinct from omitted ones, including default ports.
            // A configured :* matches either form.
            hostParts = ipv6 ? new[] { host } : host.Split('.');
            if (hostParts.Any(p => p.Length == 0 ||
                                   (p.Contains('*') && !(allowWildcards && p == "*"))))
                return false;

            // Validate wildcard templates using a single valid label in place of each wildcard.
            var hostType = Uri.CheckHostName(string.Join(".", hostParts.Select(p => p == "*" ? "a" : p)));
            return ipv6 ? hostType == UriHostNameType.IPv6 :
                hostType == UriHostNameType.Dns || hostType == UriHostNameType.IPv4;
        }

        private static bool MatchesDomain(string[] template, string[] host)
        {
            int x = 0, y = 0;
            int starX = -1, starEnd = -1;

            while (y < host.Length)
            {
                if (x < template.Length && template[x] == "*")
                {
                    // Each wildcard initially consumes one complete domain label.
                    starX = x;
                    starEnd = y + 1;
                    x++;
                    y = starEnd;
                }
                else if (x < template.Length &&
                         string.Equals(template[x], host[y], StringComparison.OrdinalIgnoreCase))
                {
                    x++;
                    y++;
                }
                else if (starX >= 0 && starEnd < host.Length)
                {
                    // Extend the last wildcard and retry the following template labels.
                    starEnd++;
                    x = starX + 1;
                    y = starEnd;
                }
                else
                {
                    return false;
                }
            }

            return x == template.Length;
        }
    }
}
