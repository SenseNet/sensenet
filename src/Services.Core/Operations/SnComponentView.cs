using System;
using System.Diagnostics;
using SenseNet.Packaging;

namespace SenseNet.Services.Core.Operations
{
    [DebuggerDisplay("{ComponentId}v{Version}")]
    public class SnComponentView
    {
        public string ComponentId { get; set; }
        public Version Version { get; set; }
        public Version LatestVersion { get; set; }
        public string Description { get; set; }
        public Dependency[] Dependencies { get; set; }
    }
}
