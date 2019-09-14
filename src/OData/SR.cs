using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.OData
{
    internal class SNSR
    {
        internal class Exceptions
        {
            internal class OData
            {
                public static string InvalidId =                 "$Error_Portal:OData_InvalidId";
                public static string InvalidTopOption =          "$Error_Portal:OData_InvalidTopOption";
                public static string InvalidSkipOption =         "$Error_Portal:OData_InvalidSkipOption";
                public static string InvalidInlineCountOption =  "$Error_Portal:OData_InvalidInlineCountOption";
                public static string InvalidFormatOption =       "$Error_Portal:OData_InvalidFormatOption";
                public static string InvalidOrderByOption =      "$Error_Portal:OData_InvalidOrderByOption";
                public static string ResourceNotFound =          "$Error_Portal:OData_ResourceNotFound";
                public static string ResourceNotFound_2 =        "$Error_Portal:OData_ResourceNotFound_2";
                public static string CannotConvertToJSON_2 =     "$Error_Portal:OData_CannotConvertToJSON_2";
                public static string ContentAlreadyExists_1 =    "$Error_Portal:OData_ContentAlreadyExists_1";
                public static string ErrorContentNotFound =      "$Error_Portal:ErrorContentNotFound";

                public static string RestoreExistingName =         "$Error_Portal:OData_Restore_ExistingName";
                public static string RestoreForbiddenContentType = "$Error_Portal:OData_Restore_ForbiddenContentType";
                public static string RestoreNoParent =             "$Error_Portal:OData_Restore_NoParent";
                public static string RestorePermissionError =      "$Error_Portal:OData_Restore_PermissionError";
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
        public static string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(SenseNetResourceManager.Current.GetString(fullResourceKey), args);
        }
    }
}
