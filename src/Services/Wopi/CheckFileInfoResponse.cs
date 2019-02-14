using Newtonsoft.Json;

namespace SenseNet.Services.Wopi
{
    // See https://wopi.readthedocs.io/projects/wopirest/en/latest/files/CheckFileInfo.html

    [JsonObject(MemberSerialization.OptOut)]
    internal class CheckFileInfoResponse : WopiResponse, IWopiObjectResponse
    {
        private static readonly string[] _emptyArray = new string[0];

        /* Base properties */

        [JsonProperty] public string BaseFileName { get; internal set; }
        [JsonProperty] public string OwnerId { get; internal set; }
        [JsonProperty] public long Size { get; internal set; }
        [JsonProperty] public string UserId { get; internal set; }
        [JsonProperty] public string Version { get; internal set; }

        /* WOPI host capabilities properties */

        public string[] SupportedShareUrlTypes => _emptyArray;
        public bool SupportsCobalt => false;
        public bool SupportsContainers => false;
        public bool SupportsDeleteFile => false;
        public bool SupportsEcosystem => false;
        public bool SupportsExtendedLockLength => false;
        public bool SupportsFolders => false;
        public bool SupportsGetFileWopiSrc => false;
        public bool SupportsGetLock => true;
        public bool SupportsLocks => true;
        public bool SupportsRename => false;
        public bool SupportsUpdate => true;
        public bool SupportsUserInfo => false;

        /* User metadata properties */

        [JsonProperty] public bool IsAnonymousUser { get; internal set; }
        [JsonProperty] public bool IsEduUser { get; internal set; }
        [JsonProperty] public bool LicenseCheckForEditIsEnabled { get; internal set; }
        [JsonProperty] public string UserFriendlyName { get; internal set; }
        [JsonProperty] public string UserInfo { get; internal set; }

        /* User permissions properties */

        [JsonProperty] public bool ReadOnly { get; internal set; }
        [JsonProperty] public bool RestrictedWebViewOnly { get; internal set; }
        [JsonProperty] public bool UserCanAttend { get; internal set; }
        [JsonProperty] public bool UserCanNotWriteRelative { get; internal set; }
        [JsonProperty] public bool UserCanPresent { get; internal set; }
        [JsonProperty] public bool UserCanRename { get; internal set; }
        [JsonProperty] public bool UserCanWrite { get; internal set; }

        /* File URL properties */

        [JsonProperty] public string CloseUrl { get; internal set; }
        [JsonProperty] public string DownloadUrl { get; internal set; }
        [JsonProperty] public string FileSharingUrl { get; internal set; }
        [JsonProperty] public string FileUrl { get; internal set; }
        [JsonProperty] public string FileVersionUrl { get; internal set; }
        [JsonProperty] public string HostEditUrl { get; internal set; }
        [JsonProperty] public string HostEmbeddedViewUrl { get; internal set; }
        [JsonProperty] public string HostViewUrl { get; internal set; }
        [JsonProperty] public string SignoutUrl { get; internal set; }

        /* Breadcrumb properties */

        [JsonProperty] public string BreadcrumbBrandName { get; internal set; }
        [JsonProperty] public string BreadcrumbBrandUrl { get; internal set; }
        [JsonProperty] public string BreadcrumbDocName { get; internal set; }
        [JsonProperty] public string BreadcrumbFolderName { get; internal set; }
        [JsonProperty] public string BreadcrumbFolderUrl { get; internal set; }

        /* Other miscellaneous properties */

        public bool AllowAdditionalMicrosoftServices => false;
        public bool AllowErrorReportPrompt => false;
        public bool AllowExternalMarketplace => false;
        public bool CloseButtonClosesWindow => false;
        public bool DisablePrint => false;
        public bool DisableTranslation => false;
        [JsonProperty] public string FileExtension { get; internal set; }
        public int FileNameMaxLength => 0;
        [JsonProperty] public string LastModifiedTime { get; internal set; }
        // ReSharper disable once InconsistentNaming
        [JsonProperty] public string SHA256 { get; internal set; }
        [JsonProperty] public string UniqueContentId { get; internal set; }

        /* Method for tests */

        internal static CheckFileInfoResponse Parse(string src)
        {
            return JsonConvert.DeserializeObject<CheckFileInfoResponse>(src);
        }
    }
}
