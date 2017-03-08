using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.Schema;
using System.Collections.Specialized;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
    public class CurrencyFieldSetting : NumberFieldSetting
    {
        public const string FormatName = "Format";

        private string _format;
        public string Format
        {
            get
            {
                return _format ?? (this.ParentFieldSetting == null ? null :
                    ((CurrencyFieldSetting)this.ParentFieldSetting).Format);
            }
            set
            {
                if (!_mutable)
                    throw new InvalidOperationException("Setting Format is not allowed within readonly instance.");
                _format = value;
            }
        }

        [Obsolete("Please use RegionInfos instead")]
        public static NameValueCollection CurrencyTypes
        {
            get
            {
                return System.Configuration.ConfigurationManager.GetSection("currencyValues") as NameValueCollection;
            }
        }

        private static Dictionary<string, RegionInfo> _regionInfos;
        private static object _regionLock = new object();

        /// <summary>
        /// Provides region information for specific cultures (e.g. 'en-US')
        /// </summary>
        protected static IDictionary<string, RegionInfo> RegionInfos
        {
            get
            {
                if (_regionInfos == null)
                {
                    lock (_regionLock)
                    {
                        if (_regionInfos == null)
                        {
                            var regions = new Dictionary<string, RegionInfo>();
                            var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
                            
                            foreach (var cultureInfo in cultures)
                            {
                                if (cultureInfo.IsNeutralCulture || cultureInfo.LCID == CultureInfo.InvariantCulture.LCID)
                                    continue;

                                try
                                {
                                    if (!regions.ContainsKey(cultureInfo.Name))
                                    {
                                        regions.Add(cultureInfo.Name, new RegionInfo(cultureInfo.Name));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SnLog.WriteWarning(ex);
                                }
                            }

                            _regionInfos = regions;
                        }
                    }
                }

                return _regionInfos;
            }
        }

        protected override void ParseConfiguration(XPathNavigator configurationElement, IXmlNamespaceResolver xmlNamespaceResolver, ContentType contentType)
        {
            base.ParseConfiguration(configurationElement, xmlNamespaceResolver, contentType);

            foreach (XPathNavigator node in configurationElement.SelectChildren(XPathNodeType.Element))
            {
                switch (node.LocalName)
                {
                    case FormatName:
                        if (!string.IsNullOrEmpty(node.InnerXml))
                            _format = node.InnerXml;
                        break;
                }
            }
        }
        protected override void ParseConfiguration(Dictionary<string, object> info)
        {
            base.ParseConfiguration(info);
            _format = GetConfigurationStringValue(info, FormatName, null);
        }
        protected override Dictionary<string, object> WriteConfiguration()
        {
            var result = base.WriteConfiguration();
            result.Add(FormatName, _format);
            return result;
        }

        protected override void WriteConfiguration(XmlWriter writer)
        {
            base.WriteConfiguration(writer);

            WriteElement(writer, this._format, FormatName);
        }

        protected override void CopyPropertiesFrom(FieldSetting source)
        {
            base.CopyPropertiesFrom(source);

            var currentSource = (CurrencyFieldSetting)source;

            Format = currentSource.Format;
        }

        public override IDictionary<string, FieldMetadata> GetFieldMetadata()
        {
            var fmd = base.GetFieldMetadata();

            var fieldSetting = fmd[ShowAsPercentageName].FieldSetting;
            fieldSetting.VisibleBrowse = FieldVisibility.Hide;
            fieldSetting.VisibleEdit = FieldVisibility.Hide;
            fieldSetting.VisibleNew = FieldVisibility.Hide;

            fmd[DigitsName].FieldSetting.DefaultValue = "0";

            var minvalFs = (NumberFieldSetting) fmd[MinValueName].FieldSetting;
            minvalFs.DefaultValue = "0";

            var fs = new ChoiceFieldSetting
                         {
                             Name = FormatName,
                             DisplayName = GetTitleString(FormatName),
                             Description = GetDescString(FormatName),
                             FieldClassName = typeof(ChoiceField).FullName,
                             AllowMultiple = false,
                             AllowExtraValue = false,
                             Options = GetCurrencyOptions(),
                             DisplayChoice = DisplayChoice.DropDown,
                             DefaultValue = @"[Script:jScript] System.Globalization.CultureInfo.CurrentUICulture.IsNeutralCulture ? System.Globalization.CultureInfo.CreateSpecificCulture(System.Globalization.CultureInfo.CurrentUICulture.Name).Name : System.Globalization.CultureInfo.CurrentUICulture.Name; [/Script]"
                         };

            fmd.Add(FormatName, new FieldMetadata
            {
                FieldName = FormatName,
                CanRead = true,
                CanWrite = true,
                FieldSetting = fs
            });

            return fmd;
        }

        public override object GetProperty(string name, out bool found)
        {
            var val = base.GetProperty(name, out found);

            if (!found)
            {
                switch (name)
                {
                    case FormatName:
                        found = true;
                        if (!string.IsNullOrEmpty(_format))
                            val = _format;
                        break;
                }
            }

            return found ? val : null;
        }

        public override bool SetProperty(string name, object value)
        {
            var found = base.SetProperty(name, value);

            if (!found)
            {
                switch (name)
                {
                    case FormatName:
                        found = true;
                        _format = value as string;
                        break;
                }
            }

            return found;
        }

        // ============================================================================= Helper methods

        public static string GetCurrencySymbol(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return string.Empty;
            
            return RegionInfos.ContainsKey(cultureName)
                ? RegionInfos[cultureName].CurrencySymbol
                : string.Empty;
        }

        private static string GetCurrencyText(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return string.Empty;

            if (RegionInfos.ContainsKey(cultureName))
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                var reg = RegionInfos[cultureName];

                return string.Format("{0} ({1}), {2}", reg.ISOCurrencySymbol, reg.CurrencySymbol, culture.DisplayName);
            }

            return cultureName;
        }

        private static List<ChoiceOption> GetCurrencyOptions()
        {
            var coList = new List<ChoiceOption>();
 
            coList.AddRange(RegionInfos.Keys.Select(cName => new ChoiceOption(cName, GetCurrencyText(cName))).OrderBy(co => co.Text));

            return coList;
        }
    }
}
