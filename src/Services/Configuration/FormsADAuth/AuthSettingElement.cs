using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SenseNet.Configuration
{
    public class AuthSettingElement : ConfigurationElement
    {
        /* ==================================================================================== Public Consts */
        public const string LoginProperty = "sAMAccountName";


        /* ==================================================================================== Members */
        private const string VirtualADUserName = "virtualADUser";
        private const string CustomLoginPropertyName = "customLoginProperty";
        private const string ADServerName = "adServer";
        private const string DomainName = "domain";
        private const string CustomADAdminAccountNameName = "customADAdminAccountName";
        private const string CustomADAdminAccountPwdName = "customADAdminAccountPwd";


        /* ==================================================================================== Configuration element properties */
        [ConfigurationProperty(VirtualADUserName, DefaultValue = "false", IsRequired = false)]
        public Boolean VirtualADUser
        {
            get
            {
                return (Boolean)this[VirtualADUserName];
            }
            set
            {
                this[VirtualADUserName] = value;
            }
        }
        [ConfigurationProperty(CustomLoginPropertyName)]
        public string CustomLoginProperty
        {
            get
            {
                return (string)this[CustomLoginPropertyName];
            }
            set
            {
                this[CustomLoginPropertyName] = value;
            }
        }
        [ConfigurationProperty(ADServerName)]
        public string ADServer
        {
            get
            {
                var adPath = (string) this[ADServerName];
                if (!adPath.StartsWith("LDAP://"))
                    adPath = string.Concat("LDAP://", adPath);

                return adPath;
            }
            set
            {
                this[ADServerName] = value;
            }
        }
        [ConfigurationProperty(DomainName)]
        public string Domain
        {
            get
            {
                return (string)this[DomainName];
            }
            set
            {
                this[DomainName] = value;
            }
        }
        [ConfigurationProperty(CustomADAdminAccountNameName)]
        public string CustomADAdminAccountName
        {
            get
            {
                return (string)this[CustomADAdminAccountNameName];
            }
            set
            {
                this[CustomADAdminAccountNameName] = value;
            }
        }
        [ConfigurationProperty(CustomADAdminAccountPwdName)]
        public string CustomADAdminAccountPwd
        {
            get
            {
                return (string)this[CustomADAdminAccountPwdName];
            }
            set
            {
                this[CustomADAdminAccountPwdName] = value;
            }
        }
    }
}
