using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;

namespace SenseNet.ContentRepository.Fields
{
    public class ActivationFieldSetting : ShortTextFieldSetting
    {
        public const string EnabledName = "Enabled";
        public const string AdminEmailName = "AdminEmail";
        public const string RequireUniqueEmailName = "RequireUniqueEmail";
        public const string MailDefinitionName = "MailDefinition";
        public const string IsBodyHtmlName = "IsBodyHtml";
        public const string MailSubjectName = "MailSubject";
        public const string MailPriorityName = "MailPriority";
        public const string MailFromName = "MailFrom";

        private bool? _enabled;
        private string _adminEmail;
        private bool? _requireUniqueEmail;
        private string _mailDefinition;
        private bool? _isBodyHtml;
        private string _mailSubject;
        private string _mailFrom;
        private MailPriority _mailPriority;


        public bool? Enabled
        {
            get
            {
                if (this.ParentFieldSetting == null)
                    return null;

                if (_enabled.HasValue)
                    return _enabled.Value;

                return null;
            }
        }
        public string AdminEmail
        {
            get
            {
                if (this.ParentFieldSetting == null)
                    return null;
                return _adminEmail;
            }
        }
        public bool? RequireUniqueEmail
        {
            get
            {
                if (this.ParentFieldSetting == null)
                    return null;
                if (_requireUniqueEmail.HasValue)
                    return _requireUniqueEmail.Value;
                return null;
            }
        }
        public string MailDefinition
        {
            get
            {
                if (_mailDefinition != null)
                {
                    return SenseNetResourceManager.Current.GetString(_mailDefinition);
                }

                return this.ParentFieldSetting != null 
                    ? ((ActivationFieldSetting)this.ParentFieldSetting).MailDefinition 
                    : _mailDefinition;
            }
        }
        public bool? IsBodyHtml
        {
            get
            {
                if (this.ParentFieldSetting == null)
                    return null;
                if (_isBodyHtml.HasValue)
                    return _isBodyHtml.Value;
                return null;
            }
        }
        public string MailSubject
        {
            get
            {
                if (_mailSubject != null)
                {
                    return SenseNetResourceManager.Current.GetString(_mailSubject);
                }

                return this.ParentFieldSetting != null
                    ? ((ActivationFieldSetting)this.ParentFieldSetting).MailSubject
                    : _mailSubject;
            }
        }
        public MailPriority MailPriority
        {
            get
            {
                if (this.ParentFieldSetting == null)
                    return MailPriority.Normal;
                return _mailPriority;
            }
        }
        public string MailFrom
        {
            get
            {
                if (this.ParentFieldSetting == null)
                    return null;
                return _mailFrom;
            }
        }


        protected override void ParseConfiguration(System.Xml.XPath.XPathNavigator configurationElement, System.Xml.IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            // <Enabled>true|false</Enabled>
            // <AdminEmail>valid@email.hu</AdminEmail>
            // <RequireUniqueEmail>true|false</RequireUniqueEmail>
            // <MailDefinition>...</MailDefinition>
            // <IsBodyHtml>true|false</IsBodyHtml>
            // <MailSubject>...</MailSubject>
            // <MailPriority>Low|Normal|High</MailPriority>
            // <MailFrom>valid@email.hu</MailFrom>
            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case EnabledName:
                        bool enabled;
                        if (Boolean.TryParse(node.InnerXml, out enabled))
                            _enabled = enabled;
                        break;
                    case AdminEmailName:
                        _adminEmail = node.InnerXml;
                        break;
                    case RequireUniqueEmailName:
                        bool requireUniqueEmail;
                        if (Boolean.TryParse(node.InnerXml, out requireUniqueEmail))
                            _requireUniqueEmail = requireUniqueEmail;
                        break;
                    case MailDefinitionName:
                        _mailDefinition = node.InnerXml;
                        break;
                    case IsBodyHtmlName:
                        bool isBodyHtml;
                        if (Boolean.TryParse(node.InnerXml, out isBodyHtml))
                            _isBodyHtml = isBodyHtml;
                        break;
                    case MailSubjectName:
                        _mailSubject = node.InnerXml;
                        break;
                    case MailPriorityName:
                        if (node.InnerXml == Enum.GetName(typeof(MailPriority), MailPriority.Low))
                            _mailPriority = MailPriority.Low;
                        else if (node.InnerXml == Enum.GetName(typeof(MailPriority), MailPriority.Normal))
                            _mailPriority = MailPriority.Normal;
                        else if (node.InnerXml == Enum.GetName(typeof(MailPriority), MailPriority.High))
                            _mailPriority = MailPriority.High;
                        else
                            _mailPriority = MailPriority.Normal;
                        break;
                    case MailFromName:
                        _mailFrom = node.InnerXml;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _enabled = GetConfigurationNullableValue<bool>(info, EnabledName, null);
            _adminEmail = GetConfigurationStringValue(info, AdminEmailName, null);
            _requireUniqueEmail = GetConfigurationNullableValue<bool>(info, RequireUniqueEmailName, null);
            _mailDefinition = GetConfigurationStringValue(info, MailDefinitionName, null);
            _isBodyHtml = GetConfigurationNullableValue<bool>(info, IsBodyHtmlName, null);
            _mailSubject = GetConfigurationStringValue(info, MailSubjectName, null);
            _mailPriority = GetConfigurationValue<MailPriority>(info, MailPriorityName, MailPriority.Normal);
            _mailFrom = GetConfigurationStringValue(info, MailFromName, null);
        }
        protected override Dictionary<string,object>  WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(EnabledName, _enabled);
            result.Add(AdminEmailName, _adminEmail);
            result.Add(RequireUniqueEmailName, _requireUniqueEmail);
            result.Add(MailDefinitionName, _mailDefinition);
            result.Add(IsBodyHtmlName, _isBodyHtml);
            result.Add(MailSubjectName, _mailSubject);
            result.Add(MailPriorityName, _mailPriority);
            result.Add(MailFromName, _mailFrom);
            return result;
        }
        protected override void SetDefaults()
        {
            _enabled = null;
            _adminEmail = null;
            _requireUniqueEmail = null;
            _mailDefinition = string.Empty;
            _isBodyHtml = null;
            _mailSubject = string.Empty;
            _mailFrom = string.Empty;
            _mailPriority = MailPriority.Normal;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            WriteElement(writer, this._enabled, EnabledName);
            WriteElement(writer, this._adminEmail, AdminEmailName);
            WriteElement(writer, this._requireUniqueEmail, RequireUniqueEmailName);
            WriteElement(writer, this._mailDefinition, MailDefinitionName);
            WriteElement(writer, this._isBodyHtml, IsBodyHtmlName);
            WriteElement(writer, this._mailSubject, MailSubjectName);
            WriteElement(writer, this.MailPriority.ToString(), MailPriorityName);
            WriteElement(writer, this._mailFrom, MailFromName);
        }
    }
}