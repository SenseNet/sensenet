using System.Configuration;

namespace SenseNet.Configuration
{
    public class SiteElement : ConfigurationElement
    {
        /* ==================================================================================== Members */
        private const string PathName = "path";
        private const string UrlsName = "urls";


        /* ==================================================================================== Configuration element properties */
        [ConfigurationProperty(PathName)]
        public string Path
        {
            get
            {
                return (string)this[PathName];
            }
            set
            {
                this[PathName] = value;
            }
        }

        [ConfigurationProperty(UrlsName)]
        public UrlElementCollection Urls
        {
            get
            {
                return (UrlElementCollection)this[UrlsName];
            }
            set
            {
                this[UrlsName] = value;
            }
        }
    }
}
