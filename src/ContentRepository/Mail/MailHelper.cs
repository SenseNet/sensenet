using System.Net.Mail;

// ReSharper disable InconsistentNaming

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

        public static bool IsExchangeModeEnabled => MailProvider.Instance.IsExchangeModeEnabled;

        public static MailMessage[] GetMailMessages(string contentListPath)
        {
            return MailProvider.Instance.GetMailMessages(contentListPath);
        }
    }
}
