using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Portal
{
    internal class SNSR
    {
        internal class Exceptions
        {
            internal class HttpAction
            {
                public static string NodeIsNotAnApplication_3 = "$Error_Portal:HttpAction_NodeIsNotAnApplication_3";
                public static string NotFound_1 =               "$Error_Portal:HttpAction_NotFound_1";
                public static string Forbidden_1 =              "$Error_Portal:HttpAction_Forbidden_1";
            }
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

                public static string RestoreExistingName =         "$Error_Portal:OData_Restore_ExistingName";
                public static string RestoreForbiddenContentType = "$Error_Portal:OData_Restore_ForbiddenContentType";
                public static string RestoreNoParent =             "$Error_Portal:OData_Restore_NoParent";
                public static string RestorePermissionError =      "$Error_Portal:OData_Restore_PermissionError";
            }
            internal class Site
            {
                public static string UrlListCannotBeEmpty =        "$Error_Portal:Site_UrlListCannotBeEmpty";
                public static string StartPageMustBeUnderTheSite = "$Error_Portal:Site_StartPageMustBeUnderTheSite";
                public static string UrlAlreadyUsed_2 =            "$Error_Portal:Site_UrlAlreadyUsed_2";
                public static string InvalidUri_1 =                "$Error_Portal:Site_InvalidUri_1";
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
