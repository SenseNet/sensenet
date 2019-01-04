using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SenseNet.Configuration
{
    public class UrlListSection : ConfigurationSection
    {
        private const string UrlListSectionName = "sensenet/urlList";
        private const string SitesName = "sites";

        [ConfigurationProperty(SitesName)]
        public SiteElementCollection Sites
        {
            get
            {
                return (SiteElementCollection)this[SitesName];
            }
            set
            {
                this[SitesName] = value;
            }
        }

        public static UrlListSection Current
        {
            get
            {
                var section = (UrlListSection)ConfigurationManager.GetSection(UrlListSectionName);
                if (section == null)
                    section = new UrlListSection() { Sites = new SiteElementCollection() };
                return section;
            }
        }
    }
}
