using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Wopi
{
    public static class WopiActions
    {
        private static readonly TimeSpan DefaultTokenTimeout = TimeSpan.FromHours(3);

        /// <summary></summary>
        /// <snCategory>Office Online Editing</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [ODataFunction]
        [RequiredPermissions(N.P.Open)]
        public static object GetWopiData(Content content, HttpContext context, string action)
        {
            if (!(content.ContentHandler is File))
                throw new SnNotSupportedException("Office Online is not supported for this type of content.");

            var officeOnlineUrl = Settings.GetValue("OfficeOnline", "OfficeOnlineUrl", content.Path, string.Empty);
            if (string.IsNullOrEmpty(officeOnlineUrl))
                throw new SnNotSupportedException("Office Online Server setting not found.");

            var wd = WopiDiscovery.GetInstance(officeOnlineUrl);
            if (wd == null || !wd.Zones.Any())
                throw new SnNotSupportedException("Office Online Server not found.");

            //TODO: handle internal or external zone urls
            var wopiApp = wd.Zones["internal-https"]?.GetApp(content.Name, action);
            var wopiAction = wopiApp?.Actions.GetAction(action, content.Name);
            if (wopiAction == null)
                throw new SnNotSupportedException($"Office Online action '{action}' is not supported on this content.");

            // load an existing token or create a new one
            var token = AccessTokenVault.GetOrAddToken(User.Current.Id, DefaultTokenTimeout, content.Id,
                WopiMiddleware.AccessTokenFeatureName);

            var expiration = Math.Truncate((token.ExpirationDate - new DateTime(1970, 1, 1).ToUniversalTime()).TotalMilliseconds);

            return new Dictionary<string, object>
            {
                { "accesstoken", token.Value },
                { "expiration", expiration },
                { "actionUrl", TransformUrl(wopiAction.UrlSrc, content.Id, context) },
                { "faviconUrl", wopiApp.FaviconUrl }
            };
        }

        /// <summary></summary>
        /// <snCategory>Office Online Editing</snCategory>
        /// <param name="content"></param>
        /// <returns></returns>
        [ODataFunction(Icon = "office", DisplayName = "$Action,WopiOpenView-DisplayName")]
        [RequiredPermissions(N.P.Open)]
        [RequiredPolicies("WopiOpenView")]
        [Scenario(N.S.ContextMenu)]
        public static object WopiOpenView(Content content)
        {
            // This method serves only action listing and will not actually execute.
            return null;
        }

        /// <summary></summary>
        /// <snCategory>Office Online Editing</snCategory>
        /// <param name="content"></param>
        /// <returns></returns>
        [ODataFunction(Icon = "office", DisplayName = "$Action,WopiOpenEdit-DisplayName")]
        [RequiredPermissions(N.P.Save)]
        [RequiredPolicies("WopiOpenEdit")]
        [Scenario(N.S.ContextMenu)]
        public static object WopiOpenEdit(Content content)
        {
            // This method serves only action listing and will not actually execute.
            return null;
        }

        private static string TransformUrl(string url, int contentId, HttpContext context)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            var queryIndex = url.IndexOf("?", StringComparison.Ordinal);
            if (queryIndex < 0)
                return url;

            var query = url.Substring(queryIndex + 1);
            var regex = new Regex("<(?<pname>\\w+)=(?<pvalue>\\w+)[&]{0,1}>");

            // hardcoded secure schema
            var wopiSrcUrl = $"https://{context.Request.Host}/wopi/files/{contentId}";

            var newQueryParams = new StringBuilder();

            newQueryParams.AppendFormat($"WOPISrc={HttpUtility.UrlEncode(wopiSrcUrl)}&");

            foreach (Match match in regex.Matches(query))
            {
                switch (match.Groups["pvalue"].Value.Trim('&'))
                {
                    case "BUSINESS_USER":
                        newQueryParams.AppendFormat($"{match.Groups["pname"].Value}=0&");
                        break;
                    case "DC_LLCC":
                        newQueryParams.AppendFormat(
                            $"{match.Groups["pname"].Value}={CultureInfo.CurrentUICulture.Name}&");
                        break;
                    case "UI_LLCC":
                        newQueryParams.AppendFormat(
                            $"{match.Groups["pname"].Value}={CultureInfo.CurrentUICulture.Name}&");
                        break;
                    case "DISABLE_CHAT":
                        newQueryParams.AppendFormat($"{match.Groups["pname"].Value}=1&");
                        break;
                }
            }

            return url.Substring(0, queryIndex + 1) + newQueryParams.ToString().TrimEnd('&');
        }
    }    
}
