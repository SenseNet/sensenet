using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Collections;
using System.Linq;
using System.Globalization;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("Choice")]
    [DataSlot(0, RepositoryDataType.String, typeof(string), typeof(int), typeof(Enum))]
    [DefaultFieldSetting(typeof(ChoiceFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.DropDown")]
    public class ChoiceField : Field
    {
        public const string EXTRAVALUEPREFIX = "~other.";
        protected static readonly char[] SplitChars = new char[] { ',', ';' };

        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            object data = GetData();

            var stringData = data as string;
            if (stringData != null)
            {
                writer.WriteString(stringData);
                return;
            }

            var listData = data as List<string>;
            if (listData != null)
            {
                string output = String.Join(";", listData.ToArray());
                writer.WriteString(output);
                return;
            }

            var enumerableData = data as IEnumerable;
            if (enumerableData != null)
            {
                var sb = new StringBuilder();
                foreach (var item in enumerableData)
                {
                    if (sb.Length != 0)
                        sb.Append(";");
                    sb.Append(Convert.ToString(item, CultureInfo.InvariantCulture));
                }
                writer.WriteString(sb.ToString());
                return;
            }

            if (data != null && (data is Enum || data is int))
            {
                writer.WriteString(((int)data).ToString());
                return;
            }

            throw ExportNotSupportedException(data);
        }
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            ExportData(writer, null);
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            string value = fieldNode.InnerXml;
            if (value.Trim() == String.Empty)
                return;
            var values = new List<string>(value.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries));
            SetData(values);
        }

        protected override string GetXmlData()
        {
            object data = GetData();

            var stringData = data as string;
            if (stringData != null)
                return stringData;

            var listData = data as List<string>;
            if (listData != null)
                return String.Join(";", listData.ToArray());

            var enumerableData = data as IEnumerable;
            if (enumerableData != null)
            {
                var sb = new StringBuilder();
                foreach (var item in enumerableData)
                {
                    if (sb.Length != 0)
                        sb.Append(";");
                    sb.Append(Convert.ToString(item, CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }

            if (data != null && (data is Enum || data is int))
            {
                return ((int)data).ToString();
            }

            throw ExportNotSupportedException(data);
        }

        protected override object ConvertTo(object[] handlerValues)
        {
            Type typeOfSlot = base.GetHandlerSlot(0);
            if (handlerValues[0] == null)
                return new List<string>();
            // return new object[] { null }; http://iso/bugtracking/mod.aspx?bugid=4143
            string valueAsString;
            if(typeOfSlot == typeof(Enum))
                valueAsString = ((int)handlerValues[0]).ToString();
            else
                valueAsString = handlerValues[0].ToString();

            var extraValueIndex = valueAsString.IndexOf(EXTRAVALUEPREFIX);
            string extravalue = null;
            if (extraValueIndex >= 0)
            {
                extravalue = valueAsString.Substring(extraValueIndex);
                valueAsString = valueAsString.Substring(0, extraValueIndex);
            }
            List<string> valueAsList = valueAsString.Length == 0
                ? new List<string>()
                : new List<string>(valueAsString.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries));
            if (extravalue != null)
                valueAsList.Add(extravalue);

            return valueAsList;
        }
        protected override object[] ConvertFrom(object value)
        {
            return new object[] { ConvertFromControlInner(value) };
        }
        private object ConvertFromControlInner(object value)
        {
            Type type = base.GetHandlerSlot(0);
            string[] arrayValue = ConvertToStringList(value).ToArray();
            switch (type.FullName)
            {
                case "System.String":
                    return String.Join(";", arrayValue);
                case "System.Enum":
                case "System.Int32":
                    if (arrayValue.Length == 0)
                        return 0;
                    return Convert.ToInt32(arrayValue[0]);
                default:
                    throw new NotSupportedException(String.Concat(
                        "ChoiceField not supports this type: ", value.GetType().FullName
                        , ". FieldName: ", this.Name
                        , ", ContentType: ", this.Content.ContentHandler.NodeType.Name
                        , ", ContentName: ", this.Content.ContentHandler.Name
                        ));
            }
        }

        public static List<string> ConvertToStringList(object value)
        {
            List<string> list = value as List<string>;
            if (list != null)
                return list;

            list = new List<string>();
            if (value == null)
                return list;

            string stringValue = value as string;
            if (stringValue != null)
            {
                list.Add(stringValue);
                return list;
            }

            IEnumerable enumerableValue = value as IEnumerable;
            if (enumerableValue == null)
            {
                list.Add(value.ToString());
                return list;
            }

            foreach (object item in enumerableValue)
                list.Add(item.ToString());
            return list;

        }

        protected override bool ParseValue(string value)
        {
            var list = value.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries).ToList();
            this.SetData(list);
            return true;
        }

        public override bool HasValue()
        {
            return ((List<string>)OriginalValue).Count > 0;
        }


        protected TEnum ConvertFromInputToEnumValue<TEnum, TField>(object value) where TEnum : struct, System.Enum
        {
            string stringValue = null;
            int intValue = 0;

            if (value is int @int)
                intValue = @int;
            else if (value is int[] intArray && intArray.Length > 0)
                intValue = intArray[0];
            else if (value is List<int> intList && intList.Count > 0)
                intValue = intList[0];
            else if (value is string @string)
                stringValue = @string;
            else if (value is string[] stringArray && stringArray.Length > 0)
                stringValue = stringArray[0];
            else if (value is List<string> stringList && stringList.Count > 0)
                stringValue = stringList[0];
            else
                throw GetEnumParsingError<TField, TEnum>();

            if (stringValue != null && !int.TryParse(stringValue, out intValue))
            {
                if (Enum.TryParse<TEnum>(stringValue, true, out var parsed))
                    return (TEnum)parsed;
                throw GetEnumParsingError<TField, TEnum>();
            }

            if (!Enum.IsDefined(typeof(TEnum), intValue))
                throw GetEnumParsingError<TField, TEnum>();

            foreach (var item in Enum.GetValues(typeof(TEnum)))
                if (intValue == (int)item)
                    return (TEnum)item;
            throw GetEnumParsingError<TField, TEnum>();
        }
        private Exception GetEnumParsingError<TField, TEnum>()
        {
            return new ArgumentException($"Cannot parse a {typeof(TField).Name}: invalid {typeof(TEnum).Name} value.");
        }

    }
}
