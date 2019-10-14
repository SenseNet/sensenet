using System.Collections.Generic;
using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class Property : NamedItem
    {
        public string Type;
        public string FieldType;
        public bool Nullable;
        public List<KeyValue> Attributes;                // e.g.: "m:IsDefaultEntityContainer", "true"
        public int? MaxLength;                           // binary, stream or string
        public int? FixedLength;                         // binary, stream or string
        public int? Precision;                           // temporal or decimal
        public int? Scale;                               // decimal
        public bool? Unicode;                            // string
        public string DefaultValue;                      // string

        public override void WriteXml(TextWriter writer)
        {
            // <Property Name="Price" Type="Edm.Decimal" Nullable="false"/>
            writer.Write("        <Property");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "Type", Type);
            WriteAttribute(writer, "FieldType", FieldType);
            WriteAttribute(writer, "Nullable", Nullable.ToString().ToLower());
            if (MaxLength.HasValue)
                WriteAttribute(writer, "MaxLength", MaxLength.Value.ToString());
            if (FixedLength.HasValue)
                WriteAttribute(writer, "FixedLength", FixedLength.Value.ToString());
            if (Precision.HasValue)
                WriteAttribute(writer, "Precision", Precision.Value.ToString());
            if (Scale.HasValue)
                WriteAttribute(writer, "Scale", Scale.Value.ToString());
            if (Unicode.HasValue)
                WriteAttribute(writer, "Unicode", Unicode.Value.ToString().ToLower());
            if (Attributes != null)
            {
                foreach (var attribute in Attributes)
                {
                    WriteAttribute(writer, attribute.Key, attribute.Value);
                }
            }

            writer.WriteLine("/>");
        }
    }
}
