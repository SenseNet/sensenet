using System;
using System.Xml;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    public class VersionControl
    {
        [Obsolete("####", true)]
        public Version Target { get; private set; }
        public Version ExpectedMinimum { get; private set; }
        public Version ExpectedMaximum { get; private set; }

        internal static VersionControl Initialize(XmlElement element, PackageLevel level)
        {
            var vc = new VersionControl();
            if (element == null)
                return vc;

            var expectedMin = GetVersion(element, "expectedMin", false);
            var expectedMax = GetVersion(element, "expectedMax", false);
            var expected = GetVersion(element, "expected", false);
            if (expected != null && (expectedMin != null || expectedMax != null))
                throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedExpectedVersion);

            if (expected != null)
                expectedMin = expectedMax = expected;

            vc.ExpectedMinimum = expectedMin;
            vc.ExpectedMaximum = expectedMax;

            return vc;
        }

        private static Version GetVersion(XmlElement element, string name, bool required)
        {
            var attr = element.Attributes[name] ?? element.Attributes[PackageManager.ToPascalCase(name)];

            if (attr == null)
            {
                if(required)
                    throw new InvalidPackageException(String.Format(SR.Errors.Manifest.MissingVersionAttribute_1, name));
                return null;
            }

            return ParseVersion(attr.Value);
        }
        internal static Version ParseVersion(string value)
        {
            Version v;
            if (Version.TryParse(value, out v))
                return v;
            throw new InvalidPackageException(string.Format(SR.Errors.Manifest.InvalidVersion_1, value));
        }
    }
}
