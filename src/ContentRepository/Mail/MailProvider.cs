using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;
// ReSharper disable InconsistentNaming

namespace SenseNet.ContentRepository.Mail
{
    public class MailServerCredentials
    {
        public string Username = string.Empty;
        public string Password = string.Empty;
    }

    public abstract class MailProvider
    {
        // ================================================================================ Static instance

        private static readonly object MailProviderLock = new object();

        public static MailProvider Instance
        {
            get
            {
                var instance = Providers.Instance.GetProvider<MailProvider>();
                if (instance == null)
                {
                    lock (MailProviderLock)
                    {
                        instance = Providers.Instance.GetProvider<MailProvider>();

                        if (instance == null)
                        {
                            try
                            {
                                var mailProviderTypes = TypeResolver.GetTypesByBaseType(typeof(MailProvider))
                                    .Where(t => t.FullName != typeof(MailProvider).FullName &&
                                                t.FullName != typeof(DefaultMailProvider).FullName).ToArray();

                                if (mailProviderTypes.Length > 0)
                                {
                                    if (mailProviderTypes.Length > 1)
                                    {
                                        SnLog.WriteWarning("Too many mailprovider implementations exist, there must be only one. Fallback to default provider.", 
                                            properties: new Dictionary<string, object> { { "Mailproviders", string.Join(", ", mailProviderTypes.Select(t => t.FullName))}});
                                    }
                                    else
                                    {
                                        instance = (MailProvider)TypeResolver.CreateInstance(mailProviderTypes.First().FullName); 
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                SnLog.WriteException(ex);
                            }
                            
                            // fallback to the default
                            if (instance == null)
                                instance = (MailProvider)TypeResolver.CreateInstance(typeof(DefaultMailProvider).FullName);

                            Providers.Instance.SetProvider(typeof(MailProvider), instance);

                            SnLog.WriteInformation("MailProvider created: " + instance.GetType().FullName);
                        }
                    }
                }

                return instance;
            }
        }

        // ================================================================================ Instance API

        public virtual bool IsExchangeModeEnabled { get; } = false;

        public virtual MailServerCredentials GetPOP3Credentials(string contentListPath)
        {
            return new MailServerCredentials();
        }

        public virtual MailMessage[] GetMailMessages(string contentListPath)
        {
            return new MailMessage[0];
        }

        public virtual void OnListEmailChanged(ContentList list) {}
    }

    /// <summary>
    /// Default mail provider implementation.
    /// </summary>
    public class DefaultMailProvider : MailProvider
    {
    }

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
