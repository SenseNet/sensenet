using System.Configuration;

namespace SenseNet.Configuration
{
    public class UrlElement : ConfigurationElement
    {
        /* ==================================================================================== Members */
        private const string HostName = "host";
        private const string AuthName = "auth";


        /* ==================================================================================== Configuration element properties */
        [ConfigurationProperty(HostName)]
        public string Host
        {
            get
            {
                return (string)this[HostName];
            }
            set
            {
                this[HostName] = value;
            }
        }
        [ConfigurationProperty(AuthName)]
        public string Auth
        {
            get
            {
                return (string)this[AuthName];
            }
            set
            {
                this[AuthName] = value;
            }
        }

    }
}
