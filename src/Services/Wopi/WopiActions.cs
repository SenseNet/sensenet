using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.Services.Wopi
{
    public static class WopiActions
    {
        [ODataFunction]
        public static object GetWopiData(Content content, string action)
        {
            if (!(content.ContentHandler is File))
                throw new SnNotSupportedException("Office Online is not supported for this type of content.");

            var wopiServerUrl = Settings.GetValue("OfficeOnline", "WopiServerUrl", content.Path, string.Empty);
            if (string.IsNullOrEmpty(wopiServerUrl))
                throw new SnNotSupportedException("Office Online Server setting not found.");

            var wd = WopiDiscovery.GetInstance(wopiServerUrl);
            if (wd == null || !wd.Zones.Any())
                throw new SnNotSupportedException("Office Online Server not found.");

            //UNDONE: handle internal or external zone urls
            var wopiApp = wd.Zones["internal-https"]?.GetApp(content.Name, action);
            var wopiAction = wopiApp?.Actions.GetAction(action, content.Name);
            if (wopiAction == null)
                throw new SnNotSupportedException($"Office Online action '{action}' is not supported on this content.");
            
            //UNDONE: load or create new tokens here?
            //UNDONE: use Wopi feature name constant when available
            var token = AccessTokenVault.GetTokens(User.Current.Id).FirstOrDefault(t =>
                            t.Feature == "Wopi" && t.ContentId == content.Id &&
                            t.ExpirationDate > DateTime.UtcNow.AddMinutes(1)) ??
                        AccessTokenVault.CreateToken(User.Current.Id, TimeSpan.FromHours(3), content.Id, "Wopi");

            var expiration = Math.Truncate((token.ExpirationDate - new DateTime(1970, 1, 1).ToUniversalTime()).TotalMilliseconds);

            return new Dictionary<string, object>
            {
                { "accesstoken", token.Value },
                { "expiration", expiration },
                { "actionUrl", TransformUrl(wopiAction.UrlSrc, content.Id) },
                { "faviconUrl", wopiApp.FaviconUrl }
            };
        }
        
        private static string TransformUrl(string url, int contentId)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            var queryIndex = url.IndexOf("?", StringComparison.Ordinal);
            if (queryIndex < 0)
                return url;

            var query = url.Substring(queryIndex + 1);
            var regex = new Regex("<(?<pname>\\w+)=(?<pvalue>\\w+)[&]{0,1}>");
            var wopiSrcUrl = HttpContext.Current.Request.Url.GetComponents(
                UriComponents.SchemeAndServer, UriFormat.Unescaped) + "/wopi/files/" + contentId;

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

    internal class WopiDiscovery
    {
        private static readonly ConcurrentDictionary<string, Lazy<WopiDiscovery>> Instances =
            new ConcurrentDictionary<string, Lazy<WopiDiscovery>>();

        internal static WopiDiscovery GetInstance(string wopiServerUrl)
        {
            return Instances.GetOrAdd(wopiServerUrl.TrimEnd('/'), oosUrl => new Lazy<WopiDiscovery>(() =>
            {
                var discoveryXml = new XmlDocument();

                Retrier.Retry(3, 500, typeof(Exception), () =>
                {
                    using (var client = new HttpClient())
                    {
                        using (var discoveryStream = client.GetAsync($"{oosUrl}/hosting/discovery")
                            .Result.Content.ReadAsStreamAsync().Result)
                        {
                            discoveryXml.Load(discoveryStream);
                        }
                    }
                });

                if (discoveryXml.DocumentElement == null)
                {
                    SnLog.WriteWarning($"Could not connect to Office Online Server {oosUrl} for available actions.");
                }

                return FromXmlDocument(discoveryXml);
            })).Value;
        }

        internal class WopiZone
        {
            public string Name { get; set; }
            internal WopiAppCollection Apps { get; set; } = new WopiAppCollection();

            internal WopiZone(string name)
            {
                Name = name ?? "unknown";
            }

            internal WopiApp GetApp(string fileName, string actionName)
            {
                var extension = System.IO.Path.GetExtension(fileName)?.Trim('.');

                foreach (var app in Apps)
                {
                    // if there is an action that fullfills the criteria
                    if (app.Actions.FirstOrDefault(act => 
                            act.Name == actionName && 
                            act.Extension == extension) != null)
                        return app;
                }

                return null;
            }

            internal static WopiZone FromXmlNode(XmlNode zoneNode)
            {
                if (zoneNode == null)
                    return null;

                var wopiZone = new WopiZone(zoneNode.Attributes?["name"]?.Value);

                wopiZone.Apps.AddRange(zoneNode.SelectNodes("app")?.Cast<XmlNode>().Where(an => an != null)
                                      .Select(WopiApp.FromXmlNode) ?? new WopiApp[0]);

                return wopiZone;
            }
        }
        internal class WopiZoneCollection : List<WopiZone>
        {
            public WopiZone this[string name]
            {
                get { return this.FirstOrDefault(z => z.Name == name); }
            }
        }

        internal class WopiApp
        {
            public string Name { get; set; }
            public string FaviconUrl { get; set; }
            internal WopiActionCollection Actions { get; set; } = new WopiActionCollection();

            internal WopiApp(string name, string faviconUrl)
            {
                Name = name ?? string.Empty;
                FaviconUrl = faviconUrl ?? string.Empty;
            }

            internal static WopiApp FromXmlNode(XmlNode appNode)
            {
                if (appNode == null)
                    return null;

                var wopiApp = new WopiApp(
                    appNode.Attributes?["name"]?.Value, 
                    appNode.Attributes?["favIconUrl"]?.Value);

                wopiApp.Actions.AddRange(appNode.SelectNodes("action")?.Cast<XmlNode>().Where(actn => actn != null)
                                             .Select(WopiAction.FromXmlNode) ?? new WopiAction[0]);

                return wopiApp;
            }
        }
        internal class WopiAppCollection : List<WopiApp>
        {
            public WopiApp this[string name]
            {
                get { return this.FirstOrDefault(app => app.Name == name); }
            }
        }

        internal class WopiAction
        {
            public string Name { get; set; }
            public string Extension { get; set; }
            public string Requires { get; set; }
            public string UrlSrc { get; set; }

            internal WopiAction(string name, string extension, string requires, string urlSrc)
            {
                Name = name ?? string.Empty;
                Extension = extension ?? string.Empty;
                Requires = requires ?? string.Empty;
                UrlSrc = urlSrc ?? string.Empty;
            }

            internal static WopiAction FromXmlNode(XmlNode actionNode)
            {
                if (actionNode == null)
                    return null;

                return new WopiAction(
                    actionNode.Attributes?["name"]?.Value,
                    actionNode.Attributes?["ext"]?.Value,
                    actionNode.Attributes?["requires"]?.Value,
                    actionNode.Attributes?["urlsrc"]?.Value);
            }
        }
        internal class WopiActionCollection : List<WopiAction>
        {
            public WopiAction GetAction(string name, string fileName)
            {
                var extension = System.IO.Path.GetExtension(fileName)?.Trim('.').ToLowerInvariant();

                return this.FirstOrDefault(act => act.Name == name && act.Extension == extension);
            }
        }

        internal WopiZoneCollection Zones { get; set; } = new WopiZoneCollection();

        internal static WopiDiscovery FromXmlDocument(XmlDocument discoveryXml)
        {
            if (discoveryXml?.DocumentElement == null)
                return new WopiDiscovery();

            var wd = new WopiDiscovery();

            wd.Zones.AddRange(discoveryXml.DocumentElement?.SelectNodes("net-zone")?.Cast<XmlNode>().Where(zn => zn != null)
                                  .Select(WopiZone.FromXmlNode) ?? new WopiZone[0]);

            return wd;
        }
    }
}
