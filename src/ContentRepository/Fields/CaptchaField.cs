using System.Xml;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("Captcha")]
    [DataSlot(0, RepositoryDataType.String, typeof(string), typeof(bool))]
    [DefaultFieldSetting(typeof(CaptchaFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.Captcha.CaptchaControl")]
        
    public class CaptchaField : ShortTextField
    {
        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
        }
        protected override bool HasExportData
        {
            get
            {
                return false;
            }
        }
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            ExportData(writer, null);
        }

        protected override object[] ConvertFrom(object value)
        {
            return new object[]{null};
        }

        protected override object ConvertTo(object[] handlerValues)
        {
            return null;
        }


    }
    
}
