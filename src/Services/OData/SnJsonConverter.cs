using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage;
using System.IO;
using SenseNet.Tools;

namespace SenseNet.Portal.OData
{
    /// <summary>
    /// Provides helper methods for serializing OData response objects to JSON format.
    /// </summary>
    public static class SnJsonConverter
    {
        private static List<JsonConverter> _jsonConverters;
        internal static List<JsonConverter> JsonConverters { get { return _jsonConverters; } }
        private static List<FieldConverter> _fieldConverters;
        internal static List<FieldConverter> FieldConverters { get { return _fieldConverters; } }

        private static JsonSerializerSettings _jsonSettings;
        internal static JsonSerializerSettings JsonSettings { get { return _jsonSettings; } }

        static SnJsonConverter()
        {
            var fieldConverterTypes = TypeResolver.GetTypesByBaseType(typeof(FieldConverter));
            _jsonConverters = new List<JsonConverter>();
            _fieldConverters = new List<FieldConverter>();

            foreach (var fieldConverterType in fieldConverterTypes)
            {
                var fieldConverter = (FieldConverter)Activator.CreateInstance(fieldConverterType);
                _jsonConverters.Add(fieldConverter);
                _fieldConverters.Add(fieldConverter);
            }

            _jsonSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented,
                Converters = JsonConverters
            };
        }

        /// <summary>
        /// Serializes a single <see cref="SenseNet.ContentRepository.Content"/> into JSON with the specified options.
        /// </summary>
        public static void ToJson(this Content content, TextWriter writer, IEnumerable<string> select = null, IEnumerable<string> expand = null)
        {
            var req = ODataRequest.CreateSingleContentRequest(select, expand);
            var projector = Projector.Create(req, false, null);
            var fields = projector.Project(content);
            
            var serializer = JsonSerializer.Create(SnJsonConverter.JsonSettings);
            serializer.Serialize(writer, fields);
        }

        /// <summary>
        /// Serializes a single <see cref="SenseNet.ContentRepository.Content"/> into JSON with the specified options.
        /// </summary>
        public static string ToJson(this Content content, IEnumerable<string> select = null, IEnumerable<string> expand = null)
        {
            using (var writer = new StringWriter())
            {
                ToJson(content, writer, select, expand);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serializes a single <see cref="SenseNet.ContentRepository.Content"/> into JSON with the specified options.
        /// </summary>
        public static string ToJson(this Content content, params string[] select)
        {
            return ToJson(content, select, null);
        }

        /// <summary>
        /// Serializes a collection of <see cref="SenseNet.ContentRepository.Content"/> into JSON with the specified options.
        /// </summary>
        public static void ToJson(this IEnumerable<Content> contents, TextWriter writer, IEnumerable<string> select = null, IEnumerable<string> expand = null)
        {
            writer.Write("[");
            var first = true;
            foreach (var c in contents)
            {
                if (first)
                    first = false;
                else
                    writer.Write(",");
                ToJson(c, writer, select, expand);
            }
            writer.Write("]");
        }

        /// <summary>
        /// Serializes a collection of <see cref="SenseNet.ContentRepository.Content"/> into JSON with the specified options.
        /// </summary>
        public static string ToJson(this IEnumerable<Content> contents, IEnumerable<string> select = null, IEnumerable<string> expand = null)
        {
            using (var writer = new StringWriter())
            {
                ToJson(contents, writer, select, expand);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serializes a collection of <see cref="SenseNet.ContentRepository.Content"/> into JSON with the specified options.
        /// </summary>
        public static string ToJson(this IEnumerable<Content> contents, params string[] select)
        {
            return ToJson(contents, select, null);
        }
    }
}
