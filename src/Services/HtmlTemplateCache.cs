using SenseNet.ContentRepository;

namespace SenseNet.Portal.UI
{
    internal class HtmlTemplateCache : JsonTreeCache<HtmlTemplate>
    {
        protected override string LocalFolderName
        {
            get { return "Templates"; }
        }

        static HtmlTemplateCache()
        {
            _cacheType = typeof(HtmlTemplateCache);
        }
    }
}
