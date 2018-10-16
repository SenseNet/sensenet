using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SenseNet.ContentRepository.Sharing
{
    //UNDONE: implement SharingData
    public enum SharingLevel
    {
        Open,
        Edit
    }
    public enum SharingMode
    {
        Public,
        Authenticated,
        Private
    }
    /// <summary>
    /// Storage model of content sharing information.
    /// </summary>
    internal class SharingData
    {
        public string Token { get; set; }
        public int Identity { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] //UNDONE: json converter attribute may be moved to serializer settings
        public SharingMode Mode { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SharingLevel Level { get; set; }
        public int CreatorId { get; set; }
        public DateTime ShareDate { get; set; }
    }
}
