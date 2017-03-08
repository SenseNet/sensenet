using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
	public static class RepositorySchema
	{
		public static string[] ContentTypeNames { get { return ContentType.GetContentTypeNames(); } }
		public static string[] RootContentTypeNames { get { return ContentType.GetRootTypeNames(); } }
		public static ContentType[] ContentTypes { get { return ContentType.GetContentTypes(); } }
		public static ContentType[] RootContentTypes { get { return ContentType.GetRootTypes(); } }

		public static Type[] ContentHandlers { get { return TypeResolver.GetTypesByBaseType(typeof(Node)); } }
		public static Type[] NodeObservers { get { return TypeResolver.GetTypesByBaseType(typeof(NodeObserver)); } }
		public static Type[] FieldTypes { get { return FieldManager.GetFieldTypes(); } }
		public static Type[] FieldSettingTypes { get { return TypeResolver.GetTypesByBaseType(typeof(FieldSetting)); } }

		public static string[] FieldShortNames { get { return FieldManager.FieldShortNamesFullNames.Keys.ToArray<string>(); } }

		public static string GetFieldTypeNameByShortName(string shortName)
		{
			return FieldManager.GetFieldHandlerName(shortName);
		}
		public static Type GetFieldTypeByShortName(string shortName)
		{
			var name = FieldManager.GetFieldHandlerName(shortName);
			return TypeResolver.GetType(name);
		}
		public static string GetFieldShortName(Type fieldType)
		{
			return GetFieldShortName(fieldType.FullName);
		}
		public static string GetFieldShortName(string fieldTypeName)
		{
			var dict = FieldManager.FieldShortNamesFullNames;
			foreach (string shortName in dict.Keys)
				if (dict[shortName] == fieldTypeName)
					return shortName;
			return null;
		}
        public static int GetCountOfFieldBindings(string fieldTypeName)
        {
            return FieldManager.GetCountOfFieldBindings(fieldTypeName);
        }

		public static string GetDefaultFieldSettingTypeName(string shortName)
		{
			return GetDefaultFieldSettingTypeName(GetFieldTypeByShortName(shortName));
		}
		public static string GetDefaultFieldSettingTypeName(Type fieldType)
		{
			return FieldManager.GetDefaultFieldSettingTypeName(fieldType.FullName);
		}
		public static Type GetDefaultFieldSettingType(string shortName)
		{
			return GetDefaultFieldSettingType(GetFieldTypeByShortName(shortName));
		}
		public static Type GetDefaultFieldSettingType(Type fieldType)
		{
			var typeName = GetDefaultFieldSettingTypeName(fieldType);
			return TypeResolver.GetType(typeName);
		}
		public static Type[] GetAvilableFieldSettingTypes(string shortName)
		{
			return GetAvilableFieldSettingTypes(GetFieldTypeByShortName(shortName));
		}
		public static Type[] GetAvilableFieldSettingTypes(Type fieldType)
		{
			var types = new List<Type>();
			types.Add(GetDefaultFieldSettingType(fieldType));
			types.AddRange(TypeResolver.GetTypesByBaseType(GetDefaultFieldSettingType(fieldType)));
			return types.ToArray();
		}
	}
}
