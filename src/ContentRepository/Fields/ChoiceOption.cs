using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.ContentRepository.Fields
{
    [Serializable]
	public class ChoiceOption
	{
		private string _value;
		private string _text;
		private bool _enabled;
		private bool _selected;

		public string Value
		{
			get { return _value; }
		}
		public string Text
		{
			get
			{
                string className, optionResourceKey;
                return SenseNetResourceManager.ParseResourceKey(_text ?? string.Empty, out className, out optionResourceKey) 
                    ? SenseNetResourceManager.Current.GetString(className, optionResourceKey) 
                    : _text;
			}
			set { _text = value; }
		}
        public string StoredText
        {
            get { return _text; }
        }
        public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}
		public bool Selected
		{
			get { return _selected; }
			set { _selected = value; }
		}

		public ChoiceOption(string key, string text) : this(key, text, true) { }
		public ChoiceOption(string key, string text, bool enabled) : this(key, text, enabled, false) { }
		public ChoiceOption(string key, string text, bool enabled, bool selected)
		{
			_value = key;
			_text = text;
			_enabled = enabled;
			_selected = selected;
		}

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Option");
            writer.WriteAttributeString("value", this._value);

            if (this._selected)
                writer.WriteAttributeString("selected", "true");
            if (!this._enabled)
                writer.WriteAttributeString("enabled", "false");

            writer.WriteString(this._text);

            writer.WriteEndElement();
            writer.Flush();
        }
	}
}