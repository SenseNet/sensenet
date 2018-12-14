using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SenseNet.Configuration
{
    [ConfigurationCollection(typeof(AuthSettingElement), AddItemName = "authSetting",
          CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class AuthSettingElementCollection : ConfigurationElementCollection 
    {
        static AuthSettingElementCollection()
        {
            m_properties = new ConfigurationPropertyCollection();
        }

        public AuthSettingElementCollection()
        {
        }

        private static ConfigurationPropertyCollection m_properties;

        protected override ConfigurationPropertyCollection Properties
        {
            get { return m_properties; }
        }
    
        protected override ConfigurationElement CreateNewElement()
        {
            return new AuthSettingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as AuthSettingElement).Domain;
        }

        public AuthSettingElement this[int index]
        {
            get { return (AuthSettingElement)base.BaseGet(index); }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "authSetting"; }
        }
    }
}
