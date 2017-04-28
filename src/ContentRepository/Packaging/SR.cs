﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging
{
    internal static class SR
    {
        internal static class Errors
        {
            internal static readonly string ManifestNotFound = "Manifest not found.";
            internal static readonly string PackageCanContainOnlyOneFileInTheRoot = "Package can contain only one file in the root.";
            internal static readonly string InvalidPhaseIndex_2 = "Phase index out of range. Count of phases: {0}, requested index: {1}.";
            internal static readonly string InvalidParameters = "Invalid parameters";

            internal static readonly string PhaseFinishedWithError_1 = "Phase terminated with error: {0}";

            internal static class Manifest
            {
                internal static readonly string WrongRootName = @"Invalid manifest: root element must be ""Package"".";
                internal static readonly string MissingType = @"Invalid manifest: missing ""type"" attribute.";
                internal static readonly string InvalidType = @"Invalid manifest: invalid ""type"" attribute. The value must be ""tool"", ""patch"", or ""install""";
                internal static readonly string MissingComponentId = @"Invalid manifest: missing ""Id"" element.";
                internal static readonly string InvalidComponentId = @"Invalid manifest: invalid ""Id"" element. Value cannot be empty.";
                internal static readonly string MissingReleaseDate = @"Invalid manifest: missing ""ReleaseDate"" element.";
                internal static readonly string InvalidReleaseDate = @"Invalid manifest: invalid ""ReleaseDate"" element.";
                internal static readonly string TooBigReleaseDate = @"Invalid manifest: invalid ""ReleaseDate"" element: released in the future.";
                internal static readonly string MissingVersion = @"Invaid manifest: missing ""Version"" element.";
                internal static readonly string MissingVersionAttribute1 = @"Invalid manifest: missing ""{0}"" VersionControl attribute.";
                internal static readonly string InvalidVersion1 = @"Invalid manifest: invalid version: ""{0}""";
                internal static readonly string UnexpectedVersionAttribute = @"Invalid manifest: the ""version"" attribute cannot be defined if the ""minVersion"", ""maxVersion"", ""minVersionExclusive"" or ""maxVersionExclusive"" exist.";
                internal static readonly string DoubleMinVersionAttribute = @"Invalid manifest: cannot use the ""minVersion"" and ""minVersionExclusive"" attributes together.";
                internal static readonly string DoubleMaxVersionAttribute = @"Invalid manifest: cannot use the ""maxVersion"" and ""maxVersionExclusive"" attributes together.";
                internal static readonly string MissingDependencyId = @"Invalid manifest: missing ""id"" attribute in a Dependency element.";
                internal static readonly string EmptyDependencyId = @"Invalid manifest: empty ""id"" attribute in a Dependency element.";
                internal static readonly string MissingDependencyVersion = @"Invaid manifest: missing dependency version.";
            }

            internal static class Precondition
            {
                internal static readonly string DependencyNotFound1 = "Package cannot be executed: Dependency not found: {0}";
                internal static readonly string MinimumVersion1 = "Package cannot be executed: the version is smaller than permitted: {0}";
                internal static readonly string MaximumVersion1 = "Package cannot be executed: the version is greater than permitted: {0}";
                internal static readonly string TargetVersionTooSmall2 = @"Invalid manifest: the target version ({0}) must be greater than the current version ({1}).";
                internal static readonly string CannotInstallExistingComponent1 = "Cannot install existing component: {0}";
                internal static readonly string CannotUpdateMissingComponent1 = "Cannot update missing component: {0}";
            }

            internal static class StepParsing
            {
                internal static readonly string AttributeAndElementNameCollision_2 = "Attribute and element name must be unique. Step: {0}, name: {1}.";
                internal static readonly string UnknownStep_1 = "Unknown Step: {0}";
                internal static readonly string UnknownProperty_2 = "Unknown property. Step: {0}, name: {1}.";
                internal static readonly string DefaultPropertyNotFound_1 = "Default property not found. Step: {0}.";
                internal static readonly string PropertyTypeMustBeConvertible_2 = "The type of the property must be IConvertible. Step: {0}, property: {1}.";
                internal static readonly string CannotConvertToPropertyType_3 = "Cannot convert a string value to target type. Step: {0}, property: {1}, property type: {2}.";
            }

            internal static class Import
            {
                internal static readonly string SourceNotFound = "Source not found.";
                internal static readonly string TargetNotFound = "Target not found.";
                internal static readonly string InvalidTarget = "Invalid target. See inner exception.";
            }

            internal static class ContentTypeSteps
            {
                internal static readonly string InvalidContentTypeName = "Invalid content type name.";
                internal static readonly string ContentTypeNotFound = "Content type not found.";
                internal static readonly string FieldExists = "Cannot add the field if it exists.";
                internal static readonly string InvalidField_NameNotFound = "Invalid field xml: name not found";
                internal static readonly string FieldNameCannotBeNullOrEmpty = "Field name cannot be null or empty.";
                internal static readonly string InvalidFieldXml = "Invalid field xml.";
            }

            internal static class Content
            {
                internal static readonly string InvalidContentName_1 = "Invalid content name: {0}.";
                internal static readonly string ContentNotFound_1 = "Content not found: {0}.";
                internal static readonly string EmptyContent_1 = "Empty content: {0}.";
                internal static readonly string FieldNotFound_1 = "Field not found: {0}.";
            }

            internal static class Resource
            {
                internal static readonly string AttributeIsMissing_1 = "Invalid manifest: attribute is missing: {0}.";
                internal static readonly string ElementIsMissing_2 = "Invalid resource: {0} is missing in resource {1}.";
            }
        }
    }
}
