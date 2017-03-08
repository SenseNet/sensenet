using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    public class VersionControl
    {
        public Version Target { get; private set; }
        public Version ExpectedProductMinimum { get; private set; }
        public Version ExpectedProductMaximum { get; private set; }
        public Version ExpectedApplicationMinimum { get; private set; }
        public Version ExpectedApplicationMaximum { get; private set; }

        private static Version _minVersion = new Version(0, 0);
        private static Version _maxVersion = new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);

        internal static VersionControl Initialize(XmlElement element, PackageLevel level, PackageType type)
        {
            var vc = new VersionControl();
            if (element == null)
                return vc;

            if (level == PackageLevel.Tool)
            {
                if (GetVersion(element, "target", false) != null)
                    throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedTarget);
            }
            else
            {
                vc.Target = GetVersion(element, "target", true);
            }

            var expectedMin = GetVersion(element, "expectedMin", false);
            var expectedMax = GetVersion(element, "expectedMax", false);
            var expected = GetVersion(element, "expected", false);
            if (expected != null && (expectedMin != null || expectedMax != null))
                throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedExpectedVersion);

            if (expected != null)
                expectedMin = expectedMax = expected;

            switch (type)
            {
                case PackageType.Product:
                    vc.ExpectedProductMinimum = expectedMin;
                    vc.ExpectedProductMaximum = expectedMax;
                    break;
                case PackageType.Application:
                    vc.ExpectedApplicationMinimum = expectedMin;
                    vc.ExpectedApplicationMaximum = expectedMax;
                    break;
                default:
                    throw new SnNotSupportedException("Unknown PackageType: " + type);
            }

            return vc;
        }

        private static Version GetVersion(XmlElement element, string name, bool required)
        {
            var attr = element.Attributes[name];
            if(attr == null)
                attr = element.Attributes[PackageManager.ToPascalCase(name)];

            if (attr == null)
            {
                if(required)
                    throw new InvalidPackageException(String.Format(SR.Errors.Manifest.MissingVersionAttribute_1, name));
                return null;
            }

            return GetVersion(attr);
        }
        private static Version GetVersion(XmlAttribute attr)
        {
            Version v;
            if (Version.TryParse(attr.Value, out v))
                return v;
            throw new InvalidPackageException(String.Format(SR.Errors.Manifest.InvalidVersionAttribute_1, attr.Name));
        }

    }
}
