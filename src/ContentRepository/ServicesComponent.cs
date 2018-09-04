using System;

namespace SenseNet.ContentRepository
{
    internal class ServicesComponent : SnComponent
    {
        public override string ComponentId => "SenseNet.Services";

        //TODO: Set SupportedVersion before release.
        // This value has to change if there were database, content
        // or configuration changes since the last release that
        // should be enforced using an upgrade patch.
        public override Version SupportedVersion => new Version(7, 3, 3);
    }
}
