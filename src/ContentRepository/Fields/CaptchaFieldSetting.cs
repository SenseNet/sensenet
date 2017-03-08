using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.XPath;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Fields
{
    public class CaptchaFieldSetting : FieldSetting
    {
        #region Enums
        public enum Layout
        {
            Horizontal,
            Vertical
        }
        public enum CacheType
        {
            HttpRuntime,
            Session
        }
        #endregion

        protected override void WriteConfiguration(XmlWriter writer)
        {
        }

        public override FieldValidationResult ValidateData(object value, Field field)
        {
            if (value == null)
            {
                return base.ValidateData(value, field);
            }
            var strValue = value as string;
            
            if(strValue!=null && strValue==string.Empty)
                return FieldValidationResult.Successful;

            if (!(bool)value)
            {
                return new FieldValidationResult("Invalid captcha text");
            }
            
            return FieldValidationResult.Successful;
        }
    }
}
