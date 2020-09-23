using System;
using System.Xml;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Defines a component / package dependency
    /// </summary>
    public class Dependency
    {
        /// <summary>
        /// Gets or sets the Id of the target component / package
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the scope of the dependency
        /// </summary>
        public VersionBoundary Boundary { get; set; } = new VersionBoundary();

        /// <summary>
        /// Gets or sets the Boundary.MinVersion.
        /// This property is deprecated use the Boundary.MinVersion instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        [JsonIgnore]
        public Version MinVersion { get => Boundary.MinVersion; set => Boundary.MinVersion = value; }

        /// <summary>
        /// Gets or sets the Boundary.MaxVersion.
        /// This property is deprecated use the Boundary.MaxVersion instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        [JsonIgnore]
        public Version MaxVersion { get => Boundary.MaxVersion; set => Boundary.MaxVersion = value; }

        /// <summary>
        /// Gets or sets the Boundary.MinVersionIsExclusive.
        /// This property is deprecated use the Boundary.MinVersionIsExclusive instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        [JsonIgnore]
        public bool MinVersionIsExclusive
        {
            get => Boundary.MinVersionIsExclusive;
            set => Boundary.MinVersionIsExclusive = value;
        }

        /// <summary>
        /// Gets or sets the Boundary.MaxVersionIsExclusive.
        /// This property is deprecated use the Boundary.MaxVersionIsExclusive instead.
        /// </summary>
        [Obsolete("Use Boundary.MaxVersionIsExclusive instead.")]
        [JsonIgnore]
        public bool MaxVersionIsExclusive
        {
            get => Boundary.MaxVersionIsExclusive;
            set => Boundary.MaxVersionIsExclusive = value;
        }

        public static Dependency Parse(XmlElement element)
        {
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

            return new Dependency
            {
                Id = id,
                Boundary = new VersionBoundary
                {
                    MinVersion = minVersion ?? minVersionExclusive,
                    MaxVersion = maxVersion ?? maxVersionExclusive,
                    MinVersionIsExclusive = minVersionExclusive != null,
                    MaxVersionIsExclusive = maxVersionExclusive != null,
                }
            };
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

        /// <summary>
        /// String representation of this Dependency instance.
        /// </summary>
        public override string ToString()
        {
            return $"{Id}: {Boundary}";
        }
    }
}
