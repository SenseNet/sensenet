using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
//using SenseNet.Portal.Virtualization;

namespace SenseNet.ContentRepository.Fields
{
    public class PageBreakFieldSetting : FieldSetting
    {
        public const string RuleName = "Rule";

        private List<SurveyRule> _surveyRules;
        public List<SurveyRule> SurveyRules
        {
            get { return _surveyRules; }
            set { _surveyRules = value; }
        }

        private string _selectedQuestion;
        public string SelectedQuestion
        {
            get { return _selectedQuestion; }
            set { _selectedQuestion = value; }
        }


        private string _rule;
        public string Rule
        {
            get
            {
                if (_rule != null)
                    return _rule;
                if (this.ParentFieldSetting == null)
                    return null;
                return ((PageBreakFieldSetting)this.ParentFieldSetting).Rule;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Rule is not allowed within readonly instance.");
                _rule = value.Replace("&amp;", "&");
            }
        }

        protected override void WriteConfiguration(System.Xml.XmlWriter writer)
        {
            WriteElement(writer, this._rule, RuleName);
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case RuleName:
                        _rule = node.InnerXml;
                        ParseRules(node);
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _rule = GetConfigurationStringValue(info, RuleName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(RuleName, _rule);
            return result;
        }

        private void ParseRules(XPathNavigator node)
        {
            _surveyRules = new List<SurveyRule>();
            foreach (XPathNavigator optionElement in node.SelectChildren(XPathNodeType.Element))
            {
                var s = optionElement.Name;
            }
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);
            var etSource = (PageBreakFieldSetting)source;
            Rule = etSource.Rule;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            fmd.Remove("DefaultValue");
            fmd.Remove("Compulsory");

            fmd.Add(RuleName, new FieldMetadata
              {
                  FieldName = RuleName,
                  CanRead = true,
                  CanWrite = true,
                  FieldSetting = new LongTextFieldSetting()
                     {
                         Name = RuleName,
                         DisplayName = GetTitleString(RuleName),
                         Description = GetDescString(RuleName),
                         FieldClassName = typeof(LongTextField).FullName,
                         ControlHint = "sn:SurveyRuleEditor"
                     }
              });

            return fmd;
        }
    }
}
