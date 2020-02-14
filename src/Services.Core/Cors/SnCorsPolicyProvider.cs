using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Cors
{
    internal class SnCorsPolicyProvider : ICorsPolicyProvider
    {
        public const string DefaultSenseNetCorsPolicyName = "sensenet";

        private readonly CorsOptions _options;

        public SnCorsPolicyProvider(IOptions<CorsOptions> options)
        {
            _options = options.Value;
        }

        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            if (string.Equals(policyName, DefaultSenseNetCorsPolicyName, StringComparison.InvariantCultureIgnoreCase))
            {
                var originHeader = context.Request.Headers["Origin"].FirstOrDefault();
                if (!string.IsNullOrEmpty(originHeader) &&
                    string.Compare(originHeader, "null", StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    var policyBuilder = new CorsPolicyBuilder();

                    var corsSettings =
                        Settings.GetValue<IEnumerable<string>>("Portal", "AllowedOriginDomains", null,
                            new[]
                            {
                                "*.sensenet.com",
                                "localhost:*"
                            });
                    var allowedDomain = GetAllowedDomain(originHeader, corsSettings);

                    if (!string.IsNullOrEmpty(allowedDomain))
                    {
                        policyBuilder.WithOrigins(originHeader);
                        if (!string.Equals(originHeader, CorsConstants.AnyOrigin))
                            policyBuilder.AllowCredentials();
                        
                        var allowedMethods = Settings.GetValue("Portal", "AllowedMethods", null,
                            SnCorsConstants.AccessControlAllowMethodsDefault);
                        var allowedHeaders = Settings.GetValue("Portal", "AllowedHeaders", null,
                            SnCorsConstants.AccessControlAllowHeadersDefault);

                        policyBuilder.WithMethods(allowedMethods);
                        policyBuilder.WithHeaders(allowedHeaders);
                    }

                    return Task.FromResult(policyBuilder.Build());
                }
            }

            // default behavior
            return Task.FromResult(_options.GetPolicy(policyName ?? _options.DefaultPolicyName));
        }

        internal static string GetAllowedDomain(string originDomain, IEnumerable<string> allowedDomains)
        {
            bool TemplateMatch(string template)
            {
                if (string.IsNullOrEmpty(template) || !template.Contains("*"))
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
                string.Compare(d, originDomain, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                TemplateMatch(d));
        }
    }
}
