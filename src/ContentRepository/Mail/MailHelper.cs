using System;
using System.Collections.Generic;
using System.Net.Mail;
using OpenPop.Pop3;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Mail
{
    public enum MailProcessingMode { ExchangePull, ExchangePush, POP3, IMAP }
    public class POP3Settings
    {
        public string Server = string.Empty;
        public string Password = string.Empty;
        public int Port;
        public bool SSL;
    }

    public class MailHelper
    {
        public static readonly string MAILPROCESSOR_SETTINGS = "MailProcessor";
        public static readonly string SETTINGS_POP3 = "POP3";
        public static readonly string SETTINGS_MODE = "MailProcessingMode";
        public static readonly string SETTINGS_POLLINGINTERVAL = "StatusPollingIntervalInMinutes";
        public static readonly string SETTINGS_SERVICEPATH = "PushNotificationServicePath";
        public static readonly string SETTINGS_EXCHANGEADDRESS = "ExchangeAddress";

        public static bool IsExchangeModeEnabled
        {
            get
            {
                var mpm = Settings.GetValue<MailProcessingMode>(MAILPROCESSOR_SETTINGS, SETTINGS_MODE);
                return mpm == MailProcessingMode.ExchangePull || mpm == MailProcessingMode.ExchangePush;
            }
        }

        public static MailMessage[] GetMailMessages(string contentListPath)
        {
            switch (Settings.GetValue(MAILPROCESSOR_SETTINGS, SETTINGS_MODE, contentListPath, MailProcessingMode.ExchangePull))
            {
                case MailProcessingMode.ExchangePull:
                case MailProcessingMode.ExchangePush:
                    throw new SnNotSupportedException("Exchange mail processing modes are not supported here. Use the ExchangeHelper class instead.");
                case MailProcessingMode.POP3:
                    return GetMailMessagesByPOP3(contentListPath);
                default:
                    throw new SnNotSupportedException("Unknown mail processing mode");
            }
        }

        private static MailMessage[] GetMailMessagesByPOP3(string contentListPath)
        {
            var messages = new List<MailMessage>();
            var credentials = MailProvider.Instance.GetPOP3Credentials(contentListPath);
            var pop3s = Settings.GetValue<POP3Settings>(MAILPROCESSOR_SETTINGS, SETTINGS_POP3, contentListPath) ?? new POP3Settings();

            using (var client = new Pop3Client())
            {
                try
                {
                    client.Connect(pop3s.Server, pop3s.Port, pop3s.SSL);
                    client.Authenticate(credentials.Username, credentials.Password);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Mail processor workflow error: connecting to mail server " + pop3s.Server + " with the username " + credentials.Username + " failed.");
                    return messages.ToArray();
                }

                int messageCount;

                try
                {
                    messageCount = client.GetMessageCount();
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Mail processor workflow error: getting messages failed. Content list: " + contentListPath);
                    return messages.ToArray();
                }

                // Messages are numbered in the interval: [1, messageCount]
                // Most servers give the latest message the highest number
                for (var i = messageCount; i > 0; i--)
                {
                    try
                    {
                        var msg = client.GetMessage(i);
                        var mailMessage = msg.ToMailMessage();
                        messages.Add(mailMessage);
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex, "Mail processor workflow error. Content list: " + contentListPath);
                    }
                }

                try
                {
                    client.DeleteAllMessages();
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, "Mail processor workflow error: deleting messages failed. Content list: " + contentListPath);
                }
            }

            SnTrace.Workflow.Write("MailPoller workflow: " + messages.Count + " messages received. Content list: " + contentListPath);

            return messages.ToArray();
        }
    }
}
