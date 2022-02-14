using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Email
{
    internal class RepositoryEmailTemplateManager : IEmailTemplateManager
    {
        private const string EmailTemplateRoot = "/Root/System/Templates/Email";

        public async Task<IEmailTemplate> GetEmailTemplateAsync(string templateName)
        {
            var template =
                await Node.LoadAsync<EmailTemplate>($"{EmailTemplateRoot}/{templateName}", CancellationToken.None)
                    .ConfigureAwait(false) ?? 
                await Node.LoadAsync<EmailTemplate>($"{EmailTemplateRoot}/{templateName}.html", CancellationToken.None)
                    .ConfigureAwait(false);

            return template;
        }
    }
}
