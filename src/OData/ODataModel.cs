using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.ContentRepository.OData;

namespace SenseNet.OData
{
    internal class ODataSimpleMeta
    {
        [JsonProperty(PropertyName = "uri", Order = 1)]
        public string Uri { get; set; }
        [JsonProperty(PropertyName = "type", Order = 2)]
        public string Type { get; set; }
    }
    internal class ODataFullMeta : ODataSimpleMeta
    {
        [JsonProperty(PropertyName = "actions", Order = 3)]
        public ODataOperation[] Actions { get; set; }
        [JsonProperty(PropertyName = "functions", Order = 4)]
        public ODataOperation[] Functions { get; set; }
    }
    internal class ODataOperation
    {
        [JsonProperty(PropertyName = "title", Order = 1)]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "name", Order = 2)]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "target", Order = 3)]
        public string Target { get; set; }
        [JsonProperty(PropertyName = "forbidden", Order = 4)]
        public bool Forbidden { get; set; }
        [JsonProperty(PropertyName = "parameters", Order = 5)]
        public ODataOperationParameter[] Parameters { get; set; }
    }
    internal class ODataOperationParameter
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "type", Order = 2)]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "required", Order = 3)]
        public bool Required { get; set; }
    }

    internal class ODataSingleContent
    {
        [JsonProperty(PropertyName = "d", Order = 1)]
        public ODataEntity FieldData { get; set; }
    }
    internal class ODataMultipleContent
    {
        [JsonProperty(PropertyName = "d", Order = 1)]
        public Dictionary<string, object> Contents { get; private set; }
        public static ODataMultipleContent Create(IEnumerable<ODataEntity> data, int count)
        {
            var array = data.ToArray();

            // ReSharper disable once CoVariantArrayConversion
            return CreateFromArray(array, count);
        }
        public static ODataMultipleContent Create(IEnumerable<ODataObject> data, int count)
        {
            var array = data?.Select(odc => odc.Data).ToArray() ?? new object[0];

            return CreateFromArray(array, count);
        }

        private static ODataMultipleContent CreateFromArray(object[] array, int count)
        {
            var dict = new Dictionary<string, object>
            {
                {"__count", count == 0 ? array.Length : count},
                {"results", array}
            };
            return new ODataMultipleContent { Contents = dict };
        }
    }
    
    internal class ODataReference
    {
        [JsonProperty(PropertyName = "__deferred", Order = 1)]
        public ODataUri Reference { get; private set; }
        public static ODataReference Create(string uri)
        {
            return new ODataReference { Reference = new ODataUri { Uri = uri } };
        }
    }

    internal class ODataUri
    {
        [JsonProperty(PropertyName = "uri", Order = 1)]
        public string Uri { get; set; }
    }
    internal class ODataBinary
    {
        [JsonProperty(PropertyName = "__mediaresource", Order = 1)]
        public ODataMediaResource Resource { get; private set; }
        public static ODataBinary Create(string url, string editUrl, string mimeType, string etag)
        {
            return new ODataBinary { Resource = new ODataMediaResource { EditMediaUri = editUrl, MediaUri = url, MimeType = mimeType, MediaETag = etag } };
        }
    }
    internal class ODataMediaResource
    {
        [JsonProperty(PropertyName = "edit_media", Order = 1)]
        public string EditMediaUri { get; set; }
        [JsonProperty(PropertyName = "media_src", Order = 2)]
        public string MediaUri { get; set; }
        [JsonProperty(PropertyName = "content_type", Order = 3)]
        public string MimeType { get; set; }
        [JsonProperty(PropertyName = "media_etag", Order = 4)]
        public string MediaETag { get; set; }
    }

    /// <summary>
    /// Represents an item in the Actions list of the OData Content metadata.
    /// </summary>
    public class ODataActionItem
    {
        internal static readonly string Ctd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""Action"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    <Field name=""Name"" type=""ShortText"" />
    <Field name=""DisplayName"" type=""ShortText"" />
    <Field name=""Index"" type=""Integer"" />
    <Field name=""Icon"" type=""ShortText"" />
    <Field name=""Url"" type=""ShortText"" />
    <Field name=""IsODataAction"" type=""Boolean"" />
    <Field name=""ActionParameters"" type=""LongText"" />
    <Field name=""Scenario"" type=""ShortText"" />
    <Field name=""Forbidden"" type=""Boolean"" />
  </Fields>
</ContentType>
";

        /// <summary>
        /// Gets or sets the name of the Action.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the human readable name of the Action.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Gets os sets the value that helps sorting the items.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Gets or sets the icon name of the Action.
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// Gets or sets the URL of the Action.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the Action is an <see cref="ODataOperation"/>.
        /// </summary>
        public bool IsODataAction { get; set; }
        /// <summary>
        /// Gets or sets the parameter names of the Action.
        /// </summary>
        public string[] ActionParameters { get; set; }
        /// <summary>
        /// Gets or sets the scenario in which the Action was found.
        /// </summary>
        public string Scenario { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the Action is
        /// visible but not executable for the current user.
        /// </summary>
        public bool Forbidden { get; set; }
    }

}
