using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.OData;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.OData
{
    public class ODataTools
    {
        /// <summary>
        /// Returns the OData service url for the specified content. Eg.: /OData/Root/Sites/Default_Site('test.txt')"
        /// </summary>
        /// <param name="content">Requested content</param>
        /// <param name="escapeApostrophes">Replaces ' with escaped form so it can be used in javascript in form '(url)'</param>
        /// <returns></returns>
        public static string GetODataUrl(Content content, bool escapeApostrophes = false)
        {
            var url = string.Concat("/" + Configuration.Services.ODataServiceToken, content.ContentHandler.ParentPath, "('", content.Name, "')");
            return escapeApostrophes ? url.Replace("'", "\\'") : url;
        }
        /// <summary>
        /// Returns the OData service url for the specified content. Eg.: /OData/Root/Sites/Default_Site('test.txt')"
        /// </summary>
        /// <param name="path">Path of requested content</param>
        /// <param name="escapeApostrophes">Replaces ' with escaped form so it can be used in javascript in form '(url)'</param>
        /// <returns></returns>
        public static string GetODataUrl(string path, bool escapeApostrophes = false)
        {
            var parentPath = RepositoryPath.GetParentPath(path);

            // this is to make sure that the constructed odata url will be correct in case of Root
            if (string.IsNullOrEmpty(parentPath))
                parentPath = "/";

            var url = string.Concat("/" + Configuration.Services.ODataServiceToken, parentPath, "('", RepositoryPath.GetFileName(path), "')");
            return escapeApostrophes ? url.Replace("'", "\\'") : url;
        }
        /// <summary>
        /// Returns the OData service operation url for the specified content and operation name. Eg.: /OData/Root/Sites/Default_Site('test.txt')/MoveTo"
        /// </summary>
        /// <param name="content">Requested content</param>
        /// <param name="operationName">Name of requested operation</param>
        /// <param name="escapeApostrophes">Replaces ' with escaped form so it can be used in javascript in form '(url)'</param>
        /// <returns></returns>
        public static string GetODataOperationUrl(Content content, string operationName, bool escapeApostrophes = false)
        {
            return string.Concat(GetODataUrl(content, escapeApostrophes), "/", operationName);
        }
        /// <summary>
        /// Returns the OData service operation url for the specified content and operation name. Eg.: /OData/Root/Sites/Default_Site('test.txt')/MoveTo"
        /// </summary>
        /// <param name="path">Path of requested content</param>
        /// <param name="operationName">Name of requested operation</param>
        /// <param name="escapeApostrophes">Replaces ' with escaped form so it can be used in javascript in form '(url)'</param>
        /// <returns></returns>
        public static string GetODataOperationUrl(string path, string operationName, bool escapeApostrophes = false)
        {
            return string.Concat(GetODataUrl(path, escapeApostrophes), "/", operationName);
        }

        internal static IEnumerable<ActionBase> GetActions(Content content, ODataRequest request, HttpContext httpContext)
        {
            return ODataMiddleware.ActionResolver.GetActions(content, request?.Scenario, null, httpContext);
        }

        internal static IEnumerable<ODataActionItem> GetActionItems(Content content, ODataRequest request, HttpContext httpContext)
        {
            return GetActionsWithScenario(content, request, httpContext).Select(a => new ODataActionItem
            {
                Name = a.Action.Name,
                DisplayName = SNSR.GetString(a.Action.Text),
                Icon = a.Action.Icon,
                Index = a.Action.Index,
                Url = a.Action.Uri,
                Forbidden = a.Action.Forbidden,
                IsODataAction = a.Action.IsODataOperation,
                ActionParameters = a.Action.ActionParameters.Select(p => p.Name).ToArray(),
                Scenario = a.Scenario
            });
        }

        private struct ScenarioAction
        {
            public ActionBase Action { get; set; }
            public string Scenario { get; set; }
        }

        private static IEnumerable<ScenarioAction> GetActionsWithScenario(Content content, ODataRequest request, HttpContext httpContext)
        {
            var scenario = request?.Scenario;
            var actions = ActionFramework.GetActions(content, scenario, null, null, httpContext);

            return actions.Select(action => new ScenarioAction
            {
                Action = action,
                Scenario = scenario
            });
        }

        internal static ODataArray CreateODataArray(Type type, object[] data)
        {
            return CreateODataArray(type, data, false);
        }
        internal static ODataArray CreateODataArray(Type type, string data)
        {
            return CreateODataArray(type, data, true);
        }
        private static ODataArray CreateODataArray(Type type, object data, bool fromString)
        {
            var ctorParamType = fromString ? typeof(string) : typeof(object[]);
            var ctor = type.GetConstructors().Where(c =>
            {
                var p = c.GetParameters();
                if (p.Length != 1)
                    return false;
                return p[0].ParameterType == ctorParamType;
            }).FirstOrDefault();

            if (ctor == null)
                throw new MissingMemberException($"Missing constructor: {type.Name}(object[]).");

            return (ODataArray)ctor.Invoke(new [] { data });
        }
    }
}
