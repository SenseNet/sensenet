using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using SenseNet.Portal.Virtualization;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class WebApplication : SnConfig
    {
        private const string SectionName = "sensenet/webApplication";

        public static bool SignalRSqlEnabled { get; internal set; } = GetValue<bool>(SectionName, "SignalRSqlEnabled");

        public static string[] JsBundlingBlacklist { get; internal set; } = GetList<string>(SectionName, "JsBundlingBlacklist", new List<string>
            {
                "/Root/Global/scripts/tinymce/",
                "/Root/Global/scripts/jquery/plugins/tree/"
            }).ToArray();

        public static string[] CssBundlingBlacklist { get; internal set; } = GetListOrEmpty<string>(SectionName, "CssBundlingBlacklist").ToArray();

        public static bool AllowCssBundling { get; internal set; } = GetValue<bool>(SectionName, "AllowCssBundling", true);
        public static bool AllowJsBundling { get; internal set; } = GetValue<bool>(SectionName, "AllowJsBundling", true);

        public static List<string> ProxyIPs { get; internal set; } = GetListOrEmpty<string>(SectionName, "ProxyIP");

        public static int PurgeUrlDelayInMilliSeconds { get; internal set; } =
            GetInt(SectionName, "PurgeUrlDelayInSeconds", 0) * 1000;

        public static string[] EditSourceExtensions { get; internal set; } = GetListOrEmpty<string>(SectionName, "EditSourceExtensions", 
                new List<string>(new []
                {
                    ".ascx", ".asmx", ".eml", ".config", ".css", ".js", ".xml", ".xaml", ".html", ".htm", ".aspx",
                    ".template", ".xslt", ".txt", ".ashx", ".settings", ".cshtml", ".json", ".vbhtml"
                })).ToArray();

        // use the same extension list as in webdav, plus pdf
        public static string[] DownloadExtensions { get; internal set; } = Webdav.WebdavEditExtensions
            .Union(new[] {".pdf"}, StringComparer.InvariantCultureIgnoreCase)
            .ToArray();

        public static bool DenyCrossSiteAccessEnabled { get; internal set; } = GetValue<bool>(SectionName, "DenyCrossSiteAccessEnabled", true);

        public static bool GlobaFieldControlTemplateEnabled { get; internal set; } = GetValue<bool>(SectionName, "GlobaFieldControlTemplateEnabled", true);

        public static string ScriptMode { get; internal set; } = GetString(SectionName, "ScriptMode", "Release");
        public static int ContentPickerRowNum { get; internal set; } = GetInt(SectionName, "SNPickerRowNum", 20);
        public static int ReferenceGridRowNum { get; internal set; } = GetInt(SectionName, "SNReferenceGridRowNum", 5);

        public static string DefaultAuthenticationMode { get; internal set; } =
            GetString(SectionName, "DefaultAuthenticationMode", "Forms");

        public static string CacheFolderFileSystemPath { get; internal set; } =
            GetString(SectionName, "CacheFolderFileSystemPath");

        public static List<string> WebRootFiles { get; internal set; } = GetListOrEmpty<string>(SectionName, "WebRootFiles")
            .Union(new[]
            {
                "binaryhandler.ashx",
                "Explore.html",
                "ExploreFrame.html",
                "ExploreTree.aspx",
                "picker.aspx",
                "portlet-preview.aspx",
                "prc.ascx",
                "tinyproxy.ashx",
                "UploadProxy.ashx",
                "vsshandler.ashx"
            }).Select(f => string.Concat("/", f).ToLower()).Distinct().ToList();

        public static DiskFSSupportMode DiskFSSupportMode { get; internal set; } = GetValue<DiskFSSupportMode>(SectionName, "DiskFSSupportMode", DiskFSSupportMode.Fallback);


        public static string WebContentNameList { get; internal set; } = GetString(SectionName, "WebContentNameList", "WebContent");
        public static bool ShowErrorDetails { get; internal set; } = GetValue<bool>(SectionName, "ShowErrorDetails");

        public static IReadOnlyDictionary<string, ErrorStatusCode> ExceptionStatusCodes { get; internal set; } =
            GetExceptionStatusCodes();

        private static IReadOnlyDictionary<string, ErrorStatusCode> GetExceptionStatusCodes()
        {
            // Start with the built-in list of status codes. The items in that list
            // can be overridden from configuration.

            // This default list cannot be a static class member, because it would be NULL
            // at this point, because currently we are in a static property initializer.
            var statusCodes = new Dictionary<string, ErrorStatusCode>
            {
                { "System.Security.SecurityException", new ErrorStatusCode(403) },
                { "System.Security.Policy.PolicyException", new ErrorStatusCode(403, 1) },
                { "System.Security.AccessControl.PrivilegeNotHeldException", new ErrorStatusCode(403, 2) },
                { "System.UnauthorizedAccessException", new ErrorStatusCode(403, 3) },
                { "System.Security.HostProtectionException", new ErrorStatusCode(403, 8) },
                { "System.Runtime.Remoting.RemotingTimeoutException", new ErrorStatusCode(403, 9) },
                { "System.NotSupportedException", new ErrorStatusCode(404) }
            };

            var statusCodeSection = ConfigurationManager.GetSection("sensenet/exceptionStatusCodes") as NameValueCollection;
            if (statusCodeSection != null)
            {
                foreach (var errorType in statusCodeSection.AllKeys.Where(et => !string.IsNullOrEmpty(et)))
                {
                    var statusCode = ErrorStatusCode.Parse(statusCodeSection[errorType]);
                    if(statusCode == null)
                        continue;

                    statusCodes[errorType] = statusCode;
                }
            }

            return new ReadOnlyDictionary<string, ErrorStatusCode>(statusCodes);
        }

        /// <summary>
        /// Gets a strongly typed value (for example enums that are not available in the lower layers) 
        /// from the current section from configuration.
        /// </summary>
        public static T GetValue<T>(string key, T defaultValue = default(T))
        {
            return GetValue<T>(SectionName, key, defaultValue);
        }
    }
}
