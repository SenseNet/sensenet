using System.Collections.Generic;
using System.Configuration;

namespace SenseNet.Configuration
{
    [ConfigurationCollection(typeof(UrlElement), AddItemName = "url",
          CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class UrlElementCollection : ConfigurationElementCollection
    {
        static UrlElementCollection()
        {
            m_properties = new ConfigurationPropertyCollection();
        }

        public UrlElementCollection()
        {
        }

        private static ConfigurationPropertyCollection m_properties;

        protected override ConfigurationPropertyCollection Properties
        {
            get { return m_properties; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new UrlElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as UrlElement).Host;
        }

        public UrlElement this[int index]
        {
            get { return (UrlElement)base.BaseGet(index); }
        }

        public new UrlElement this[string host]
        {
            get { return (UrlElement)base.BaseGet(host); }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "url"; }
        }

        // helpers
        public List<string> GetUrlHosts()
        {
            var hosts = new List<string>();
            foreach (UrlElement urlSetting in this)
            {
                hosts.Add(urlSetting.Host);
            }
            return hosts;
        }
    }
}
