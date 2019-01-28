using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SenseNet.Configuration
{
    public class FormsAuthenticationFromADSection : ConfigurationSection
    {
        private const string FormsAuthenticationFromADSectionName = "sensenet/formsAuthenticationFromAD";
        private const string AuthSettingsName = "authSettings";

        [ConfigurationProperty(AuthSettingsName)]
        public AuthSettingElementCollection AuthSettings
        {
            get
            {
                return (AuthSettingElementCollection)this[AuthSettingsName];
            }
            set
            {
                this[AuthSettingsName] = value;
            }
        }

        public static FormsAuthenticationFromADSection Current
        {
            get
            {
                var section = (FormsAuthenticationFromADSection)ConfigurationManager.GetSection(FormsAuthenticationFromADSectionName);
                if (section == null)
                    section = new FormsAuthenticationFromADSection() { AuthSettings = new AuthSettingElementCollection() };
                return section;
            }
        }

        public static List<AuthSettingElement> GetFormsADAuthSettings()
        {
            var formsADAuthSettings = new List<AuthSettingElement>();

            var authSettings = Current.AuthSettings as AuthSettingElementCollection;
            foreach (AuthSettingElement authSetting in authSettings)
            {
                formsADAuthSettings.Add(authSetting);
            }
            return formsADAuthSettings;
        }
    }
}
