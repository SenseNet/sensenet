using System.Collections.Generic;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Services.Core.Virtualization;

namespace SenseNet.Portal
{
    [ContentHandler]
    public class PortalSettings : Settings
    {
        public const string SETTINGSNAME = "Portal";
        public const string SETTINGS_CACHEHEADERS = "ClientCacheHeaders";
        public const string SETTINGS_BINARYHANDLER_MAXAGE = "BinaryHandlerClientCacheMaxAge";
        public const string SETTINGS_APPS_WITHOUT_OPEN = "PermittedAppsWithoutOpenPermission";
        public const string SETTINGS_UPLOADFILEEXTENSIONS = "UploadFileExtensions";
        public const string SETTINGS_UPLOADFILEEXTENSIONS_DEFAULT = "UploadFileExtensions.DefaultContentType";
        public const string SETTINGS_ALLOWEDORIGINDOMAINS = "AllowedOriginDomains";
        public const string SETTINGS_ALLOWEDMETHODS = "AllowedMethods";
        public const string SETTINGS_ALLOWEDHEADERS = "AllowedHeaders";

        public PortalSettings(Node parent) : this(parent, null) { }
        public PortalSettings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        protected PortalSettings(NodeToken nt) : base(nt) { }

        protected override object GetValueFromXml(XmlNode xmlNode, string key)
        {
            switch (key)
            {
                case SETTINGS_CACHEHEADERS:
                    return ParseCacheHeaderSettings(xmlNode);
                default: 
                    return base.GetValueFromXml(xmlNode, key);
            }
        }

        private static IEnumerable<CacheHeaderSetting> ParseCacheHeaderSettings(XmlNode xmlNode)
        {
            var cacheHeaderList = new List<CacheHeaderSetting>();

            foreach (XmlNode cacheHeaderNode in xmlNode.ChildNodes)
            {
                // skip text nodes
                if (!(cacheHeaderNode is XmlElement))
                    continue;

                var cacheHeader = new CacheHeaderSetting();
                var attrCt = cacheHeaderNode.Attributes["ContentType"];
                var attrPath = cacheHeaderNode.Attributes["Path"];
                var attrExt = cacheHeaderNode.Attributes["Extension"];
                var attrMaxAge = cacheHeaderNode.Attributes["MaxAge"];

                // if the value is not a real integer, skip this setting
                if (attrMaxAge == null || !int.TryParse(attrMaxAge.Value, out var value))
                    continue;

                cacheHeader.MaxAge = value;

                if (attrCt != null && !string.IsNullOrEmpty(attrCt.Value))
                    cacheHeader.ContentType = attrCt.Value;
                if (attrPath != null && !string.IsNullOrEmpty(attrPath.Value))
                    cacheHeader.Path = attrPath.Value;
                if (attrExt != null && !string.IsNullOrEmpty(attrExt.Value))
                    cacheHeader.Extension = attrExt.Value.ToLower().Trim(new[]{' ', '.'});
                
                // if none of the above were set, skip this setting
                if (string.IsNullOrEmpty(cacheHeader.ContentType) &&
                    string.IsNullOrEmpty(cacheHeader.Path) &&
                    string.IsNullOrEmpty(cacheHeader.Extension))
                {
                    SnLog.WriteWarning("Empty client cache header setting found in Portal settings.");
                    continue;
                }

                cacheHeaderList.Add(cacheHeader);
            }

            return cacheHeaderList;
        }
    }
}
