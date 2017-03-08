using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Schema;
using System.Reflection;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Fields;
using System.Linq;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
    public static class FieldManager
    {
        private static object syncRoot = new object();

        private static bool initialized = false;

        // Field FullName => Field Type
        private static Dictionary<string, Type> fieldTypes;
        // FieldSetting FullName => FieldSetting Type
        private static Dictionary<string, Type> fieldSettingTypes;
        // Field ShortName => Field FullName
        private static Dictionary<string, string> fieldShortNamesFullNames;
        // Field FullName to FieldSetting FullName
        private static Dictionary<string, string> defaultFieldSettingTypeNames;
        // Field FullName to FieldControl FullName
        private static Dictionary<string, string> defaultFieldControlTypeNames;
        // Field FullName to DataSlotInfo
        private static Dictionary<string, List<DataSlotInfo>> dataSlots;
        private static Dictionary<string, Type> fieldDataTypes;

        private static Dictionary<string, Type> FieldTypes
        {
            get
            {
                if (!initialized)
                    Initialize();
                return fieldTypes;
            }
        }
        private static Dictionary<string, Type> FieldSettingTypes
        {
            get
            {
                if (!initialized)
                    Initialize();
                return fieldSettingTypes;
            }
        }
        public static Dictionary<string, string> FieldShortNamesFullNames
        {
            get
            {
                if (!initialized)
                    Initialize();
                return fieldShortNamesFullNames;
            }
        }
        private static Dictionary<string, string> DefaultFieldSettingTypesNames
        {
            get
            {
                if (!initialized)
                    Initialize();
                return defaultFieldSettingTypeNames;
            }
        }
        public static Dictionary<string, string> DefaultFieldControlTypeNames
        {
            get
            {
                if (!initialized)
                    Initialize();
                return defaultFieldControlTypeNames;
            }
        }
        private static Dictionary<string, List<DataSlotInfo>> DataSlots
        {
            get
            {
                if (!initialized)
                    Initialize();
                return dataSlots;
            }
        }
        private static Dictionary<string, Type> FieldDataTypes
        {
            get
            {
                if (!initialized)
                    Initialize();
                return fieldDataTypes;
            }
        }

        private static void Initialize()
        {
            lock (syncRoot)
            {
                if (!initialized)
                {
                    fieldShortNamesFullNames = new Dictionary<string, string>();
                    fieldTypes = new Dictionary<string, Type>();
                    fieldSettingTypes = new Dictionary<string, Type>();
                    defaultFieldSettingTypeNames = new Dictionary<string, string>();
                    defaultFieldControlTypeNames = new Dictionary<string, string>();
                    dataSlots = new Dictionary<string, List<DataSlotInfo>>();
                    fieldDataTypes = new Dictionary<string, Type>();

                    foreach (var type in TypeResolver.GetTypesByBaseType(typeof(Field)))
                    {
                        fieldTypes.Add(type.FullName, type);

                        // ShortName
                        ShortNameAttribute[] nameAttrs = (ShortNameAttribute[])type.GetCustomAttributes(typeof(ShortNameAttribute), false);
                        //TODO: Field.ShortName collision handling: make overridable from configuration
                        string shortName = (nameAttrs.Length > 0) ? nameAttrs[0].ShortName : GetShortNameFromTypeName(type);
                        fieldShortNamesFullNames.Add(shortName, type.FullName);

                        // DefaultFieldSetting
                        DefaultFieldSettingAttribute[] settingAttrs = (DefaultFieldSettingAttribute[])type.GetCustomAttributes(typeof(DefaultFieldSettingAttribute), false);
                        Type fieldSettingType = (settingAttrs.Length > 0) ? settingAttrs[0].FieldSettingType : typeof(NullFieldSetting);
                        defaultFieldSettingTypeNames.Add(type.FullName, fieldSettingType.FullName);

                        // DefaultFieldControl
                        DefaultFieldControlAttribute[] controlAttrs = (DefaultFieldControlAttribute[])type.GetCustomAttributes(typeof(DefaultFieldControlAttribute), false);
                        string controlTypeName = (controlAttrs.Length > 0) ? controlAttrs[0].FieldControlTypeName : null;
                        defaultFieldControlTypeNames.Add(type.FullName, controlTypeName);

                        // DataSlots
                        DataSlotAttribute[] slotAttrs = (DataSlotAttribute[])type.GetCustomAttributes(typeof(DataSlotAttribute), false);
                        List<DataSlotInfo> slotList = new List<DataSlotInfo>();
                        foreach (DataSlotAttribute attr in slotAttrs)
                            slotList.Add(new DataSlotInfo(attr.SlotIndex, attr.DataType, attr.AcceptedTypes));
                        slotList.Sort();
                        for (int i = 0; i < slotList.Count; i++)
                            if (slotList[i].SlotIndex != i)
                                throw new ApplicationException("Indices of DataSlots must be real zero based continuous sequence.");
                        dataSlots.Add(type.FullName, slotList);

                        // FieldDataTypes
                        FieldDataTypeAttribute[] fieldDataTypeAttrs = (FieldDataTypeAttribute[])type.GetCustomAttributes(typeof(FieldDataTypeAttribute), false);
                        Type dataType = null;
                        if (fieldDataTypeAttrs.Length > 0)
                        {
                            dataType = fieldDataTypeAttrs[0].DataType;
                        }
                        else if (slotList.Count > 0)
                        {
                            if (slotList[0].AcceptedTypes.Length > 0)
                                dataType = slotList[0].AcceptedTypes[0];
                        }
                        fieldDataTypes.Add(type.FullName, dataType);
                    }

                    foreach (var type in TypeResolver.GetTypesByBaseType(typeof(FieldSetting)))
                        fieldSettingTypes.Add(type.FullName, type);

                    initialized = true;
                }
            }
        }

        private static string GetShortNameFromTypeName(Type fieldType)
        {
            string name = fieldType.Name;
            if (name.ToLower().EndsWith("field"))
                return name.Substring(0, name.Length - 5);
            return name;
        }

        internal static string GetFieldHandlerName(string shortName)
        {
            string handlerName;
            if (!FieldShortNamesFullNames.TryGetValue(shortName, out handlerName))
                throw new NotSupportedException(String.Concat(SR.Exceptions.Registration.Msg_UnknownFieldType, ": '", shortName, "'"));
            return handlerName;
        }
        internal static Type[] GetFieldTypes()
        {
            Type[] types = new Type[FieldTypes.Count];
            FieldTypes.Values.CopyTo(types, 0);
            return types;
        }
        internal static string GetDefaultFieldSettingTypeName(string fieldClassName)
        {
            string typeName = DefaultFieldSettingTypesNames[fieldClassName];
            return typeName;
        }
        internal static RepositoryDataType[] GetDataTypes(string fieldShortName)
        {
            List<DataSlotInfo> slots = DataSlots[FieldShortNamesFullNames[fieldShortName]];
            RepositoryDataType[] result = new RepositoryDataType[slots.Count];
            for (int i = 0; i < slots.Count; i++)
                result[i] = slots[i].DataType;
            return result;
        }
        internal static Type[][] GetHandlerSlots(string fieldShortName)
        {
            List<DataSlotInfo> slots = DataSlots[FieldShortNamesFullNames[fieldShortName]];
            Type[][] result = new Type[slots.Count][];
            for (int i = 0; i < slots.Count; i++)
                result[i] = slots[i].AcceptedTypes;
            return result;
        }
        internal static int GetCountOfFieldBindings(string fieldTypeName)
        {
            List<DataSlotInfo> slots = DataSlots[fieldTypeName];
            return slots.Count;
        }
        internal static Type GetFieldDataType(string fieldClassName)
        {
            var typeName = FieldDataTypes[fieldClassName];
            return typeName;
        }
        internal static IEnumerable<Type> GetAvailableFieldTypes(Type handlerSlotType)
        {
            var types = from key in DataSlots.Keys
                        from slot in DataSlots[key]
                        where slot.AcceptedTypes.Contains(handlerSlotType)
                        select FieldTypes[key];
            return types;
        }
        internal static Type GetSuggestedFieldType(Type handlerSlotType)
        {
            if (handlerSlotType == typeof(bool) || handlerSlotType == typeof(bool?))
                return typeof(BooleanField);
            if (handlerSlotType == typeof(int) || handlerSlotType == typeof(int?))
                return typeof(IntegerField);
            if (handlerSlotType == typeof(string))
                return typeof(ShortTextField);
            if (handlerSlotType == typeof(DateTime) || handlerSlotType == typeof(DateTime?))
                return typeof(DateTimeField);
            if (handlerSlotType == typeof(decimal) || handlerSlotType == typeof(decimal?))
                return typeof(NumberField);
            if (handlerSlotType == typeof(double) || handlerSlotType == typeof(double?))
                return typeof(NumberField);
            if (handlerSlotType == typeof(VersionNumber))
                return typeof(VersionField);
            if (handlerSlotType == typeof(BinaryData))
                return typeof(BinaryField);
            if (typeof(Enum).IsAssignableFrom(handlerSlotType))
                return typeof(ChoiceField);
            if (typeof(Node).IsAssignableFrom(handlerSlotType))
                return typeof(ReferenceField);
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(handlerSlotType))
            {
                if (handlerSlotType.IsGenericType)
                {
                    var genTypes = handlerSlotType.GetGenericArguments();
                    if (genTypes.Length == 1)
                    {
                        var genType = genTypes[0];
                        if (typeof(Node).IsAssignableFrom(genType))
                            return typeof(ReferenceField);
                        if (genType == typeof(string))
                            return typeof(ChoiceField);
                    }
                }
            }
            return null;
        }
        internal static string GetShortName(string fieldClassName)
        {
            foreach (var shortName in fieldShortNamesFullNames.Keys)
                if (fieldShortNamesFullNames[shortName] == fieldClassName)
                    return shortName;
            return null;
        }

        internal static Field CreateField(string typeName)
        {
            Type type;
            if (!FieldTypes.TryGetValue(typeName, out type))
                throw new ApplicationException(String.Concat(SR.Exceptions.Registration.Msg_UnknownFieldType, ": ", typeName));
            return (Field)Activator.CreateInstance(type);
        }
        internal static FieldSetting CreateFieldSetting(string typeName)
        {
            Type type;
            if (!FieldSettingTypes.TryGetValue(typeName, out type))
                throw new ApplicationException(String.Concat(SR.Exceptions.Registration.Msg_UnknownFieldSettingType, ": ", typeName));
            return (FieldSetting)Activator.CreateInstance(type);
        }

    }
}