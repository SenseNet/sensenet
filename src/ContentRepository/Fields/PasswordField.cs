using System;
using System.Collections.Generic;
using System.Text;

using  SenseNet.ContentRepository.Schema;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Password")]
	[DataSlot(0, RepositoryDataType.String, typeof(string))]
	[DefaultFieldSetting(typeof(PasswordFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.Password")]
	public class PasswordField : Field
	{
		public class PasswordData
		{
			public string Text { get; set; }
			public string Hash { get; set; }
		}

        public class OldPasswordData
        {
            public DateTime ModificationDate { get; set; }
            public string Hash { get; set; }
        }

        public string EncodePassword(string passwordInClearText)
        {
            return EncodePassword(passwordInClearText, this.Content.ContentHandler);
        }
        public static string EncodePassword(string passwordInClearText, IPasswordSaltProvider saltProvider)
        {
            return PasswordHashProvider.EncodePassword(passwordInClearText, saltProvider);
        }


        public sealed override object GetData(bool localized = true)
        {
            var wm = RepositoryEnvironment.WorkingMode;
            if(wm.Importing || wm.Exporting)
                return base.GetData(localized);
            return null;
        }

		protected override bool HasExportData
        {
            get
            {
                var pw = OriginalValue as PasswordData;
                return pw != null && !string.IsNullOrEmpty(pw.Hash);
            }
        }
		protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
		{
			var data = GetData() as PasswordData;
			if(data == null)
				return;
			if(String.IsNullOrEmpty(data.Hash))
				return;
			writer.WriteElementString("Hash", data.Hash);
		}
		protected override void ImportData(XmlNode fieldNode, ImportContext context)
		{
			var data = new PasswordData();
			foreach (XmlNode dataNode in fieldNode.SelectNodes("*"))
			{
				switch (dataNode.LocalName)
				{
					case "Text": data.Text = dataNode.InnerText; break;
					case "Hash": data.Hash = dataNode.InnerText; break;
				}
			}
			this.SetData(data);
		}

		protected override object[] ConvertFrom(object value)
		{
            var passwordData = value as PasswordData;
            if (passwordData != null)
                return new object[] { EncodeTransferData(passwordData) };
            var stringData = value as string;
            return new object[] { EncodeTransferData(stringData) };
        }
		protected override object ConvertTo(object[] handlerValues)
		{
			return new PasswordData { Hash = (string)handlerValues[0] };
		}
		private string EncodeTransferData(PasswordData data)
		{
            var user = this.Content.ContentHandler as User;
            if (user != null)
                user.Password = data.Text;

			if (data.Text != null)
				data.Hash = EncodePassword(data.Text);
			return data.Hash;
		}
        private string EncodeTransferData(string text)
        {
            var user = this.Content.ContentHandler as User;
            if (user != null)
                user.Password = text;

            string hash = null;
            if (text != null)
                hash = EncodePassword(text);
            return hash;
        }

        protected override void WriteXmlData(XmlWriter writer)
        {
            ExportData(writer, null);
        }
    }
}