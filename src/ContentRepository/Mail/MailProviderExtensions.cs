using SenseNet.Configuration;
using SenseNet.ContentRepository.Mail;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class MailProviderExtensions
    {
        public static IRepositoryBuilder UseMailProvider(this IRepositoryBuilder repositoryBuilder, MailProvider mailProvider)
        {
            Providers.Instance.SetProvider(typeof(MailProvider), mailProvider);
            SnLog.WriteInformation($"MailProvider created: {mailProvider?.GetType().FullName}");

            return repositoryBuilder;
        }
    }
}
