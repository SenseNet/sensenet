using System;

namespace SenseNet.ContentRepository
{
    internal class ServicesComponent : SnComponent
    {
        public override string ComponentId => "SenseNet.Services";

        //UNDONE: Set the SupportedVersion version before release.
        public override Version SupportedVersion => null;
    }
}
