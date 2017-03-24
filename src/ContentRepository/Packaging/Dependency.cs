using System;
using System.Xml;

namespace SenseNet.Packaging
{
    public class Dependency
    {
        public string Id { get; private set; }
        public Version MinVersion { get; private set; }
        public Version MaxVersion { get; private set; }
        public bool MinVersionIsExclusive { get; private set; }
        public bool MaxVersionIsExclusive { get; private set; }

        public static Dependency Parse(XmlElement element)
        {
            var dependency = new Dependency();

            var id = (element.Attributes["id"] ?? element.Attributes["Id"])?.Value;
            var version = GetVersion(element, "version");
            var minVersion = GetVersion(element, "minVersion");
            var maxVersion = GetVersion(element, "maxVersion");
            var minVersionExclusive = GetVersion(element, "minVersionExclusive");
            var maxVersionExclusive = GetVersion(element, "maxVersionExclusive");

            if (id == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingDependencyId, PackagingExceptionType.MissingDependencyId);
            if (id.Length == 0)
                throw new InvalidPackageException(SR.Errors.Manifest.EmptyDependencyId, PackagingExceptionType.EmptyDependencyId);

            if (version == null && minVersion == null && maxVersion == null && minVersionExclusive == null && maxVersionExclusive == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingDependencyVersion, PackagingExceptionType.MissingDependencyVersion);
            if (version != null && (minVersion != null || maxVersion != null || minVersionExclusive != null || maxVersionExclusive != null))
                throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedVersionAttribute, PackagingExceptionType.UnexpectedVersionAttribute);

            if (minVersion != null && minVersionExclusive != null)
                throw new InvalidPackageException(SR.Errors.Manifest.DoubleMinVersionAttribute, PackagingExceptionType.DoubleMinVersionAttribute);
            if (maxVersion != null && maxVersionExclusive != null)
                throw new InvalidPackageException(SR.Errors.Manifest.DoubleMaxVersionAttribute, PackagingExceptionType.DoubleMaxVersionAttribute);

            if (version != null)
                minVersion = maxVersion = version;

            dependency.Id = id;
            dependency.MinVersion = minVersion ?? minVersionExclusive;
            dependency.MaxVersion = maxVersion ?? maxVersionExclusive;
            dependency.MinVersionIsExclusive = minVersionExclusive != null;
            dependency.MaxVersionIsExclusive = maxVersionExclusive != null;

            return dependency;
        }

        private static Version GetVersion(XmlElement element, string name)
        {
            var attr = element.Attributes[name] ?? element.Attributes[PackageManager.ToPascalCase(name)];
            if (attr == null)
                return null;
            return ParseVersion(attr.Value);
        }
        internal static Version ParseVersion(string value)
        {
            Version v;
            if (Version.TryParse(value, out v))
                return v;
            throw new InvalidPackageException(string.Format(SR.Errors.Manifest.InvalidVersion1, value), PackagingExceptionType.InvalidVersion);
        }
    }
}
