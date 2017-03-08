using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using System.Linq;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Version")]
	[DataSlot(0, RepositoryDataType.String, typeof(VersionNumber), typeof(string))]
	[DefaultFieldSetting(typeof(NullFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.Version")]
	public class VersionField : Field
	{
		protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
		{
            var text = fieldNode.InnerText.Trim();
            var segs = text.Split('.');
            if (segs.Length != 2)
                throw new InvalidOperationException("Invalid version format. Expected: Vxx.yy");

            text += segs[1].Any(c => c != '0') ? ".D" : ".A";

            this.Content.ContentHandler.Version = VersionNumber.Parse(text);
            this.Content.ImportingExplicitVersion = true;
        }
        private bool IsMajor(string versionTextLastSegment)
        {
            return !versionTextLastSegment.Any(c => c != '0');
        }

		protected override object ConvertTo(object[] handlerValues)
		{
			if (handlerValues[0] == null)
				return null;
			if (handlerValues[0] is VersionNumber)
				return handlerValues[0];
			return VersionNumber.Parse(handlerValues[0].ToString());
		}
		protected override object[] ConvertFrom(object value)
		{
			return new object[] { ConvertFromControlInner(value) };
		}
		private object ConvertFromControlInner(object value)
		{
			if (value == null)
				return null;

			Type propertyType = this.GetHandlerSlot(0);

			if (propertyType == typeof(string))
				return value.ToString();
			else if (propertyType == typeof(VersionNumber))
				return VersionNumber.Parse(value.ToString());

			return null;
		}

        protected override string GetXmlData()
        {
            var data = GetData();
            if (data == null)
                return null;
            switch (FieldSetting.HandlerSlotIndices[0])
            {
                case 0:
                    return ((VersionNumber)data).ToString();
                case 1:
                    return (string)data;
            }
            throw new SnNotSupportedException();
        }
	}
}