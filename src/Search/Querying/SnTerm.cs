using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represents a name-value pair in the querying and indexing.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}")]
    public class SnTerm : IndexValue
    {
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.String value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.String value</param>
        public SnTerm(string name, string value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named array of System.String value.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">Array of System.String value</param>
        public SnTerm(string name, string[] value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Boolean value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Boolean value</param>
        public SnTerm(string name, bool value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Int32 value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int32 value</param>
        public SnTerm(string name, int value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Int32 value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int32 value</param>
        public SnTerm(string name, int[] value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Int64 value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int64 value</param>
        public SnTerm(string name, long value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Single value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Single value</param>
        public SnTerm(string name, float value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Double value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Double value</param>
        public SnTerm(string name, double value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.DateTime value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.DateTime value</param>
        public SnTerm(string name, DateTime value) : base(value) { Name = name; }

        /// <summary>
        /// Gets the name of the term.
        /// </summary>
        public string Name { get; }

        public override string ToString()
        {
            return $"{Name}:{base.ToString()}";
        }


        internal static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new SnTermJsonConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented
        };

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, SerializerSettings);
        }

        public static SnTerm Deserialize(string serializedSnTerm)
        {
            try
            {
                var deserialized = JsonSerializer.Create(IndexField.FormattedSerializerSettings).Deserialize(
                    new JsonTextReader(new StringReader(serializedSnTerm)));
                var field = (JObject)deserialized;
                return Deserialize(field);
            }
            catch (Exception e)
            {
                throw new SerializationException("Cannot deserialize the SnTerm: " + e.Message, e);
            }
        }

        public static SnTerm Deserialize(JObject snTermJObject)
        {
            try
            {
                string name = null;
                var type = IndexValueType.String;
                object value = null;

                foreach (var item in snTermJObject)
                {
                    switch (item.Key)
                    {
                        case "Name":
                            name = ((JValue)item.Value).ToString();
                            break;
                        case "Type":
                            type = (IndexValueType)Enum.Parse(typeof(IndexValueType), ((JValue)item.Value).ToString(),
                                true);
                            break;
                        case "Value":
                            if (item.Value is JValue jvalue)
                                value = jvalue;
                            else if (item.Value is JArray jarray)
                                value = jarray.Select(x => x.ToString()).ToArray();
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }

                SnTerm term;
                switch (type)
                {
                    case IndexValueType.String:
                        term = new SnTerm(name, value?.ToString());
                        break;
                    case IndexValueType.StringArray:
                        term = new SnTerm(name, (string[])value);
                        break;
                    case IndexValueType.Bool:
                        term = new SnTerm(name, Convert.ToBoolean(value));
                        break;
                    case IndexValueType.Int:
                        term = new SnTerm(name, Convert.ToInt32(value));
                        break;
                    case IndexValueType.IntArray:
                        var stringArray = (string[])value ?? Array.Empty<string>();
                        var intValues = stringArray.Select(x => Convert.ToInt32(x)).ToArray();
                        term = new SnTerm(name, intValues);
                        break;
                    case IndexValueType.Long:
                        term = new SnTerm(name, Convert.ToInt64(value));
                        break;
                    case IndexValueType.Float:
                        term = new SnTerm(name, Convert.ToSingle(value));
                        break;
                    case IndexValueType.Double:
                        term = new SnTerm(name, Convert.ToDouble(value));
                        break;
                    case IndexValueType.DateTime:
                        term = new SnTerm(name, DateTime.Parse(value.ToString()));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return term;
            }
            catch (Exception e)
            {
                throw new SerializationException("Cannot deserialize the SnTerm: " + e.Message, e);
            }
        }

    }
}
