using System;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using Formatting = Newtonsoft.Json.Formatting;

namespace SenseNet.ContentRepository.Fields
{  
    [ShortName("Sharing")]
    [DataSlot(0, RepositoryDataType.Text, typeof(SharingHandler))]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.ShortText")]
    public class SharingField : Field
    {
        internal sealed class IdToPathConverter : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanWrite => true;
            public override bool CanConvert(Type type) => type == typeof(int);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // This assumes that every int property of SharingData is an identity.
                var id = (int)value;
                var head = NodeHead.Get(id);
                if (head != null)
                    writer.WriteValue(head.Path);
                else
                    writer.WriteValue(id);
            }

            public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }

        // Customize properties to let the system export and import sharing data.

        protected override bool HasExportData => true;
        public override bool ReadOnly => Name != "Sharing";

        public override object GetData()
        {
            // this cannot be null as it would prevent the system exporting the field
            return string.Empty;
        }
        public override void SetData(object value)
        {
            // Do nothing, sharing info is edited only through the sharing API 
            // or the ImportData method of this class.
        }

        protected override void ExportData(XmlWriter writer, ExportContext context)
        {
            // restriction: only admins are allowed to export sharing information
            if (!IsExportImportAllowed())
                return;

            if (!(Content.ContentHandler is GenericContent gc))
                return;

            var sharingData = gc.SharingData;
            if (string.IsNullOrEmpty(sharingData))
                return;

            // In case of export mode: convert identifiers to path to let 
            // other systems import sharing data correctly.
            var wm = RepositoryEnvironment.WorkingMode;
            if (wm.Exporting)
                ExportDataPath(writer, sharingData);
            else
                writer.WriteString(sharingData);
        }
        private void ExportDataPath(XmlWriter writer, string sharingData)
        {
            // Convert all ids to path in case of identity properties
            // to let other repository instances import sharing data.

            var sharingItems = SharingHandler.Deserialize(sharingData);
            var settings = new JsonSerializerSettings
            {
                Converters = { new IdToPathConverter() },
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented
            };

            writer.WriteString(JsonConvert.SerializeObject(sharingItems, settings));
        }

        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            // restriction: only admins are allowed to import sharing information
            if (!IsExportImportAllowed())
                return;

            if (!(Content.ContentHandler is GenericContent gc))
                return;

            var sharingData = fieldNode?.InnerText.Trim();

            gc.SharingData = DeserializeAndConvertPathToId(sharingData);
        }

        private string DeserializeAndConvertPathToId(string sharingData)
        {
            if (string.IsNullOrEmpty(sharingData))
                return sharingData;

            bool ConvertPathToId(JObject sharingObject, string propertyName)
            {
                // deal with strings only
                if (sharingObject[propertyName].Type != JTokenType.String)
                    return false;

                var identityPath = sharingObject[propertyName].Value<string>();
                var identityId = 0;
                var head = NodeHead.Get(identityPath);

                if (head == null)
                {
                    // identity not found, it will be  set to 0
                    SnTrace.Security.Write($"Sharing import error: identity not found: {identityPath}");
                }
                else
                {
                    identityId = head.Id;
                }

                var property = sharingObject.Property(propertyName);
                property.Value = identityId;

                return true;

            }

            var modified = false;
            var sharingArray = (JArray)JsonConvert.DeserializeObject(sharingData);
            foreach (var sharingObject in sharingArray)
            {
                modified |= ConvertPathToId((JObject)sharingObject, "Identity");
                modified |= ConvertPathToId((JObject)sharingObject, "CreatorId");
            }

            return modified ? JsonConvert.SerializeObject(sharingArray) : sharingData;
        }

        private bool IsExportImportAllowed()
        {
            // Only administrators are allowed to export/import sharing data
            // and it can be done only through the built-in Sharing field.
            return (User.Current.Id == Identifiers.SystemUserId ||
                   User.Current.IsInGroup(Identifiers.AdministratorsGroupId)) &&
                Name == "Sharing";
        }
    }
}