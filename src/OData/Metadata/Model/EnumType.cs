using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SenseNet.ContentRepository.Fields;

namespace SenseNet.OData.Metadata.Model
{
    public class EnumType : NamedItem
    {
        [NonSerialized]
        public Type UnderlyingEnumType;
        [NonSerialized]
        public ChoiceFieldSetting UnderlyingFieldSetting;
        public IEnumerable<EnumOption> Options => GetOptions();

        public override void WriteXml(TextWriter writer)
        {
            if (UnderlyingEnumType != null)
                WriteEnumXml(writer);
            else
                WriteChoiceXml(writer);
        }
        public void WriteEnumXml(TextWriter writer)
        {
            var isFlags = UnderlyingEnumType.GetCustomAttributes(typeof(FlagsAttribute), true).Length > 0;
            var names = Enum.GetNames(UnderlyingEnumType);
            var values = Enum.GetValues(UnderlyingEnumType);

            writer.Write("      <EnumType");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "UnderlyingType", MetaGenerator.GetPrimitivePropertyType(Enum.GetUnderlyingType(UnderlyingEnumType)));
            WriteAttribute(writer, "IsFlags", isFlags.ToString().ToLower());
            writer.WriteLine(">");

            for (int i = 0; i < names.Length; i++)
            {
                writer.Write("        <Member");
                WriteAttribute(writer, "Name", names[i]);
                WriteAttribute(writer, "Value", Convert.ChangeType(values.GetValue(i), Enum.GetUnderlyingType(UnderlyingEnumType)).ToString());
                writer.WriteLine("/>");
            }

            writer.WriteLine("      </EnumType>");
        }
        public void WriteChoiceXml(TextWriter writer)
        {
            writer.Write("      <EnumType");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "UnderlyingType", MetaGenerator.GetPrimitivePropertyType(typeof(string)));
            WriteAttribute(writer, "IsFlags", UnderlyingFieldSetting.AllowMultiple.ToString().ToLower());
            writer.WriteLine(">");
            var options = UnderlyingFieldSetting.Options;
            if (options != null)
            {
                foreach (var option in options)
                {
                    writer.Write("        <Member");
                    WriteAttribute(writer, "Name", option.Text);
                    WriteAttribute(writer, "Value", option.Value);
                    writer.WriteLine("/>");
                }
            }
            writer.WriteLine("      </EnumType>");
        }

        private IEnumerable<EnumOption> GetOptions()
        {
            if (UnderlyingFieldSetting == null)
                return GetEnumOptions();
            var options = UnderlyingFieldSetting.Options;
            return options?.Select(x => new EnumOption { Name = x.Text, Value = x.Value }).ToArray();
        }
        private IEnumerable<EnumOption> GetEnumOptions()
        {
            if (UnderlyingEnumType == null)
                return null;
            var names = Enum.GetNames(UnderlyingEnumType);
            var values = Enum.GetValues(UnderlyingEnumType);
            var result = new EnumOption[names.Length];
            for (int i = 0; i < names.Length; i++)
                result[i] = new EnumOption { Name = names[i], Value = Convert.ChangeType(values.GetValue(i), Enum.GetUnderlyingType(UnderlyingEnumType)).ToString() };
            return result;
        }
    }
}
