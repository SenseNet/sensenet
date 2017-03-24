using System;
using System.Xml;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    public class VersionControl
    {
        public Version ExpectedMinimum { get; private set; }
        public Version ExpectedMaximum { get; private set; }
        public bool ExpectedMinimumIsExclusive { get; private set; }
        public bool ExpectedMaximumIsExclusive { get; private set; }


        internal static VersionControl Initialize(XmlElement element, PackageLevel level)
        {
            var vc = new VersionControl();
            if (element == null)
                return vc;

            var version = GetVersion(element, "version", false);
            var minVersion = GetVersion(element, "minVersion", false);
            var maxVersion = GetVersion(element, "maxVersion", false);
            var minVersionExclusive = GetVersion(element, "minVersionExclusive", false);
            var maxVersionExclusive = GetVersion(element, "maxVersionExclusive", false);

            if (version != null && (minVersion != null || maxVersion != null || minVersionExclusive != null || maxVersionExclusive != null))
                throw new InvalidPackageException(SR.Errors.Manifest.MissingDependencyVersion);
            if (version != null && (minVersion != null || maxVersion != null || minVersionExclusive != null || maxVersionExclusive != null))
                throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedVersionAttribute);
            if (minVersion != null && minVersionExclusive != null)
                throw new InvalidPackageException(SR.Errors.Manifest.DoubleMinVersionAttribute);
            if (maxVersion != null && maxVersionExclusive != null)
                throw new InvalidPackageException(SR.Errors.Manifest.DoubleMaxVersionAttribute);

            if (version != null)
                minVersion = maxVersion = version;
            
            vc.ExpectedMinimum = minVersion ?? minVersionExclusive;
            vc.ExpectedMaximum = maxVersion ?? maxVersionExclusive;
            vc.ExpectedMinimumIsExclusive = minVersionExclusive != null;
            vc.ExpectedMaximumIsExclusive = maxVersionExclusive != null;
            return vc;
        }

        private static Version GetVersion(XmlElement element, string name, bool required)
        {
            var attr = element.Attributes[name] ?? element.Attributes[PackageManager.ToPascalCase(name)];

            if (attr == null)
            {
                if(required)
                    throw new InvalidPackageException(String.Format(SR.Errors.Manifest.MissingVersionAttribute1, name));
                return null;
            }

            return ParseVersion(attr.Value);
        }
        internal static Version ParseVersion(string value)
        {
            Version v;
            if (Version.TryParse(value, out v))
                return v;
            throw new InvalidPackageException(string.Format(SR.Errors.Manifest.InvalidVersion1, value));
        }
    }
}
