using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("PermissionChoice")]
    [DataSlot(0, RepositoryDataType.String, typeof(IEnumerable<PermissionType>), typeof(string))]
    [DefaultFieldSetting(typeof(PermissionChoiceFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.DropDown")]
    public class PermissionChoiceField : ChoiceField
    {
        protected override bool HasExportData { get { return true; } }

        protected override object ConvertTo(object[] handlerValues)
        {
            // output: string[]

            if (handlerValues[0] == null)
                return null;

            var stringEnumerableValue = handlerValues[0] as IEnumerable<string>;
            if (stringEnumerableValue != null)
                return stringEnumerableValue.ToArray();

            var permissionTypeEnumerableValue = handlerValues[0] as IEnumerable<PermissionType>;
            if (permissionTypeEnumerableValue != null)
                return permissionTypeEnumerableValue.Select(p => p.Name).ToArray();

            var stringValue = handlerValues[0] as string;
            if (stringValue != null)
            {
                if (IsMask(stringValue))
                    return ConvertToNameArray(GetPermissionTypesByMask(stringValue));

                var stringArray = GetStringArrayFromString(stringValue);
                if (stringArray != null)
                {
                    var intEnumerable = GetIntArrayFromStringArray(stringArray);
                    if (intEnumerable != null)
                        return ConvertToNameArray(MigrateOldValues(intEnumerable));

                    return stringArray;
                }
                throw new NotSupportedException(String.Format("Cannot parse the value of a PermissionChoiceField: "
                    , stringValue.Length > 100 ? stringValue.Substring(0, 100) + "..." : stringValue));
            }

            throw new NotSupportedException(String.Format("Instance of {0} is not supported int the PermissionChoiceField.", handlerValues[0].GetType().FullName));
        }
        private static bool IsMask(string value)
        {
            if (value.Length != PermissionType.PermissionMaxCount)
                return false;
            foreach (var @char in value)
                if (@char != '_' && @char != '*')
                    return false;
            return true;
        }
        private static string[] GetStringArrayFromString(string stringValue)
        {
            if (stringValue.Length == 0)
                return new string[0];

            var array = stringValue.Split(ChoiceField.SplitChars, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            return array;
        }
        private static int[] GetIntArrayFromStringArray(string[] stringArray)
        {
            var result = new int[stringArray.Length];
            int intValue;
            for (int i = 0; i < stringArray.Length; i++)
            {
                if (!int.TryParse(stringArray[i], out intValue))
                    return null;
                result[i] = intValue;
            }
            return result;
        }

        protected override object[] ConvertFrom(object value)
        {
            return new object[] { ConvertFromControlInner(value) };
        }
        private object ConvertFromControlInner(object value)
        {
            var permissionTypes = ConvertToPermissionTypes(value);
            var newValue = (GetHandlerSlot(0) == typeof(string)) ? (object)ConvertToNames(permissionTypes) : permissionTypes;
            return newValue;
        }

        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            writer.WriteString(GetXmlData());
        }
        protected override void ExportData2(System.Xml.XmlWriter writer, ExportContext context)
        {
            ExportData(writer, context);
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            string value = fieldNode.InnerXml;
            if (value.Trim() == String.Empty)
                return;
            this.SetData(value);
        }
        protected override string GetXmlData()
        {
            object data = GetData();
            var permissionTypes = ConvertToPermissionTypes(data);
            return ConvertToNames(permissionTypes);
        }
        protected override void WriteXmlData(System.Xml.XmlWriter writer)
        {
            ExportData(writer, null);
        }

        // ---------------------------------------------------------------------------------------

        public static IEnumerable<PermissionType> ConvertToPermissionTypes(object value)
        {
            if (value == null)
                return new PermissionType[0];

            var permissionTypeEnumerableValue = value as IEnumerable<PermissionType>;
            if (permissionTypeEnumerableValue != null)
                return permissionTypeEnumerableValue;

            var stringEnumerableValue = value as IEnumerable<string>;
            if (stringEnumerableValue != null)
            {
                var intEnumerable = GetIntArrayFromStringArray(stringEnumerableValue.ToArray());
                if (intEnumerable != null)
                    return MigrateOldValues(intEnumerable);
                return GetPermissionTypesByNames(stringEnumerableValue);
            }

            var intEnumerableValue = value as IEnumerable<int>;
            if (intEnumerableValue != null)
                return MigrateOldValues(intEnumerableValue);

            var stringValue = value as string;
            if (stringValue != null)
            {
                if (IsMask(stringValue))
                    return GetPermissionTypesByMask(stringValue);

                var stringArray = GetStringArrayFromString(stringValue);
                if (stringArray != null)
                {
                    var intEnumerable = GetIntArrayFromStringArray(stringArray);
                    if (intEnumerable != null)
                        return MigrateOldValues(intEnumerable);

                    return GetPermissionTypesByNames(stringArray);
                }
                throw new NotSupportedException(string.Format("Cannot parse the value of a PermissionChoiceField: {0}",
                    stringValue.Length > 100 ? stringValue.Substring(0, 100) + "..." : stringValue));
            }

            throw new NotSupportedException();
        }

        public static string[] ConvertToNameArray(IEnumerable<PermissionType> permissionTypes)
        {
            return permissionTypes.Select(p => p.Name).ToArray();
        }
        public static string ConvertToNames(IEnumerable<PermissionType> permissionTypes)
        {
            return String.Join(",", ConvertToNameArray(permissionTypes));
        }
        public static string ConvertToMask(IEnumerable<PermissionType> permissionTypes)
        {
            var permCount = PermissionType.PermissionMaxCount;
            var chars = (new String('_', permCount)).ToCharArray();
            foreach (var permType in permissionTypes)
                chars[permCount - permType.Index - 1] = '*';
            var newValue = new String(chars);
            return newValue;
        }
        public static IEnumerable<PermissionType> GetPermissionTypesByMask(string maskValue)
        {
            var result = new List<PermissionType>();
            var length = maskValue.Length;
            for (int i = maskValue.Length - 1; i >= 0; i--)
                if (maskValue[i] != '_')
                    result.Add(PermissionType.GetByIndex(length - i - 1));
            return result;
        }
        public static IEnumerable<PermissionType> GetPermissionTypesByNames(IEnumerable<string> names)
        {
            return names.Select(n => PermissionType.GetByName(n)).ToArray();
        }
        public static IEnumerable<PermissionType> MigrateOldValues(IEnumerable<int> intValues)
        {
            var result = intValues.Select(i => PermissionType.GetByIndex(_migrationTable[i])).ToArray();
            return result;
        }

        private static readonly int[] _migrationTable = new[]
        {
            -1,  //  0: Not used (invalid) value
             0,  //  1: See
             4,  //  2: Open
             5,  //  3: OpenMinor
             6,  //  4: Save
             7,  //  5: Publish
             8,  //  6: ForceCheckin
             9,  //  7: AddNew
            10,  //  8: Approve
            11,  //  9: Delete
            12,  // 10: RecallOldVersion
            13,  // 11: DeleteOldVersion
            14,  // 12: SeePermissions
            15,  // 13: SetPermissions
            16,  // 14: RunApplication
            17,  // 15: ManageListsAndWorkspaces
             1,  // 16: Preview
             2,  // 17: PreviewWithoutWatermark
             3,  // 18: PreviewWithoutRedaction
            32,  // 19: Custom01
            33,  // 20: Custom02
            34,  // 21: Custom03
            35,  // 22: Custom04
            36,  // 23: Custom05
            37,  // 24: Custom06
            38,  // 25: Custom07
            39,  // 26: Custom08
            40,  // 27: Custom09
            41,  // 28: Custom10
            42,  // 29: Custom11
            43,  // 30: Custom12
            44,  // 31: Custom13
            45,  // 32: Custom14
        };

    }
}
