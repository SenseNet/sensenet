using System;

namespace SenseNet.ContentRepository.Storage
{
    public class ComponentInfo
    {
        public string ComponentId { get; set; }
        public Version Version { get; set; }
        public Version AcceptableVersion { get; set; }
        public string Description { get; set; }

        public static readonly ComponentInfo Empty = new ComponentInfo
        {
            ComponentId = string.Empty,
            Version = new Version(0, 0),
            AcceptableVersion = null,
            Description = string.Empty
        };
    }
}
