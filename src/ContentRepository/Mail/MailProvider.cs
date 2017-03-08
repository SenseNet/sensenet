using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Tools;

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

        private static readonly object _mailProviderLock = new object();
        private static MailProvider _instance;
        public static MailProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_mailProviderLock)
                    {
                        if (_instance == null)
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
                                        _instance = (MailProvider)TypeResolver.CreateInstance(mailProviderTypes.First().FullName); 
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                SnLog.WriteException(ex);
                            }
                            
                            // fallback to the default
                            if (_instance == null)
                                _instance = (MailProvider)TypeResolver.CreateInstance(typeof(DefaultMailProvider).FullName);

                            SnLog.WriteInformation("MailProvider created: " + _instance.GetType().FullName);
                        }
                    }
                }

                return _instance;
            }
        }

        // ================================================================================ Instance API

        public abstract MailServerCredentials GetPOP3Credentials(string contentListPath);
    }

    public class DefaultMailProvider : MailProvider
    {
        public override MailServerCredentials GetPOP3Credentials(string contentListPath)
        {
            var contentList = Node.LoadNode(contentListPath);

            var pop3Settings = Settings.GetValue<POP3Settings>(MailHelper.MAILPROCESSOR_SETTINGS, MailHelper.SETTINGS_POP3, contentListPath) ?? new POP3Settings();
            var username = contentList["ListEmail"] as string;

            return new MailServerCredentials { Username = username, Password = pop3Settings.Password };
        }
    }
}
