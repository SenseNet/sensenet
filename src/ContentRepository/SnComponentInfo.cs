using System;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    internal class SnComponentInfo
    {
        public string ComponentId { get; set; }
        public Version AssemblyVersion { get; set; }
        public Version SupportedVersion { get; set; }
        public Func<Version, bool> IsComponentAllowed { get; set; }
        public SnPatch[] Patches { get; set; }

        public static SnComponentInfo Create(ISnComponent component)
        {
            var asmVersion = TypeHandler.GetVersion(component.GetType().Assembly);
            if (component.SupportedVersion > asmVersion)
                throw new ApplicationException($"Invalid component: {component.ComponentId}: supported version ({component.SupportedVersion}) cannot be greater than the assembly version ({asmVersion}).");

            return new SnComponentInfo
            {
                ComponentId = component.ComponentId,
                SupportedVersion = component.SupportedVersion ?? asmVersion,
                AssemblyVersion = asmVersion,
                IsComponentAllowed = component.IsComponentAllowed,
                Patches = component.Patches
            };
        }
    }
}
