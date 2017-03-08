using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Xml.XPath;
using System.Xml;
using System.Web;

namespace SenseNet.ContentRepository.Fields
{
    public class RatingFieldSetting : ShortTextFieldSetting
    {
        public const string RangeName = "Range";
        public const string SplitName = "Split";
        private const int DefaultRange = 5;
        private const int DefaultSplit = 1;

        private int _range;
        private int _split;

        public int Range
        {
            get
            {
                if (_range > 0)
                    return _range;
                if (this.ParentFieldSetting == null)
                    return 0;
                return ((RatingFieldSetting)this.ParentFieldSetting).Range;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Range is not allowed within readonly instance.");
                _range = value;
            }
        }

        public int Split
        {
            get
            {
                if (_split > 0)
                    return _split;
                if (this.ParentFieldSetting == null)
                    return 0;
                return ((RatingFieldSetting)this.ParentFieldSetting).Split;
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Range is not allowed within readonly instance.");
                _split = value;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator element in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (element.LocalName)
                {
                    case RangeName:
                        int range;
                        _range = Int32.TryParse(element.InnerXml, out range) ? range : DefaultRange;
                        break;
                    case SplitName:
                        int split;
                        _split = Int32.TryParse(element.InnerXml, out split) ? split : DefaultSplit;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _range = GetConfigurationValue<int>(info, RangeName, DefaultRange);
            _split = GetConfigurationValue<int>(info, SplitName, DefaultSplit);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(RangeName, _range);
            result.Add(SplitName, _split);
            return result;
        }

        protected override void SetDefaults()
        {
            _range = DefaultRange;
            _split = DefaultSplit;
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            var vote = value as VoteData;
            if (vote == null)
                return new FieldValidationResult("Invalid cast");

            if (Compulsory.HasValue && Compulsory.Value && !vote.SelectedValue.HasValue)
                throw new NotSupportedException("Compulsory");

            if (vote.SelectedValue.HasValue)
            {
                if (vote.SelectedValue.Value > Range*Split)
                    return new FieldValidationResult("Vote out of range");
            }
            return FieldValidationResult.Successful;
        }
    }
}