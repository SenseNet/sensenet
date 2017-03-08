using System;
using System.Globalization;
using SenseNet.ContentRepository.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("Currency")]
    [DataSlot(0, RepositoryDataType.Currency, typeof(decimal), typeof(byte), typeof(Int16), typeof(Int32), typeof(Int64),
            typeof(Single), typeof(Double), typeof(SByte), typeof(UInt16), typeof(UInt32), typeof(UInt64))]
    [DefaultFieldSetting(typeof(CurrencyFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.Currency")]
    public class CurrencyField : NumberField
    {
        public override string GetFormattedValue()
        {
            var val = Convert.ToDecimal(this.GetData());
            var fs = this.FieldSetting as CurrencyFieldSetting;
            var digits = Math.Min(fs == null || !fs.Digits.HasValue ? 0 : fs.Digits.Value, 29);

            try
            {
                if (fs != null && !string.IsNullOrEmpty(fs.Format))
                {
                    var cultForField = CultureInfo.GetCultureInfo(fs.Format);
                    var cultCurrent = (CultureInfo)CultureInfo.CurrentUICulture.Clone();

                    cultCurrent.NumberFormat.CurrencySymbol = cultForField.NumberFormat.CurrencySymbol; 
                    cultCurrent.NumberFormat.CurrencyPositivePattern = cultForField.NumberFormat.CurrencyPositivePattern;
                    cultCurrent.NumberFormat.CurrencyGroupSeparator = cultForField.NumberFormat.CurrencyGroupSeparator;

                    return val.ToString("C" + digits, cultCurrent);
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning(ex);
            }

            return base.GetFormattedValue();
        }
    }
}
