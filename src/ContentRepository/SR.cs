using SenseNet.ContentRepository.i18n;

namespace SenseNet.ContentRepository
{
    internal static class SR
    {
        internal static class Exceptions
        {
            internal static class Content
            {
                internal static string Msg_CannotCreateNewContentWithNullArgument = "Cannot create new Content: Argument cannot be null in this construction mechanism.";
                internal static string Msg_UnknownContentType = "Unknown ContentType";
            }
            internal static class Registration
            {
                internal static string Msg_NodeTypeMustBeInheritedFromNode_1 = "NodeType must be inherited from Node: {0}";
                internal static string Msg_DefinedHandlerIsNotAContentHandler = "Invalid ContentTypeDefinition: defined handler is not a ContentHandler";
                internal static string Msg_UnknownParentContentType = "Parent ContentType is not found";
                internal static string Msg_DataTypeCollisionInTwoProperties_4 = "DataType collision in two properties. NodeType = '{0}', PropertyType = '{1}', original DataType = {2}, passed DataType = {3}.";

                // Attribute parsing
                internal static string Msg_PropertyTypeAttributesWithTheSameName_2 = "PropertyAttributes with the same name are not allowed. Class: {0}, Property: {1}";

                // Content registration
                internal static string Msg_InvalidContentTypeDefinitionXml = "Invalid ContentType Definition XML";
                internal static string Msg_InvalidContentListDefinitionXml = "Invalid ContentList Definition XML";
                internal static string Msg_InvalidAspectDefinitionXml = "Invalid Aspect Definition XML";
                internal static string Msg_ContentHandlerNotFound = "ContentHandler cannot be found";
                internal static string Msg_UnknownFieldType = "Unknown FieldType";
                internal static string Msg_UnknownFieldSettingType = "Unknown FieldSetting Type";
                internal static string Msg_FieldTypeNotSpecified = "FieldType is not specified";
                internal static string Msg_NotARepositoryDataType = "Type is not a sensenet Content Repository DataType";
                internal static string Msg_FieldBindingsCount_1 = "The length of Field's Bindings list must be {0}";
                internal static string Msg_InconsistentContentTypeName = "Cannot modify ContentTypeDefinition: ContentTypeSetting's name and ContentType name in XML content are not equal.";
                internal static string Msg_PropertyAndFieldAreNotConnectable = "Field cannot be saved with the name {1} on Content Type {0}.";
                internal static string Msg_InvalidReferenceField_2 = "Property cannot be used with a Reference field. Type of the property must be IEnumerable<Node>, Node or a class that is inherited from Node. ContentType: {0}, Field: {1}";
            }
            internal static class Configuration
            {
                internal static string Msg_DirectoryProviderImplementationDoesNotExist = "DirectoryProvider implementation does not exist";
                internal static string Msg_InvalidDirectoryProviderImplementation = "DirectoryProvider implementation must be inherited from SenseNet.ContentRepository.DirectoryProvider";
                internal static string Msg_DocumentPreviewProviderImplementationDoesNotExist = "DocumentPreviewProvider implementation does not exist";
                internal static string Msg_InvalidDocumentPreviewProviderImplementation = "DocumentPreviewProvider implementation must be inherited from SenseNet.Preview.DocumentPreviewProvider";
            }

            internal static class i18n
            {
                internal static string LoadResourcesParameterValueNull = "LoadResourcesParameterValueNull";
            }

            internal static class Settings
            {
                internal static string Error_ForbiddenPath_2 = "$Error_ContentRepository:Settings_ForbiddenPath_2";
                internal static string Error_ForbiddenExtension = "$Error_ContentRepository:Settings_ForbiddenExtension";
                internal static string Error_NameExists_1 = "$Error_ContentRepository:Settings_NameExists_1";
                internal static string Error_GlobalOnly_1 = "$Error_ContentRepository:Settings_GlobalOnly_1";
                internal static string Error_InvalidEnumValue_2 = "$Error_ContentRepository:Settings_InvalidEnumValue_2";
            }

            internal static class File
            {
                internal static string Error_ForbiddenExecutable_2 = "$Error_ContentRepository:File_ForbiddenExecutable_2";
                internal static string Error_ForbiddenExecutable = "$Error_ContentRepository:File_ForbiddenExecutable";
            }

            internal static class ContentList
            {
                internal static string Error_EmailIsTaken = "$Error_ContentRepository:ContentList_EmailIsTaken";
            }

            internal static class Fields
            {
                internal static string Error_Choice_OneOption = "$FieldEditor:FieldError_Choice_OneOption";
            }

            internal static class CalendarEvent
            {
                internal static string Error_InvalidStartEndDate = "$Error_ContentRepository:CalendarEvent_InvalidStartEndDate";
            }

            internal static class Group
            {
                internal static readonly string NotAGroup = "The content must be a group.";
                internal static readonly string CannotAddMembersToASpecialGroup = "Cannot add members to the {0} special group.";
                internal static readonly string CannotRemoveMembersFromASpecialGroup = "Cannot remove members from the {0} special group.";
            }

            internal static class User
            {
                internal static string Error_NonUnique = "$Error_ContentRepository:User_NonUnique";
            }
        }

        public static string GetString(string fullResourceKey)
        {
            return SenseNetResourceManager.Current.GetString(fullResourceKey);
        }
        public static string GetString(string className, string name)
        {
            return SenseNetResourceManager.Current.GetString(className, name);
        }
    }
}