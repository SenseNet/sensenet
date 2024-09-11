﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.OData.IO;

namespace SenseNet.OData
{
    internal abstract class Projector
    {
        protected const string ACTIONSPROPERTY = "Actions";
        protected const string ICONPROPERTY = "Icon";
        protected const string ISFILEPROPERTY = "IsFile";

        protected ODataRequest Request { get; private set; }
        protected bool IsCollectionItem { get; private set; }

        internal static Projector Create(ODataRequest request, bool isCollectionItem, Content container = null)
        {
            Projector prj;
            if (request.IsExport)
                prj = new ExportProjector();
            else if (request.HasExpand)
                if (request.HasSelect)
                    prj = new ExpanderProjector();
                else
                    prj = new SimpleExpanderProjector();
            else
                prj = new SimpleProjector();
            prj.Request = request;
            prj.IsCollectionItem = isCollectionItem;
            prj.Initialize(container);
            return prj;
        }
        internal abstract void Initialize(Content container);
        internal abstract ODataEntity Project(Content content, HttpContext httpContext);

        protected string GetSelfUrl(Content content)
        {
            return string.Concat("/", Configuration.Services.ODataServiceToken, ODataMiddleware.GetEntityUrl(content.Path));
        }
        protected ODataSimpleMeta GetMetadata(Content content, string selfurl, MetadataFormat format, HttpContext httpContext)
        {
            var dynamicContent = content.ContentHandler is Content.RuntimeContentHandler;
            if (dynamicContent)
                selfurl = string.Empty;

            if (format == MetadataFormat.Minimal)
            {
                return new ODataSimpleMeta
                {
                    Uri = selfurl,
                    Type = content.ContentType.Name,
                };
            }

            if(dynamicContent)
                return new ODataFullMeta
                {
                    Uri = selfurl,
                    Type = content.ContentType.Name,
                    Actions = new ODataOperation[0],
                    Functions = new ODataOperation[0],
                };

            var snActions = ODataTools.GetActions(content, this.Request, httpContext).ToArray();

            return new ODataFullMeta
            {
                Uri = selfurl,
                Type = content.ContentType.Name,
                Actions = snActions.Where(a => a.CausesStateChange && a.IsODataOperation).Select(a => CreateOdataOperation(a, selfurl)).OrderBy(x => x.Title).ToArray(),
                Functions = snActions.Where(a => !a.CausesStateChange && a.IsODataOperation).Select(a => CreateOdataOperation(a, selfurl)).OrderBy(x => x.Title).ToArray(),
            };
        }
        private ODataOperation CreateOdataOperation(ActionBase a, string selfUrl)
        {
            return new ODataOperation
            {
                Title = SNSR.GetString(a.Text),
                Name = a.Name,
                OpId = ODataTools.GetOperationId(a.Name, a.ActionParameters),
                Target = string.Concat(selfUrl, "/", a.Name),
                Forbidden = a.Forbidden,
                Parameters = a.ActionParameters.Select(p => new ODataOperationParameter
                    {
                        Name = p.Name,
                        Type = ResolveODataParameterType(p.Type),
                        Required = p.Required 
                    }).ToArray()
            };
        }
        private string ResolveODataParameterType(Type type)
        {
            if (type == null)
                return null;

            if (type == typeof(string))
                return "string";
            if (type == typeof(int))
                return "int";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(DateTime))
                return "dateTime";

            if (type == typeof(string[]))
                return "string[]";
            if (type == typeof(int[]))
                return "int[]";

            return type.FullName;
        }

        protected virtual bool IsAllowedField(Content content, string fieldName)
        {
            switch (fieldName)
            {
                case ACTIONSPROPERTY:
                case ICONPROPERTY:
                case ISFILEPROPERTY:
                case ODataMiddleware.ChildrenPropertyName:
                    return true;
                default:
                    return content.IsAllowedField(fieldName);
            }
        }

        private static readonly string[] HeadOnlyExpandableFields = {"CreatedBy", "ModifiedBy"};
        protected bool IsHeadOnlyExpandableField(string name)
        {
            return HeadOnlyExpandableFields.Contains(name);
        }

        protected ODataActionItem[] GetActions(Content content, HttpContext httpContext)
        {
            return ODataTools.GetActionItems(content, this.Request, httpContext).ToArray();
        }

        protected virtual object GetJsonObject(Field field, string selfUrl, ODataRequest oDataRequest)
        {
            object data;
            if (field is ReferenceField)
            {
                return ODataReference.Create(String.Concat(selfUrl, "/", field.Name));
            }
            else if (field is BinaryField binaryField)
            {
                try
                {
                    // load binary fields only if the content is finalized
                    var binaryData = field.Content.ContentHandler.SavingState == ContentSavingState.Finalized
                        ? (BinaryData)binaryField.GetData()
                        : null;

                    return ODataBinary.Create(BinaryField.GetBinaryUrl(binaryField.Content.Id, binaryField.Name, binaryData?.Timestamp ?? default),
                        null, binaryData?.ContentType, null);
                }
                catch (Exception ex)
                {
                    SnTrace.System.WriteError(
                        $"Error accessing field {field.Name} of {field.Content.Path} with user {User.Current.Username}: " +
                        ex.Message);

                    return null;
                }
            }
            else if (ODataMiddleware.DeferredFieldNames.Contains(field.Name))
            {
                return ODataReference.Create(String.Concat(selfUrl, "/", field.Name));
            }
            try
            {
                data = field.GetData();
            }
            catch (SenseNetSecurityException)
            {
                // The user does not have access to this field (e.g. cannot load
                // a referenced content). In this case we serve a null value.
                data = null;

                SnTrace.Repository.Write("PERMISSION warning: user {0} does not have access to field '{1}' of {2}.", User.LoggedInUser.Username, field.Name, field.Content.Path);
            }

            if (data is NodeType nodeType)
                return nodeType.Name;
            if (data is RichTextFieldValue rtfValue)
                return GetRichTextOutput(field.Name, rtfValue, oDataRequest);
            return data;
        }
        protected virtual object GetRichTextOutput(string fieldName, RichTextFieldValue rtfValue, ODataRequest oDataRequest)
        {
            if (!oDataRequest.HasExpandedRichTextField)
                return rtfValue.Text;
            return oDataRequest.AllRichTextFieldExpanded || oDataRequest.ExpandedRichTextFields.Contains(fieldName)
                ? JsonConvert.SerializeObject(rtfValue)
                : rtfValue.Text;
        }
    }
}
