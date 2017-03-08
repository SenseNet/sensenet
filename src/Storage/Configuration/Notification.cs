// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Notification : SnConfig
    {
        private const string SectionName = "sensenet/notification";

        public static string NotificationSender { get; internal set; } = GetString(SectionName, "NotificationSenderAddress", string.Empty);
        public static string DefaultEmailSender { get; internal set; } = GetString(SectionName, "DefaultEmailSender", "mailservice@example.com");
    }
}
