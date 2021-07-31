using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData.Writers;

namespace SenseNet.OData
{
    // No metadata. $export and $select are irrelevant
    internal class ExportProjector : Projector
    {
        internal override void Initialize(Content container)
        {
            // do nothing
        }
        internal override ODataEntity Project(Content content, HttpContext httpContext)
        {
            var fields = new ODataEntity();
            var selfurl = GetSelfUrl(content);

            var fieldNames = content.Fields.Keys;

            foreach (var fieldName in fieldNames)
            {
                if (ODataMiddleware.DisabledFieldNames.Contains(fieldName))
                    continue;

                if (IsAllowedField(content, fieldName))
                {
                    if (content.Fields.TryGetValue(fieldName, out var field))
                        fields.Add(fieldName, GetJsonObject(field, selfurl, Request));
                    else if (fieldName == ICONPROPERTY)
                        fields.Add(fieldName, content.Icon ?? content.ContentType.Icon);
                    else
                        fields.Add(fieldName, null);
                }
            }
            return fields;
        }

        protected override bool IsAllowedField(Content content, string fieldName)
        {
            switch (fieldName)
            {
                case "EffectiveAllowedChildTypes":
                case "TypeIs":
                case "InTree":
                case "InFolder":
                case "SavingState":
                case ACTIONSPROPERTY:
                case ICONPROPERTY:
                case ODataMiddleware.ChildrenPropertyName:
                    return false;
                default:
                    return base.IsAllowedField(content, fieldName);
            }
        }


        protected override object GetJsonObject(Field field, string selfUrl, ODataRequest oDataRequest)
        {
            if(field is AllowedChildTypesField actField)
                return GetAllowedChildTypes(actField, selfUrl, oDataRequest);
            if (field is ReferenceField refField)
                return GetReference(refField, selfUrl, oDataRequest);
            return base.GetJsonObject(field, selfUrl, oDataRequest);
        }

        private object GetAllowedChildTypes(AllowedChildTypesField field, string selfUrl, ODataRequest oDataRequest)
        {
            var value = field.GetData();

            if (value == null)
                return null;
            if (value is Node node)
                return new[] {node.Name};
            if (value is IEnumerable<Node> nodes)
                return nodes.Where(n => n != null).Select(n => n.Name).ToArray();

            throw new NotSupportedException();
        }

        private object GetReference(ReferenceField field, string selfUrl, ODataRequest oDataRequest)
        {
            var value = field.GetData();

            if (value == null)
                return null;
            if (value is Node node)
                return node.Path;
            if (value is IEnumerable<Node> nodes)
                return nodes.Where(n => n != null).Select(n => n.Path).ToArray();

            throw new NotSupportedException();
        }
    }
}
