using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository;
using SenseNet.Portal;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Cors
{
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
            bool TemplateMatch(string template)
            {
                if (string.Equals(template, originDomain, StringComparison.InvariantCultureIgnoreCase))
                    return true;

                if (string.IsNullOrEmpty(template))
                    return false;

                // If there is a port wildcard, we will match all ports,
                // including the not present default port.
                if (template.EndsWith(":*"))
                    template = template.Replace(":*", "(:[\\d]+){0,1}");

                // subdomain wildcard
                template = template.Replace("*", "[\\da-z.-]+");

                // template prefix: optional schema (http or https) is acceptable
                var regex = new Regex($"^([\\w]+:\\/\\/){{0,1}}{template}$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                return regex.IsMatch(originDomain);
            }

            return allowedDomains?.FirstOrDefault(d =>
                d == CorsConstants.AnyOrigin ||
                TemplateMatch(d));
        }
    }
}
