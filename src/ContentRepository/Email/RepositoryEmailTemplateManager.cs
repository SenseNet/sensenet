using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Email
{
    internal class RepositoryEmailTemplateManager : IEmailTemplateManager
    {
        private readonly ILogger<RepositoryEmailTemplateManager> _logger;
        private const string EmailTemplateRoot = "/Root/System/Templates/Email";

        public RepositoryEmailTemplateManager(ILogger<RepositoryEmailTemplateManager> logger)
        {
            _logger = logger;
        }

        public async Task<IEmailTemplate> GetEmailTemplateAsync(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentNullException(nameof(templateName));

            // Load templates in elevated mode, like settings. This is a backend api,
            // used by features that should not require template content permissions.
            using var sa = new SystemAccount();

            var template =
                await Node.LoadAsync<EmailTemplate>($"{EmailTemplateRoot}/{templateName.Trim('/')}", CancellationToken.None)
                    .ConfigureAwait(false) ?? 
                await Node.LoadAsync<EmailTemplate>($"{EmailTemplateRoot}/{templateName.Trim('/')}.html", CancellationToken.None)
                    .ConfigureAwait(false);

            if (template == null)
                _logger.LogWarning($"Email template {templateName} not found in repository.");

            return template;
        }
    }
}
