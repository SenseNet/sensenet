using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Email
{
    public interface IEmailTemplate
    {
        string Subject { get; }
        string Body { get; }
    }

    public interface IEmailTemplateManager
    {
        Task<IEmailTemplate> GetEmailTemplateAsync(string templateName);
    }
}
