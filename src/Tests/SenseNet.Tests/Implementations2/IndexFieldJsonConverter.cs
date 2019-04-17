using System;
using Newtonsoft.Json;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    internal class IndexFieldJsonConverter : JsonConverter<IndexField>
    {
        public override void WriteJson(JsonWriter writer, IndexField value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(value.Name);
            writer.WritePropertyName("Type");
            writer.WriteValue(value.Type.ToString());
            if (value.Mode != IndexingMode.Default)
            {
                writer.WritePropertyName("Mode");
                writer.WriteValue(value.Mode.ToString());
            }
            if (value.Store != IndexStoringMode.Default)
            {
                writer.WritePropertyName("Store");
                writer.WriteValue(value.Store.ToString());
            }
            if (value.TermVector != IndexTermVector.Default)
            {
                writer.WritePropertyName("TermVector");
                writer.WriteValue(value.TermVector.ToString());
            }
            writer.WritePropertyName("Value");
            switch (value.Type)
            {
                case IndexValueType.String:
                    writer.WriteValue(value.StringValue);
                    break;
                case IndexValueType.Bool:
                    writer.WriteValue(value.BooleanValue);
                    break;
                case IndexValueType.Int:
                    writer.WriteValue(value.IntegerValue);
                    break;
                case IndexValueType.Long:
                    writer.WriteValue(value.LongValue);
                    break;
                case IndexValueType.Float:
                    writer.WriteValue(value.SingleValue);
                    break;
                case IndexValueType.Double:
                    writer.WriteValue(value.DoubleValue);
                    break;
                case IndexValueType.DateTime:
                    writer.WriteValue(value.DateTimeValue);
                    break;
                case IndexValueType.StringArray:
                    writer.WriteStartArray();
                    writer.WriteRaw("\"" + string.Join("\",\"", value.StringArrayValue) + "\"");
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            writer.WriteEndObject();
        }

        public override IndexField ReadJson(JsonReader reader, Type objectType, IndexField existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
