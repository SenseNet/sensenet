using System.Configuration;

namespace SenseNet.Configuration
{
    [ConfigurationCollection(typeof(SiteElement), AddItemName = "site",
          CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class SiteElementCollection : ConfigurationElementCollection
    {
        static SiteElementCollection()
        {
            m_properties = new ConfigurationPropertyCollection();
        }

        public SiteElementCollection()
        {
        }

        private static ConfigurationPropertyCollection m_properties;

        protected override ConfigurationPropertyCollection Properties
        {
            get { return m_properties; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SiteElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as SiteElement).Path;
        }

        public SiteElement this[int index]
        {
            get { return (SiteElement)base.BaseGet(index); }
        }

        public new SiteElement this[string path]
        {
            get { return (SiteElement)base.BaseGet(path); }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "site"; }
        }
    }
}
