using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SenseNet.Services.Wopi
{
    // See https://wopi.readthedocs.io/projects/wopirest/en/latest/files/CheckFileInfo.html

    [JsonObject(MemberSerialization.OptOut)]
    public class CheckFileInfoResponse : WopiResponse, IWopiObjectResponse
    {
        /* Base properties */

        public string BaseFileName { get; internal set; }
        public long Size { get; internal set; }
        public string UserId { get; internal set; }
        public string Version { get; internal set; }

        /* WOPI host capabilities properties */

        //public string[] SupportedShareUrlTypes
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

        public bool IsAnonymousUser { get; internal set; }
        public bool IsEduUser { get; internal set; }
        public bool LicenseCheckForEditIsEnabled { get; internal set; }
        public string UserFriendlyName { get; internal set; }
        public string UserInfo { get; internal set; }

        /* User permissions properties */

        public bool ReadOnly { get; internal set; }
        public bool RestrictedWebViewOnly { get; internal set; }
        public bool UserCanAttend { get; internal set; }
        public bool UserCanNotWriteRelative { get; internal set; }
        public bool UserCanPresent { get; internal set; }
        public bool UserCanRename { get; internal set; }
        public bool UserCanWrite { get; internal set; }

        /* File URL properties */

        public string CloseUrl { get; internal set; }
        public string DownloadUrl { get; internal set; }
        public string FileSharingUrl { get; internal set; }
        public string FileUrl { get; internal set; }
        public string FileVersionUrl { get; internal set; }
        public string HostEditUrl { get; internal set; }
        public string HostEmbeddedViewUrl { get; internal set; }
        public string HostViewUrl { get; internal set; }
        public string SignoutUrl { get; internal set; }

        /* Breadcrumb properties */

        public string BreadcrumbBrandName { get; internal set; }
        public string BreadcrumbBrandUrl { get; internal set; }
        public string BreadcrumbDocName { get; internal set; }
        public string BreadcrumbFolderName { get; internal set; }
        public string BreadcrumbFolderUrl { get; internal set; }

        /* Other miscellaneous properties */

        public bool AllowAdditionalMicrosoftServices => false;
        public bool AllowErrorReportPrompt => false;
        public bool AllowExternalMarketplace => false;
        public bool CloseButtonClosesWindow => false;
        public bool DisablePrint => false;
        public bool DisableTranslation => false;
        public string FileExtension { get; internal set; }
        public int FileNameMaxLength => 0;
        public string LastModifiedTime { get; internal set; }
        // ReSharper disable once InconsistentNaming
        public string SHA256 { get; internal set; }
        public string UniqueContentId { get; internal set; }
    }
}
