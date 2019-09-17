using System;
using System.Globalization;
using System.Linq;
using SenseNet.Tools;
// ReSharper disable InconsistentNaming

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Language independent strings.
    /// </summary>
    internal static class SR
	{
        private static readonly IResourceManager resMan;
        internal static IResourceManager ResourceManager => resMan;

	    static SR()
        {
            var resManType = TypeResolver.GetTypesByInterface(typeof(IResourceManager))
                .FirstOrDefault(t => t.FullName != typeof(DefaultResourceManager).FullName);

            resMan = (resManType != null) ? (IResourceManager)Activator.CreateInstance(resManType) : new DefaultResourceManager();
        }

        public static string GetString(string fullResourceKey)
        {
            return resMan.GetString(fullResourceKey);
        }
        public static string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(resMan.GetString(fullResourceKey), args);
        }

	    internal static string GetStringOrDefault(string fullResourceKey, string resourceKeyFragment, string defaultValue)
	    {
	        var msg = GetString(fullResourceKey);

	        // If the resource does not exist, resource manager returns a class-key pair. 
	        // We want to return a human readable default value instead.
	        if (string.IsNullOrEmpty(msg) || msg.Contains(resourceKeyFragment))
	            return defaultValue;

	        return msg;
	    }

        internal static class Exceptions
		{
			internal static class General
			{
				internal static string Msg_ParameterValueCannotBeZero = "The value of parameter cannot be zero.";

				internal static string Msg_InvalidPath_1 = "The path is invalid: {0}";
				internal static string Msg_PathTooLong = "Path exceeds the maximum lenght allowed";
				internal static string Msg_ParentNodeDoesNotExists = "Parent Node does not exists";
				internal static string Msg_NameCannotBeEmpty = "Name cannot be Empty";
				internal static string Msg_InvalidName = "The Name is invalid";
				internal static string Msg_InvalidParameter = "Invalid parameter";
				internal static string Msg_FileNameExtensionCannotBeNull = "The value of fileName.Extension cannot be null.";
				internal static string Msg_AssociatedBinaryDataDoesNotExist = "Associated BinaryData does not exist.";
                internal static string Msg_CannotWriteReadOnlyProperty_1 = "Cannot write the {0} property because it is read only.";
                internal static Exception Exc_LessThanDateTimeMinValue()
                {
					return new ArgumentOutOfRangeException(String.Concat("DateTime value cannot be less than ",
						Data.DataStore.DateTimeMinValue.ToString(CultureInfo.CurrentCulture)));
                }
				internal static Exception Exc_BiggerThanDateTimeMaxValue()
				{
					return new ArgumentOutOfRangeException(String.Concat("DateTime value cannot be bigger than ",
						Data.DataStore.DateTimeMaxValue.ToString(CultureInfo.CurrentCulture)));
                }

                internal static string Error_Preview_BinaryAccess_2 = "$Error_Storage:Preview_BinaryAccess_2";
                internal static string Error_PathTooLong_1 = "$Error_Storage:PathTooLong_1";
                internal static string Error_AccessToNotFinalizedBinary_2 = "$Error_Storage:AccessToNotFinalizedBinary_2";

			    internal static string Error_PathTooLong_MaxValue_1 = "$Portal:PathTooLongMessage";
			    internal static string Error_EmptyNameMessage = "$Portal:EmptyNameMessage";
			    internal static string Error_InvalidPathMessage = "$Portal:InvalidPathMessage";
			    internal static string Error_InvalidNameMessage = "$Portal:InvalidNameMessage";
			    internal static string Error_NameStartsWithWhitespaceMessage = "$Portal:NameStartsWithWhitespaceMessage";
			    internal static string Error_NameEndsWithWhitespaceMessage = "$Portal:NameEndsWithWhitespaceMessage";
			    internal static string Error_PathFirstCharMessage = "$Portal:PathFirstCharMessage";
			    internal static string Error_PathEndsWithDotMessage = "$Portal:PathEndsWithDotMessage";
            }
            internal static class Operations
            {
                internal static string CopyFailed_SouceDoesNotExistWithPath_1 = "$Error_Storage:CopyFailed_SouceDoesNotExistWithPath_1";
                internal static string CopyFailed_TargetDoesNotExistWithPath_1 = "$Error_Storage:CopyFailed_TargetDoesNotExistWithPath_1";
                internal static string MoveFailed_SouceDoesNotExistWithPath_1 = "$Error_Storage:MoveFailed_SouceDoesNotExistWithPath_1";
                internal static string MoveFailed_TargetDoesNotExistWithPath_1 = "$Error_Storage:MoveFailed_TargetDoesNotExistWithPath_1";
                internal static string DeleteFailed_ContentDoesNotExistWithPath_1 = "$Error_Storage:DeleteFailed_ContentDoesNotExistWithPath_1";
                internal static string DeleteFailed_ContentDoesNotExistWithId_1 = "$Error_Storage:DeleteFailed_ContentDoesNotExistWithId_1";
            }

			internal static class Schema
			{
				internal static string Msg_InconsistentHierarchy = "Inconsistent hierarchy";
				internal static string Msg_KeyAndTypeNameAreNotEqual = "Key and TypeName are not equal";
				internal static string Msg_UnknownPropertySetType = "Unknown PropertySet type";
				internal static string Msg_UnknownNodeType = "Cannot create Node with unknown NodeType";
				internal static string Msg_CircularReference = "NodeType and his parent cannot be same.";
				internal static string Msg_MappingAlreadyExists = "Mapping already exists";
				internal static string Msg_ProtectedPropetyTypeDeleteViolation = "Protected PropetyType Delete violation";
				internal static string Msg_PropertyTypeDoesNotExist = "PropertyType does not exist";
				internal static string Msg_NodeAttributeDoesNotEsist = "NodeAttribute does not exist:";
			}
			internal static class VersionNumber
			{
                internal static string InvalidVersionFormat = "$Exceptions:Storage_VersionNumber_InvalidVersionFormat";
                internal static string InvalidVersionStatus_1 = "$Exceptions:Storage_VersionNumber_InvalidVersionStatus";
            }
			internal static class Search
			{
				internal static string Msg_UnknownStringOperator_1 = "Unknown StringOperator: {0}";
				internal static string Msg_UnknownValueOperator_1 = "Unknown ValueOperator: {0}";
				internal static string Msg_PageSizeOutOfRange = "PageSize minimum value is 1.";
				internal static string Msg_StartIndexOutOfRange = "StartIndex minimum value is 1.";
                internal static string Msg_SkipOutOfRange = "Skip minimum value is 0.";
				internal static string Msg_TopOutOfRange = "Top minimum value is 1.";
				internal static string Msg_InvalidNodeQueryXml = "Invalid NodeQuery XML";
			}
			internal static class Configuration
			{
				internal static string Msg_DataProviderImplementationDoesNotExist = "DataProvider implementation does not exist";
				internal static string Msg_InvalidDataProviderImplementation = "DataProvider implementation must be inherited from SenseNet.ContentRepository.Storage.Data.DataProvider";
				internal static string Msg_AccessProviderImplementationDoesNotExist = "AccessProvider implementation does not exist";
				internal static string Msg_InvalidAccessProviderImplementation = "AccessProvider implementation must be inherited from SenseNet.ContentRepository.Storage.Security.AccessProvider";
                internal static string Msg_PasswordHashProviderImplementationDoesNotExist = "PasswordHashProvider implementation does not exist";
                internal static string Msg_InvalidPasswordHashProviderImplementation = "PasswordHashProvider implementation must be inherited from SenseNet.ContentRepository.Storage.Security.PasswordHashProvider";
                internal static string Msg_DocumentPreviewProviderImplementationDoesNotExist = "DocumentPreviewProvider implementation does not exist";
                internal static string Msg_InvalidDocumentPreviewProviderImplementation = "DocumentPreviewProvider implementation must be inherited from SenseNet.Preview.DocumentPreviewProvider";
			}
			internal static class XmlSchema
			{
				internal static string Msg_SchemaNotLoaded = "Cannot validate: schema was not loaded";
			}
		}
	}
}
