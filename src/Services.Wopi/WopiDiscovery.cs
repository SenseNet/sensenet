using SenseNet.Diagnostics;
using SenseNet.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;

namespace SenseNet.Services.Wopi
{
    /// <summary>
    /// Technical class for holding WOPI (Office Online) server connections
    /// and discover available actions.
    /// </summary>
    internal class WopiDiscovery
    {
        private static readonly ConcurrentDictionary<string, Lazy<WopiDiscovery>> Instances =
            new ConcurrentDictionary<string, Lazy<WopiDiscovery>>();

        /// <summary>
        /// For tests.
        /// </summary>
        internal static void AddInstance(string url, WopiDiscovery discovery)
        {
            Instances.AddOrUpdate(url.TrimEnd('/'),
                oosUrl => new Lazy<WopiDiscovery>(() => discovery),
                (oosUrl, current) => new Lazy<WopiDiscovery>(() => discovery));
        }

        internal static WopiDiscovery GetInstance(string officeOnlineUrl)
        {
            return Instances.GetOrAdd(officeOnlineUrl.TrimEnd('/'), oosUrl => new Lazy<WopiDiscovery>(() =>
            {
                var discoveryXml = new XmlDocument();

                Retrier.Retry(3, 500, () =>
                {
                    using (var client = new HttpClient())
                    {
                        using (var discoveryStream = client.GetAsync($"{oosUrl}/hosting/discovery")
                            .GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            discoveryXml.Load(discoveryStream);
                        }
                    }
                }, (i, ex) => ex == null || i > 3);

                if (discoveryXml.DocumentElement == null)
                    SnLog.WriteWarning($"Could not connect to Office Online Server {oosUrl} for available actions.");
                else
                    SnLog.WriteInformation($"Connected to Office Online Server {oosUrl} for available actions.");


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
