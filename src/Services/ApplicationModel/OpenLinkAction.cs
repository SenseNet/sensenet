namespace SenseNet.ApplicationModel
{
    public class OpenLinkAction : UrlAction
    {
        public override string Uri
        {
            get
            {
                if (this.Content == null || !this.Content.Fields.ContainsKey("Url"))
                    return base.Uri;

                var link = this.Content["Url"] as string;
                return string.IsNullOrEmpty(link) ? base.Uri : link;
            }
        }
    }
}
