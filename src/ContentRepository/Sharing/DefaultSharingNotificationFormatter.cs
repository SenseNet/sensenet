using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Default implementation of the ISharingNotificationFormatter interface.
    /// Provides a static unchanged subject and inserts the sharing level and 
    /// the content url with the sharing identifier to the body.
    /// </summary>
    public class DefaultSharingNotificationFormatter : ISharingNotificationFormatter
    {
        public string FormatSubject(Node node, SharingData sharingData, string subject)
        {
            // default implementation: static subject
            return subject;
        }

        public string FormatBody(Node node, SharingData sharingData, string siteUrl, string body)
        {
            //TODO: send a site-relative path
            // Alternative: send an absolute path, but when a request arrives
            // containing a share guid, redirect to the more compact and readable path.
            var url = $"{siteUrl?.TrimEnd('/')}{node.Path}?share={sharingData.Id}";

            // sharing level info: Open, Edit, etc.
            var levelText = SR.GetString("$Sharing:SharingLevel_" + sharingData.Level);

            return string.Format(body, url, levelText);
        }
    }
}
