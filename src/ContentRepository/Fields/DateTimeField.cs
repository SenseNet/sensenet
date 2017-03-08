using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using SenseNet.ContentRepository.Storage;

using  SenseNet.ContentRepository.Schema;
using System.Xml;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("DateTime")]
    [DataSlot(0, RepositoryDataType.DateTime, typeof(DateTime))]
    [DefaultFieldSetting(typeof(DateTimeFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.DatePicker")]
    public class DateTimeField : Field
    {
        private static CultureInfo _defaultUICulture;
        public static CultureInfo DefaultUICulture
        {
            get
            {
                // The value of this property needs to remain en-US. This is used by UI-facing
                // code that must render date values in a well-known format for client-side
                // plugins to understand the value. The actual culture-specific formatting
                // must be done by those client-side plugins.

                return _defaultUICulture ?? (_defaultUICulture = CultureInfo.GetCultureInfo("en-US"));
            }
        }

        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            writer.WriteString(GetXmlData());
        }
        protected override void WriteXmlData(XmlWriter writer)
        {
            ExportData(writer, null);
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            if (String.IsNullOrEmpty(fieldNode.InnerXml))
            {
                this.SetData(ActiveSchema.DateTimeMinValue);
                return;
            }
            DateTime value = Convert.ToDateTime(fieldNode.InnerXml);
            this.SetData(value < ActiveSchema.DateTimeMinValue ? ActiveSchema.DateTimeMinValue : value);
        }

        public override void SetData(object value)
        {
            // This conversion makes sure that the date we handle is in UTC format (e.g. if 
            // the developer provides DateTime.Now, which is in local time by default).
            if (value is DateTime)
                value = RepositoryTools.ConvertToUtcDateTime((DateTime)value);

            base.SetData(value);
        }

        protected override string GetXmlData()
        {
            return XmlConvert.ToString((DateTime)GetData(), XmlDateTimeSerializationMode.Unspecified);
        }

        protected override bool ParseValue(string value)
        {
            DateTime dateTimeValue;
            if (!DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTimeValue))
                if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                    if (!DateTime.TryParse(value, out dateTimeValue))
                        return false;
            this.SetData(dateTimeValue);
            return true;
        }

        public override string GetFormattedValue()
        {
            var data = GetData();

            // We format the datetime value using the built-in default culture, which is 'en-US'. The reason
            // behind this is that on the UI we use a client-side technique to display a culture-specific
            // value, and the client-side plugin needs the source value to be formatted using a well-known
            // format.
            // If you want the date value to be formatted using a different culture (e.g. the current culture)
            // please use the original value of the field and format it manually.

            return (data is DateTime) 
                ? ((DateTime)data).ToString(DateTimeField.DefaultUICulture)
                : base.GetFormattedValue();
        }
    }
}