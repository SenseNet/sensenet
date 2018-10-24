using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Defines methods for formatting emails that are sent to users after sharing a content with them.
    /// </summary>
    public interface ISharingNotificationFormatter
    {
        /// <summary>
        /// Formats the subject of the email.
        /// </summary>
        string FormatSubject(Node node, SharingData sharingData, string subject);
        /// <summary>
        /// Formats the body of the email.
        /// </summary>
        string FormatBody(Node node, SharingData sharingData, string siteUrl, string body);
    }
}
