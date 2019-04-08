using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    internal class InitialData
    {
        #region private string _propertyTypes = @"
        private string _propertyTypes = @"
      Id, Type      , Mapping, Name
    ----- ----------- -------- ---------------
       1, Binary    ,       0, Binary
       2, Int       ,       0, VersioningMode
       3, Text      ,       0, Description
       4, Int       ,       1, Hidden
       5, Int       ,       2, InheritableVersioningMode
       6, Int       ,       3, ApprovingMode
       7, Int       ,       4, InheritableApprovingMode
       8, Text      ,       1, AllowedChildTypes
       9, Int       ,       5, TrashDisabled
      10, Int       ,       6, EnableLifespan
      11, DateTime  ,       0, ValidFrom
      12, DateTime  ,       1, ValidTill
      13, Reference ,       0, Aspects
      14, Text      ,       2, AspectData
      15, Reference ,       1, BrowseApplication
      16, Text      ,       3, SharingData
      17, Text      ,       4, ExtensionData
      18, Int       ,       7, IsTaggable
      19, Text      ,       5, Tags
      20, Int       ,       8, IsRateable
      21, String    ,       0, RateStr
      22, Currency  ,       0, RateAvg
      23, Int       ,       9, RateCount
      24, Text      ,       6, CheckInComments
      25, Text      ,       7, RejectReason
      26, String    ,       1, AppName
      27, Int       ,      10, Disabled
      28, Int       ,      11, IsModal
      29, Int       ,      12, Clear
      30, String    ,       2, Scenario
      31, String    ,       3, ActionTypeName
      32, String    ,       4, StyleHint
      33, String    ,       5, RequiredPermissions
      34, Int       ,      13, DeepPermissionCheck
      35, String    ,       6, IncludeBackUrl
      36, String    ,       7, CacheControl
      37, String    ,       8, MaxAge
      38, String    ,       9, CustomUrlParameters
      39, String    ,      10, StoredIcon
      40, Text      ,       8, ContentListBindings
      41, Text      ,       9, ContentListDefinition
      42, String    ,      11, DefaultView
      43, Reference ,       2, AvailableViews
      44, Reference ,       3, AvailableContentTypeFields
      45, String    ,      12, ListEmail
      46, String    ,      13, ExchangeSubscriptionId
      47, Int       ,      14, OverwriteFiles
      48, String    ,      14, GroupAttachments
      49, Int       ,      15, SaveOriginalEmail
      50, Reference ,       4, IncomingEmailWorkflow
      51, Int       ,      16, OnlyFromLocalGroups
      52, String    ,      15, InboxFolder
      53, Reference ,       5, OwnerWhenVisitor
      54, Text      ,      10, AspectDefinition
      55, Reference ,       6, FieldSettingContents
      56, Reference ,       7, Link
      57, Int       ,      17, WorkflowsRunning
      58, String    ,      16, UserAgentPattern
      59, String    ,      17, SyncGuid
      60, DateTime  ,       2, LastSync
      61, String    ,      18, Watermark
      62, Int       ,      18, PageCount
      63, String    ,      19, MimeType
      64, Text      ,      11, Shapes
      65, Text      ,      12, PageAttributes
      66, String    ,      20, From
      67, Text      ,      13, Body
      68, DateTime  ,       3, Sent
      69, String    ,      21, ClassName
      70, String    ,      22, MethodName
      71, Text      ,      14, Parameters
      72, Reference ,       8, Members
      73, String    ,      23, StatusCode
      74, String    ,      24, RedirectUrl
      75, Int       ,      19, Width
      76, Int       ,      20, Height
      77, Text      ,      15, Keywords
      78, DateTime  ,       4, DateTaken
      79, Reference ,       9, CoverImage
      80, String    ,      25, ImageType
      81, String    ,      26, ImageFieldName
      82, Int       ,      21, Stretch
      83, String    ,      27, OutputFormat
      84, String    ,      28, SmoothingMode
      85, String    ,      29, InterpolationMode
      86, String    ,      30, PixelOffsetMode
      87, String    ,      31, ResizeTypeMode
      88, String    ,      32, CropVAlign
      89, String    ,      33, CropHAlign
      90, Int       ,      22, GlobalOnly
      91, DateTime  ,       5, Date
      92, String    ,      34, MemoType
      93, Reference ,      10, SeeAlso
      94, Text      ,      16, Query
      95, Currency  ,       1, Downloads
      96, Text      ,      17, SharingIds
      97, String    ,      35, SharingLevelValue
      98, Reference ,      11, SharedContent
      99, Int       ,      23, IsActive
     100, Int       ,      24, IsWallContainer
     101, Reference ,      12, WorkspaceSkin
     102, Reference ,      13, Manager
     103, DateTime  ,       6, Deadline
     104, Int       ,      25, IsCritical
     105, String    ,      36, PendingUserLang
     106, String    ,      37, Language
     107, Int       ,      26, EnableClientBasedCulture
     108, Int       ,      27, EnableUserBasedCulture
     109, Text      ,      18, UrlList
     110, Reference ,      14, StartPage
     111, Reference ,      15, LoginPage
     112, Reference ,      16, SiteSkin
     113, Int       ,      28, DenyCrossSiteAccess
     114, String    ,      38, EnableAutofilters
     115, String    ,      39, EnableLifespanFilter
     116, DateTime  ,       7, StartDate
     117, DateTime  ,       8, DueDate
     118, Reference ,      17, AssignedTo
     119, String    ,      40, Priority
     120, String    ,      41, Status
     121, Int       ,      29, TaskCompletion
     122, DateTime  ,       9, KeepUntil
     123, String    ,      42, OriginalPath
     124, Int       ,      30, WorkspaceId
     125, String    ,      43, WorkspaceRelativePath
     126, Int       ,      31, MinRetentionTime
     127, Int       ,      32, SizeQuota
     128, Int       ,      33, BagCapacity
     129, Int       ,      34, Enabled
     130, String    ,      44, Domain
     131, String    ,      45, Email
     132, String    ,      46, FullName
     133, Text      ,      19, OldPasswords
     134, String    ,      47, PasswordHash
     135, String    ,      48, LoginName
     136, Reference ,      18, Profile
     137, Reference ,      19, FollowedWorkspaces
     138, DateTime  ,      10, LastLoggedOut
     139, String    ,      49, JobTitle
     140, Reference ,      20, ImageRef
     141, Binary    ,       1, ImageData
     142, String    ,      50, Captcha
     143, String    ,      51, Department
     144, String    ,      52, Languages
     145, String    ,      53, Phone
     146, String    ,      54, Gender
     147, String    ,      55, MaritalStatus
     148, DateTime  ,      11, BirthDate
     149, Text      ,      20, Education
     150, String    ,      56, TwitterAccount
     151, String    ,      57, FacebookURL
     152, String    ,      58, LinkedInURL
";
        #endregion

        #region private string _nodeTypes = @"
        private string _nodeTypes = @"
      Id, Name                          , Parent                        , ClassName                                                   , Properties
    ----- ------------------------------- ------------------------------- ------------------------------------------------------------- ------------------------------------------
       9, ContentType                   , <null>                        , SenseNet.ContentRepository.Schema.ContentType               , [Binary]
      10, GenericContent                , <null>                        , SenseNet.ContentRepository.GenericContent                   , [VersioningMode,Description,Hidden,InheritableVersioningMode,ApprovingMode,InheritableApprovingMode,AllowedChildTypes,TrashDisabled,EnableLifespan,ValidFrom,ValidTill,Aspects,AspectData,BrowseApplication,SharingData,ExtensionData,IsTaggable,Tags,IsRateable,RateStr,RateAvg,RateCount,CheckInComments,RejectReason]
      11, Application                   , GenericContent                , SenseNet.ApplicationModel.Application                       , [AppName,Disabled,IsModal,Clear,Scenario,ActionTypeName,StyleHint,RequiredPermissions,DeepPermissionCheck,IncludeBackUrl,CacheControl,MaxAge,CustomUrlParameters,StoredIcon]
      12, FieldSettingContent           , GenericContent                , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      13, ContentLink                   , GenericContent                , SenseNet.ContentRepository.ContentLink                      , [Link]
      14, ListItem                      , GenericContent                , SenseNet.ContentRepository.GenericContent                   , []
      15, File                          , GenericContent                , SenseNet.ContentRepository.File                             , [Binary,Watermark,PageCount,MimeType,Shapes,PageAttributes]
      16, Query                         , GenericContent                , SenseNet.ContentRepository.QueryContent                     , [Query]
       1, Folder                        , GenericContent                , SenseNet.ContentRepository.Folder                           , [VersioningMode,Description,Hidden,InheritableVersioningMode,ApprovingMode,InheritableApprovingMode,TrashDisabled,EnableLifespan,ValidFrom,ValidTill,Aspects,AspectData,BrowseApplication,SharingData]
       2, Group                         , GenericContent                , SenseNet.ContentRepository.Group                            , [VersioningMode,Description,Hidden,InheritableVersioningMode,ApprovingMode,InheritableApprovingMode,AllowedChildTypes,TrashDisabled,EnableLifespan,ValidFrom,ValidTill,Aspects,AspectData,BrowseApplication,SharingData,SyncGuid,LastSync,Members]
       3, User                          , GenericContent                , SenseNet.ContentRepository.User                             , [VersioningMode,Description,Hidden,InheritableVersioningMode,ApprovingMode,InheritableApprovingMode,AllowedChildTypes,TrashDisabled,EnableLifespan,ValidFrom,ValidTill,Aspects,AspectData,BrowseApplication,SharingData,SyncGuid,LastSync,Manager,Language,Enabled,Domain,Email,FullName,OldPasswords,PasswordHash,LoginName,Profile,FollowedWorkspaces,LastLoggedOut,JobTitle,ImageRef,ImageData,Captcha,Department,Languages,Phone,Gender,MaritalStatus,BirthDate,Education,TwitterAccount,FacebookURL,LinkedInURL]
      17, ApplicationOverride           , Application                   , SenseNet.ApplicationModel.Application                       , []
      18, ExportToCsvApplication        , Application                   , SenseNet.Services.ExportToCsvApplication                    , []
      19, GenericODataApplication       , Application                   , SenseNet.Portal.ApplicationModel.GenericODataApplication    , [ClassName,MethodName,Parameters]
      20, HttpHandlerApplication        , Application                   , SenseNet.Portal.Handlers.HttpHandlerApplication             , []
      21, HttpStatusApplication         , Application                   , SenseNet.Portal.AppModel.HttpStatusApplication              , [StatusCode,RedirectUrl]
      22, ImgResizeApplication          , Application                   , SenseNet.Portal.ApplicationModel.ImgResizeApplication       , [Width,Height,ImageType,ImageFieldName,Stretch,OutputFormat,SmoothingMode,InterpolationMode,PixelOffsetMode,ResizeTypeMode,CropVAlign,CropHAlign]
      23, RssApplication                , Application                   , SenseNet.Services.RssApplication                            , []
      24, WebServiceApplication         , Application                   , SenseNet.ApplicationModel.Application                       , [Binary]
      35, BinaryFieldSetting            , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      36, TextFieldSetting              , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      37, NumberFieldSetting            , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      38, DateTimeFieldSetting          , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      39, HyperLinkFieldSetting         , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      40, IntegerFieldSetting           , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      41, NullFieldSetting              , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      42, ReferenceFieldSetting         , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      43, XmlFieldSetting               , FieldSettingContent           , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      44, CustomListItem                , ListItem                      , SenseNet.ContentRepository.GenericContent                   , [WorkflowsRunning]
      45, Memo                          , ListItem                      , SenseNet.ContentRepository.GenericContent                   , [Date,MemoType,SeeAlso]
      46, Task                          , ListItem                      , SenseNet.ContentRepository.Task                             , [StartDate,DueDate,AssignedTo,Priority,Status,TaskCompletion]
      47, DynamicJsonContent            , File                          , SenseNet.Portal.Handlers.DynamicJsonContent                 , []
      48, ExecutableFile                , File                          , SenseNet.ContentRepository.File                             , []
      49, HtmlTemplate                  , File                          , SenseNet.Portal.UI.HtmlTemplate                             , []
      50, Image                         , File                          , SenseNet.ContentRepository.Image                            , [Width,Height,Keywords,DateTaken]
      51, Settings                      , File                          , SenseNet.ContentRepository.Settings                         , [GlobalOnly]
      52, SystemFile                    , File                          , SenseNet.ContentRepository.File                             , []
       4, PortalRoot                    , Folder                        , SenseNet.ContentRepository.PortalRoot                       , [VersioningMode,Description,Hidden,InheritableVersioningMode,ApprovingMode,InheritableApprovingMode,TrashDisabled,EnableLifespan,ValidFrom,ValidTill,Aspects,AspectData,BrowseApplication,SharingData]
       5, SystemFolder                  , Folder                        , SenseNet.ContentRepository.SystemFolder                     , []
       6, Domains                       , Folder                        , SenseNet.ContentRepository.Folder                           , []
       7, Domain                        , Folder                        , SenseNet.ContentRepository.Domain                           , [SyncGuid,LastSync]
       8, OrganizationalUnit            , Folder                        , SenseNet.ContentRepository.OrganizationalUnit               , [SyncGuid,LastSync]
      25, ContentList                   , Folder                        , SenseNet.ContentRepository.ContentList                      , [ContentListBindings,ContentListDefinition,DefaultView,AvailableViews,AvailableContentTypeFields,ListEmail,ExchangeSubscriptionId,OverwriteFiles,GroupAttachments,SaveOriginalEmail,IncomingEmailWorkflow,OnlyFromLocalGroups,InboxFolder,OwnerWhenVisitor]
      26, Device                        , Folder                        , SenseNet.ApplicationModel.Device                            , [UserAgentPattern]
      27, Email                         , Folder                        , SenseNet.ContentRepository.Folder                           , [From,Body,Sent]
      28, ProfileDomain                 , Folder                        , SenseNet.ContentRepository.Folder                           , []
      29, Profiles                      , Folder                        , SenseNet.ContentRepository.Folder                           , []
      30, RuntimeContentContainer       , Folder                        , SenseNet.ContentRepository.RuntimeContentContainer          , []
      31, Workspace                     , Folder                        , SenseNet.ContentRepository.Workspaces.Workspace             , [IsActive,IsWallContainer,WorkspaceSkin,Manager,Deadline,IsCritical]
      32, Sites                         , Folder                        , SenseNet.ContentRepository.Folder                           , []
      33, SmartFolder                   , Folder                        , SenseNet.ContentRepository.SmartFolder                      , [Query,EnableAutofilters,EnableLifespanFilter]
      34, TrashBag                      , Folder                        , SenseNet.ContentRepository.TrashBag                         , [Link,KeepUntil,OriginalPath,WorkspaceId,WorkspaceRelativePath]
      53, SharingGroup                  , Group                         , SenseNet.ContentRepository.Group                            , [SharingIds,SharingLevelValue,SharedContent]
      54, GetMetadataApplication        , HttpHandlerApplication        , SenseNet.Portal.Handlers.GetMetadataApplication             , []
      62, ShortTextFieldSetting         , TextFieldSetting              , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      63, LongTextFieldSetting          , TextFieldSetting              , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      64, CurrencyFieldSetting          , NumberFieldSetting            , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      65, PreviewImage                  , Image                         , SenseNet.ContentRepository.Image                            , []
      66, IndexingSettings              , Settings                      , SenseNet.Search.IndexingSettings                            , []
      67, LoggingSettings               , Settings                      , SenseNet.ContentRepository.LoggingSettings                  , []
      68, PortalSettings                , Settings                      , SenseNet.Portal.PortalSettings                              , []
      69, Resource                      , SystemFile                    , SenseNet.ContentRepository.i18n.Resource                    , [Downloads]
      55, Resources                     , SystemFolder                  , SenseNet.ContentRepository.SystemFolder                     , []
      56, Aspect                        , ContentList                   , SenseNet.ContentRepository.Aspect                           , [AspectDefinition,FieldSettingContents]
      57, ItemList                      , ContentList                   , SenseNet.ContentRepository.ContentList                      , []
      58, Library                       , ContentList                   , SenseNet.ContentRepository.ContentList                      , []
      59, Site                          , Workspace                     , SenseNet.Portal.Site                                        , [PendingUserLang,Language,EnableClientBasedCulture,EnableUserBasedCulture,UrlList,StartPage,LoginPage,SiteSkin,DenyCrossSiteAccess]
      60, TrashBin                      , Workspace                     , SenseNet.ContentRepository.TrashBin                         , [MinRetentionTime,SizeQuota,BagCapacity]
      61, UserProfile                   , Workspace                     , SenseNet.ContentRepository.UserProfile                      , []
      75, ChoiceFieldSetting            , ShortTextFieldSetting         , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      76, PasswordFieldSetting          , ShortTextFieldSetting         , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      70, CustomList                    , ItemList                      , SenseNet.ContentRepository.ContentList                      , []
      71, MemoList                      , ItemList                      , SenseNet.ContentRepository.ContentList                      , []
      72, TaskList                      , ItemList                      , SenseNet.ContentRepository.ContentList                      , []
      73, DocumentLibrary               , Library                       , SenseNet.ContentRepository.ContentList                      , []
      74, ImageLibrary                  , Library                       , SenseNet.ContentRepository.ContentList                      , [CoverImage]
      77, PermissionChoiceFieldSetting  , ChoiceFieldSetting            , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
      78, YesNoFieldSetting             , ChoiceFieldSetting            , SenseNet.ContentRepository.Schema.FieldSettingContent       , []
";
        #endregion

        #region private static string _nodes = @"
        private static string _nodes = @"
NodeId, TypeId, Parent,  Index, MinorV, MajorV, IsSys,  Owner, Name,                                     DisplayName,                                        Path
------- ------- -------  ------ ------- ------- ------ ------- ----------------------------------------- --------------------------------------------------- -------------------------------------
     1,      3,      5,      0,      1,      1,   ---,      1, Admin                                   , """"                                              , /Root/IMS/BuiltIn/Portal/Admin
     2,      4,      0,      1,      2,      2,   ---,      1, Root                                    , """"                                              , /Root
     3,      6,      2,      3,      3,      3,   ---,      1, IMS                                     , Users and Groups                                  , /Root/IMS
     4,      7,      3,      0,      4,      4,   ---,      1, BuiltIn                                 , """"                                              , /Root/IMS/BuiltIn
     5,      8,      4,      0,      5,      5,   ---,      1, Portal                                  , """"                                              , /Root/IMS/BuiltIn/Portal
     6,      3,      5,      4,      6,      6,   ---,      1, Visitor                                 , """"                                              , /Root/IMS/BuiltIn/Portal/Visitor
     7,      2,      5,      2,      7,      7,   ---,      1, Administrators                          , """"                                              , /Root/IMS/BuiltIn/Portal/Administrators
     8,      2,      5,      3,      8,      8,   ---,      1, Everyone                                , """"                                              , /Root/IMS/BuiltIn/Portal/Everyone
     9,      2,      5,      5,      9,      9,   ---,      1, Owners                                  , """"                                              , /Root/IMS/BuiltIn/Portal/Owners
    10,      3,      5,      7,     10,     10,   ---,      1, Somebody                                , """"                                              , /Root/IMS/BuiltIn/Portal/Somebody
    11,      2,      5,      7,     11,     11,   ---,      1, Operators                               , """"                                              , /Root/IMS/BuiltIn/Portal/Operators
    12,      3,      5,      8,     12,     12,   ---,      1, Startup                                 , """"                                              , /Root/IMS/BuiltIn/Portal/Startup
  1000,      5,      2,      3,     13,     13,   sys,      1, System                                  , """"                                              , /Root/System
  1001,      5,   1000,      1,     14,     14,   sys,      1, Schema                                  , Schema                                            , /Root/System/Schema
  1002,      5,   1001,      1,     15,     15,   sys,      1, ContentTypes                            , ContentTypes                                      , /Root/System/Schema/ContentTypes
  1003,      5,   1000,      2,     16,     16,   sys,      1, Settings                                , Settings                                          , /Root/System/Settings
  1004,      9,   1002,      0,     17,     17,   sys,      1, ContentType                             , $Ctd-ContentType,DisplayName                      , /Root/System/Schema/ContentTypes/ContentType
  1005,      9,   1002,      0,     18,     18,   sys,      1, GenericContent                          , $Ctd-GenericContent,DisplayName                   , /Root/System/Schema/ContentTypes/GenericContent
  1006,      9,   1005,      0,     19,     19,   sys,      1, Application                             , $Ctd-Application,DisplayName                      , /Root/System/Schema/ContentTypes/GenericContent/Application
  1007,      9,   1006,      0,     20,     20,   sys,      1, ApplicationOverride                     , $Ctd-ApplicationOverride,DisplayName              , /Root/System/Schema/ContentTypes/GenericContent/Application/ApplicationOverride
  1008,      9,   1005,      0,     21,     21,   sys,      1, Folder                                  , $Ctd-Folder,DisplayName                           , /Root/System/Schema/ContentTypes/GenericContent/Folder
  1009,      9,   1008,      0,     22,     22,   sys,      1, ContentList                             , $Ctd-ContentList,DisplayName                      , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList
  1010,      9,   1009,      0,     23,     23,   sys,      1, Aspect                                  , $Ctd-Aspect,DisplayName                           , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Aspect
  1011,      9,   1005,      0,     24,     24,   sys,      1, FieldSettingContent                     , $Ctd-FieldSettingContent,DisplayName              , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent
  1012,      9,   1011,      0,     25,     25,   sys,      1, BinaryFieldSetting                      , $Ctd-BinaryFieldSetting,DisplayName               , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/BinaryFieldSetting
  1013,      9,   1011,      0,     26,     26,   sys,      1, TextFieldSetting                        , $Ctd-TextFieldSetting,DisplayName                 , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting
  1014,      9,   1013,      0,     27,     27,   sys,      1, ShortTextFieldSetting                   , $Ctd-ShortTextFieldSetting,DisplayName            , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting
  1015,      9,   1014,      0,     28,     28,   sys,      1, ChoiceFieldSetting                      , $Ctd-ChoiceFieldSetting,DisplayName               , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting
  1016,      9,   1005,      0,     29,     29,   sys,      1, ContentLink                             , $Ctd-ContentLink,DisplayName                      , /Root/System/Schema/ContentTypes/GenericContent/ContentLink
  1017,      9,   1011,      0,     30,     30,   sys,      1, NumberFieldSetting                      , $Ctd-NumberFieldSetting,DisplayName               , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting
  1018,      9,   1017,      0,     31,     31,   sys,      1, CurrencyFieldSetting                    , $Ctd-CurrencyFieldSetting,DisplayName             , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting/CurrencyFieldSetting
  1019,      9,   1009,      0,     32,     32,   sys,      1, ItemList                                , $Ctd-ItemList,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList
  1020,      9,   1019,      0,     33,     33,   sys,      1, CustomList                              , $Ctd-CustomList,DisplayName                       , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/CustomList
  1021,      9,   1005,      0,     34,     34,   sys,      1, ListItem                                , $Ctd-ListItem,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/ListItem
  1022,      9,   1021,      0,     35,     35,   sys,      1, CustomListItem                          , $Ctd-CustomListItem,DisplayName                   , /Root/System/Schema/ContentTypes/GenericContent/ListItem/CustomListItem
  1023,      9,   1011,      0,     36,     36,   sys,      1, DateTimeFieldSetting                    , $Ctd-DateTimeFieldSetting,DisplayName             , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/DateTimeFieldSetting
  1024,      9,   1008,      0,     37,     37,   sys,      1, Device                                  , $Ctd-Device,DisplayName                           , /Root/System/Schema/ContentTypes/GenericContent/Folder/Device
  1025,      9,   1009,      0,     38,     38,   sys,      1, Library                                 , $Ctd-Library,DisplayName                          , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library
  1026,      9,   1025,      0,     39,     39,   sys,      1, DocumentLibrary                         , $Ctd-DocumentLibrary,DisplayName                  , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary
  1027,      9,   1008,      0,     40,     40,   sys,      1, Domain                                  , $Ctd-Domain,DisplayName                           , /Root/System/Schema/ContentTypes/GenericContent/Folder/Domain
  1028,      9,   1008,      0,     41,     41,   sys,      1, Domains                                 , $Ctd-Domains,DisplayName                          , /Root/System/Schema/ContentTypes/GenericContent/Folder/Domains
  1029,      9,   1005,      0,     42,     42,   sys,      1, File                                    , $Ctd-File,DisplayName                             , /Root/System/Schema/ContentTypes/GenericContent/File
  1030,      9,   1029,      0,     43,     43,   sys,      1, DynamicJsonContent                      , Dynamic JSON content                              , /Root/System/Schema/ContentTypes/GenericContent/File/DynamicJsonContent
  1031,      9,   1008,      0,     44,     44,   sys,      1, Email                                   , $Ctd-Email,DisplayName                            , /Root/System/Schema/ContentTypes/GenericContent/Folder/Email
  1032,      9,   1029,      0,     45,     45,   sys,      1, ExecutableFile                          , $Ctd-ExecutableFile,DisplayName                   , /Root/System/Schema/ContentTypes/GenericContent/File/ExecutableFile
  1033,      9,   1006,      0,     46,     46,   sys,      1, ExportToCsvApplication                  , $Ctd-ExportToCsvApplication,DisplayName           , /Root/System/Schema/ContentTypes/GenericContent/Application/ExportToCsvApplication
  1034,      9,   1006,      0,     47,     47,   sys,      1, GenericODataApplication                 , $Ctd-GenericODataApplication,DisplayName          , /Root/System/Schema/ContentTypes/GenericContent/Application/GenericODataApplication
  1035,      9,   1006,      0,     48,     48,   sys,      1, HttpHandlerApplication                  , $Ctd-HttpHandlerApplication,DisplayName           , /Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication
  1036,      9,   1035,      0,     49,     49,   sys,      1, GetMetadataApplication                  , GetMetadataApplication                            , /Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication/GetMetadataApplication
  1037,      9,   1005,      0,     50,     50,   sys,      1, Group                                   , $Ctd-Group,DisplayName                            , /Root/System/Schema/ContentTypes/GenericContent/Group
  1038,      9,   1029,      0,     51,     51,   sys,      1, HtmlTemplate                            , $Ctd-HtmlTemplate,DisplayName                     , /Root/System/Schema/ContentTypes/GenericContent/File/HtmlTemplate
  1039,      9,   1006,      0,     52,     52,   sys,      1, HttpStatusApplication                   , $Ctd-HttpStatusApplication,DisplayName            , /Root/System/Schema/ContentTypes/GenericContent/Application/HttpStatusApplication
  1040,      9,   1011,      0,     53,     53,   sys,      1, HyperLinkFieldSetting                   , $Ctd-HyperLinkFieldSetting,DisplayName            , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/HyperLinkFieldSetting
  1041,      9,   1029,      0,     54,     54,   sys,      1, Image                                   , $Ctd-Image,DisplayName                            , /Root/System/Schema/ContentTypes/GenericContent/File/Image
  1042,      9,   1025,      0,     55,     55,   sys,      1, ImageLibrary                            , $Ctd-ImageLibrary,DisplayName                     , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/ImageLibrary
  1043,      9,   1006,      0,     56,     56,   sys,      1, ImgResizeApplication                    , $Ctd-ImgResizeApplication,DisplayName             , /Root/System/Schema/ContentTypes/GenericContent/Application/ImgResizeApplication
  1044,      9,   1029,      0,     57,     57,   sys,      1, Settings                                , $Ctd-Settings,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/File/Settings
  1045,      9,   1044,      0,     58,     58,   sys,      1, IndexingSettings                        , $Ctd-IndexingSettings,DisplayName                 , /Root/System/Schema/ContentTypes/GenericContent/File/Settings/IndexingSettings
  1046,      9,   1011,      0,     59,     59,   sys,      1, IntegerFieldSetting                     , $Ctd-IntegerFieldSetting,DisplayName              , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/IntegerFieldSetting
  1047,      9,   1044,      0,     60,     60,   sys,      1, LoggingSettings                         , $Ctd-LoggingSettings,DisplayName                  , /Root/System/Schema/ContentTypes/GenericContent/File/Settings/LoggingSettings
  1048,      9,   1013,      0,     61,     61,   sys,      1, LongTextFieldSetting                    , $Ctd-LongTextFieldSetting,DisplayName             , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/LongTextFieldSetting
  1049,      9,   1021,      0,     62,     62,   sys,      1, Memo                                    , $Ctd-Memo,DisplayName                             , /Root/System/Schema/ContentTypes/GenericContent/ListItem/Memo
  1050,      9,   1019,      0,     63,     63,   sys,      1, MemoList                                , $Ctd-MemoList,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList
  1051,      9,   1011,      0,     64,     64,   sys,      1, NullFieldSetting                        , $Ctd-NullFieldSetting,DisplayName                 , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NullFieldSetting
  1052,      9,   1008,      0,     65,     65,   sys,      1, OrganizationalUnit                      , $Ctd-OrganizationalUnit,DisplayName               , /Root/System/Schema/ContentTypes/GenericContent/Folder/OrganizationalUnit
  1053,      9,   1014,      0,     66,     66,   sys,      1, PasswordFieldSetting                    , $Ctd-PasswordFieldSetting,DisplayName             , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/PasswordFieldSetting
  1054,      9,   1015,      0,     67,     67,   sys,      1, PermissionChoiceFieldSetting            , $Ctd-PermissionChoiceFieldSetting,DisplayName     , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/PermissionChoiceFieldSetting
  1055,      9,   1008,      0,     68,     68,   sys,      1, PortalRoot                              , $Ctd-PortalRoot,DisplayName                       , /Root/System/Schema/ContentTypes/GenericContent/Folder/PortalRoot
  1056,      9,   1044,      0,     69,     69,   sys,      1, PortalSettings                          , $Ctd-PortalSettings,DisplayName                   , /Root/System/Schema/ContentTypes/GenericContent/File/Settings/PortalSettings
  1057,      9,   1041,      0,     70,     70,   sys,      1, PreviewImage                            , $Ctd-PreviewImage,DisplayName                     , /Root/System/Schema/ContentTypes/GenericContent/File/Image/PreviewImage
  1058,      9,   1008,      0,     71,     71,   sys,      1, ProfileDomain                           , $Ctd-ProfileDomain,DisplayName                    , /Root/System/Schema/ContentTypes/GenericContent/Folder/ProfileDomain
  1059,      9,   1008,      0,     72,     72,   sys,      1, Profiles                                , $Ctd-Profiles,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/Folder/Profiles
  1060,      9,   1005,      0,     73,     73,   sys,      1, Query                                   , $Ctd-Query,DisplayName                            , /Root/System/Schema/ContentTypes/GenericContent/Query
  1061,      9,   1011,      0,     74,     74,   sys,      1, ReferenceFieldSetting                   , $Ctd-ReferenceFieldSetting,DisplayName            , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/ReferenceFieldSetting
  1062,      9,   1029,      0,     75,     75,   sys,      1, SystemFile                              , $Ctd-SystemFile,DisplayName                       , /Root/System/Schema/ContentTypes/GenericContent/File/SystemFile
  1063,      9,   1062,      0,     76,     76,   sys,      1, Resource                                , $Ctd-Resource,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/Resource
  1064,      9,   1008,      0,     77,     77,   sys,      1, SystemFolder                            , $Ctd-SystemFolder,DisplayName                     , /Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder
  1065,      9,   1064,      0,     78,     78,   sys,      1, Resources                               , $Ctd-Resources,DisplayName                        , /Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Resources
  1066,      9,   1006,      0,     79,     79,   sys,      1, RssApplication                          , $Ctd-RssApplication,DisplayName                   , /Root/System/Schema/ContentTypes/GenericContent/Application/RssApplication
  1067,      9,   1008,      0,     80,     80,   sys,      1, RuntimeContentContainer                 , $Ctd-RuntimeContentContainer,DisplayName          , /Root/System/Schema/ContentTypes/GenericContent/Folder/RuntimeContentContainer
  1068,      9,   1037,      0,     81,     81,   sys,      1, SharingGroup                            , SharingGroup                                      , /Root/System/Schema/ContentTypes/GenericContent/Group/SharingGroup
  1069,      9,   1008,      0,     82,     82,   sys,      1, Workspace                               , $Ctd-Workspace,DisplayName                        , /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace
  1070,      9,   1069,      0,     83,     83,   sys,      1, Site                                    , $Ctd-Site,DisplayName                             , /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Site
  1071,      9,   1008,      0,     84,     84,   sys,      1, Sites                                   , $Ctd-Sites,DisplayName                            , /Root/System/Schema/ContentTypes/GenericContent/Folder/Sites
  1072,      9,   1008,      0,     85,     85,   sys,      1, SmartFolder                             , $Ctd-SmartFolder,DisplayName                      , /Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder
  1073,      9,   1021,      0,     86,     86,   sys,      1, Task                                    , $Ctd-Task,DisplayName                             , /Root/System/Schema/ContentTypes/GenericContent/ListItem/Task
  1074,      9,   1019,      0,     87,     87,   sys,      1, TaskList                                , $Ctd-TaskList,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList
  1075,      9,   1008,      0,     88,     88,   sys,      1, TrashBag                                , $Ctd-TrashBag,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/Folder/TrashBag
  1076,      9,   1069,      0,     89,     89,   sys,      1, TrashBin                                , $Ctd-TrashBin,DisplayName                         , /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TrashBin
  1077,      9,   1005,      0,     90,     90,   sys,      1, User                                    , $Ctd-User,DisplayName                             , /Root/System/Schema/ContentTypes/GenericContent/User
  1078,      9,   1069,      0,     91,     91,   sys,      1, UserProfile                             , $Ctd-UserProfile,DisplayName                      , /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/UserProfile
  1079,      9,   1006,      0,     92,     92,   sys,      1, WebServiceApplication                   , $Ctd-WebServiceApplication,DisplayName            , /Root/System/Schema/ContentTypes/GenericContent/Application/WebServiceApplication
  1080,      9,   1011,      0,     93,     93,   sys,      1, XmlFieldSetting                         , $Ctd-XmlFieldSetting,DisplayName                  , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/XmlFieldSetting
  1081,      9,   1015,      0,     94,     94,   sys,      1, YesNoFieldSetting                       , $Ctd-YesNoFieldSetting,DisplayName                , /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/YesNoFieldSetting
  1082,     66,   1003,      0,     95,     95,   sys,      1, Indexing.settings                       , """"                                              , /Root/System/Settings/Indexing.settings
  1083,     67,   1003,      0,     96,     96,   sys,      1, Logging.settings                        , """"                                              , /Root/System/Settings/Logging.settings
  1084,     51,   1003,      0,     97,     97,   sys,      1, MailProcessor.settings                  , """"                                              , /Root/System/Settings/MailProcessor.settings
  1085,     51,   1003,      0,     98,     98,   sys,      1, OAuth.settings                          , """"                                              , /Root/System/Settings/OAuth.settings
  1086,     68,   1003,      0,     99,     99,   sys,      1, Portal.settings                         , """"                                              , /Root/System/Settings/Portal.settings
  1087,     51,   1003,      0,    100,    100,   sys,      1, Sharing.settings                        , """"                                              , /Root/System/Settings/Sharing.settings
  1088,     51,   1003,      0,    101,    101,   sys,      1, TaskManagement.settings                 , """"                                              , /Root/System/Settings/TaskManagement.settings
  1089,     51,   1003,      0,    102,    102,   sys,      1, UserProfile.settings                    , """"                                              , /Root/System/Settings/UserProfile.settings
  1090,      5,      2,      0,    103,    103,   sys,      1, (apps)                                  , (apps)                                            , /Root/(apps)
  1091,      1,   1090,      0,    104,    104,   sys,      1, ContentList                             , """"                                              , /Root/(apps)/ContentList
  1092,     11,   1091,      0,    105,    105,   sys,      1, DeleteField                             , $Action,DeleteField                               , /Root/(apps)/ContentList/DeleteField
  1093,     11,   1091,      0,    106,    106,   sys,      1, EditField                               , $Action,EditField                                 , /Root/(apps)/ContentList/EditField
  1094,     24,   1091,      0,    107,    107,   sys,      1, ExchangeService.asmx                    , """"                                              , /Root/(apps)/ContentList/ExchangeService.asmx
  1095,      1,   1090,      0,    108,    108,   sys,      1, File                                    , """"                                              , /Root/(apps)/File
  1096,     19,   1095,      0,    109,    109,   sys,      1, CheckPreviews                           , Check preview images                              , /Root/(apps)/File/CheckPreviews
  1097,     11,   1095,      0,    110,    110,   sys,      1, EditInMicrosoftOffice                   , $Action,Edit-in-Microsoft-Office                  , /Root/(apps)/File/EditInMicrosoftOffice
  1098,     20,   1095,      0,    111,    111,   sys,      1, ExportToPdf                             , $Action,ExportToPdf                               , /Root/(apps)/File/ExportToPdf
  1099,     19,   1095,      0,    112,    112,   sys,      1, GetPageCount                            , Get page count                                    , /Root/(apps)/File/GetPageCount
  1100,     19,   1095,      0,    113,    113,   sys,      1, GetPreviewsFolder                       , Get previews folder                               , /Root/(apps)/File/GetPreviewsFolder
  1101,     19,   1095,      0,    114,    114,   sys,      1, PreviewAvailable                        , """"                                              , /Root/(apps)/File/PreviewAvailable
  1102,     19,   1095,      0,    115,    115,   sys,      1, RegeneratePreviews                      , Regenerate preview images                         , /Root/(apps)/File/RegeneratePreviews
  1103,     19,   1095,      0,    116,    116,   sys,      1, SetPageCount                            , Set page count                                    , /Root/(apps)/File/SetPageCount
  1104,     19,   1095,      0,    117,    117,   sys,      1, SetPreviewStatus                        , Set preview status                                , /Root/(apps)/File/SetPreviewStatus
  1105,     11,   1095,    250,    118,    118,   sys,      1, UploadResume                            , $Action,UploadResume                              , /Root/(apps)/File/UploadResume
  1106,      1,   1090,      0,    119,    119,   sys,      1, Folder                                  , """"                                              , /Root/(apps)/Folder
  1107,     11,   1106,   3800,    120,    120,   sys,      1, CopyBatch                               , $Action,CopyBatch                                 , /Root/(apps)/Folder/CopyBatch
  1108,     11,   1106,   3800,    121,    121,   sys,      1, DeleteBatch                             , $Action,DeleteBatch                               , /Root/(apps)/Folder/DeleteBatch
  1109,     18,   1106,   5400,    122,    122,   sys,      1, ExportToCsv                             , $Action,ExportToCsv                               , /Root/(apps)/Folder/ExportToCsv
  1110,     11,   1106,   3800,    123,    123,   sys,      1, MoveBatch                               , $Action,MoveBatch                                 , /Root/(apps)/Folder/MoveBatch
  1111,     11,   1106,      0,    124,    124,   sys,      1, Upload                                  , $Action,Upload                                    , /Root/(apps)/Folder/Upload
  1112,      1,   1090,      0,    125,    125,   sys,      1, GenericContent                          , """"                                              , /Root/(apps)/GenericContent
  1113,     19,   1112,      0,    126,    126,   sys,      1, AddAllowedChildTypes                    , """"                                              , /Root/(apps)/GenericContent/AddAllowedChildTypes
  1114,     19,   1112,      0,    127,    127,   sys,      1, GetRelatedPermissions                   , """"                                              , /Root/(apps)/GenericContent/GetRelatedPermissions
  1115,     19,   1112,      0,    128,    128,   sys,      1, GetSharing                              , """"                                              , /Root/(apps)/GenericContent/GetSharing
  1116,     19,   1112,      0,    129,    129,   sys,      1, GetTemplateScript                       , """"                                              , /Root/(apps)/GenericContent/GetTemplateScript
  1117,     20,   1112,      0,    130,    130,   sys,      1, HasPermission                           , $Action,HasPermission                             , /Root/(apps)/GenericContent/HasPermission
  1118,     19,   1112,      0,    131,    131,   sys,      1, Login                                   , """"                                              , /Root/(apps)/GenericContent/Login
  1119,     11,   1112,   9000,    132,    132,   sys,      1, Logout                                  , $Action,Logout                                    , /Root/(apps)/GenericContent/Logout
  1120,     11,   1112,   3800,    133,    133,   sys,      1, MoveTo                                  , $Action,MoveTo                                    , /Root/(apps)/GenericContent/MoveTo
  1121,     11,   1112,      0,    134,    134,   sys,      1, Publish                                 , $Action,Publish                                   , /Root/(apps)/GenericContent/Publish
  1122,     19,   1112,      0,    135,    135,   sys,      1, RebuildIndex                            , """"                                              , /Root/(apps)/GenericContent/RebuildIndex
  1123,     19,   1112,      0,    136,    136,   sys,      1, RebuildIndexSubtree                     , """"                                              , /Root/(apps)/GenericContent/RebuildIndexSubtree
  1124,     19,   1112,      0,    137,    137,   sys,      1, RefreshIndexSubtree                     , """"                                              , /Root/(apps)/GenericContent/RefreshIndexSubtree
  1125,     11,   1112,      0,    138,    138,   sys,      1, Reject                                  , $Action,Reject                                    , /Root/(apps)/GenericContent/Reject
  1126,     20,   1112,      0,    139,    139,   sys,      1, RemoveAllAspects                        , $Action,RemoveAllAspects                          , /Root/(apps)/GenericContent/RemoveAllAspects
  1127,     19,   1112,      0,    140,    140,   sys,      1, GetRelatedItemsOneLevel                 , """"                                              , /Root/(apps)/GenericContent/GetRelatedItemsOneLevel
  1128,     20,   1112,      0,    141,    141,   sys,      1, RemoveAllFields                         , $Action,RemoveAllFields                           , /Root/(apps)/GenericContent/RemoveAllFields
  1129,     20,   1112,      0,    142,    142,   sys,      1, RemoveAspects                           , $Action,RemoveAspects                             , /Root/(apps)/GenericContent/RemoveAspects
  1130,     20,   1112,      0,    143,    143,   sys,      1, RemoveFields                            , $Action,RemoveFields                              , /Root/(apps)/GenericContent/RemoveFields
  1131,     19,   1112,      0,    144,    144,   sys,      1, RemoveSharing                           , """"                                              , /Root/(apps)/GenericContent/RemoveSharing
  1132,     19,   1112,      0,    145,    145,   sys,      1, ResetRecentIndexingActivities           , """"                                              , /Root/(apps)/GenericContent/ResetRecentIndexingActivities
  1133,     11,   1112,      0,    146,    146,   sys,      1, RestoreVersion                          , $Action,RestoreVersion                            , /Root/(apps)/GenericContent/RestoreVersion
  1134,     20,   1112,      0,    147,    147,   sys,      1, RetrieveFields                          , $Action,RetrieveFields                            , /Root/(apps)/GenericContent/RetrieveFields
  1135,     23,   1112,      0,    148,    148,   sys,      1, Rss                                     , $Action,Rss                                       , /Root/(apps)/GenericContent/Rss
  1136,     19,   1112,      0,    149,    149,   sys,      1, SaveQuery                               , """"                                              , /Root/(apps)/GenericContent/SaveQuery
  1137,     11,   1112,      0,    150,    150,   sys,      1, SetPermissions                          , $Action,SetPermissions                            , /Root/(apps)/GenericContent/SetPermissions
  1138,     19,   1112,      0,    151,    151,   sys,      1, Share                                   , """"                                              , /Root/(apps)/GenericContent/Share
  1139,     19,   1112,      0,    152,    152,   sys,      1, StartBlobUpload                         , """"                                              , /Root/(apps)/GenericContent/StartBlobUpload
  1140,     19,   1112,      0,    153,    153,   sys,      1, StartBlobUploadToParent                 , """"                                              , /Root/(apps)/GenericContent/StartBlobUploadToParent
  1141,     19,   1112,      0,    154,    154,   sys,      1, TakeLockOver                            , """"                                              , /Root/(apps)/GenericContent/TakeLockOver
  1142,     19,   1112,      0,    155,    155,   sys,      1, RemoveAllowedChildTypes                 , """"                                              , /Root/(apps)/GenericContent/RemoveAllowedChildTypes
  1143,     19,   1112,      0,    156,    156,   sys,      1, GetRelatedItems                         , """"                                              , /Root/(apps)/GenericContent/GetRelatedItems
  1144,     19,   1112,      0,    157,    157,   sys,      1, GetRelatedIdentitiesByPermissions       , """"                                              , /Root/(apps)/GenericContent/GetRelatedIdentitiesByPermissions
  1145,     19,   1112,      0,    158,    158,   sys,      1, GetRelatedIdentities                    , """"                                              , /Root/(apps)/GenericContent/GetRelatedIdentities
  1146,     20,   1112,      0,    159,    159,   sys,      1, AddAspects                              , $Action,AddAspects                                , /Root/(apps)/GenericContent/AddAspects
  1147,     20,   1112,      0,    160,    160,   sys,      1, AddFields                               , $Action,AddFields                                 , /Root/(apps)/GenericContent/AddFields
  1148,     19,   1112,      0,    161,    161,   sys,      1, Ancestors                               , """"                                              , /Root/(apps)/GenericContent/Ancestors
  1149,     11,   1112,      0,    162,    162,   sys,      1, Approve                                 , $Action,Approve                                   , /Root/(apps)/GenericContent/Approve
  1150,     11,   1112,      0,    163,    163,   sys,      1, CheckIn                                 , $Action,CheckIn                                   , /Root/(apps)/GenericContent/CheckIn
  1151,     19,   1112,      0,    164,    164,   sys,      1, CheckIndexIntegrity                     , """"                                              , /Root/(apps)/GenericContent/CheckIndexIntegrity
  1152,     11,   1112,      0,    165,    165,   sys,      1, CheckOut                                , $Action,CheckOut                                  , /Root/(apps)/GenericContent/CheckOut
  1153,     19,   1112,      0,    166,    166,   sys,      1, CheckSecurityConsistency                , """"                                              , /Root/(apps)/GenericContent/CheckSecurityConsistency
  1154,     11,   1112,   3800,    167,    167,   sys,      1, CopyTo                                  , $Action,CopyTo                                    , /Root/(apps)/GenericContent/CopyTo
  1155,     11,   1112,   6000,    168,    168,   sys,      1, Delete                                  , $Action,Delete                                    , /Root/(apps)/GenericContent/Delete
  1156,     19,   1112,      0,    169,    169,   sys,      1, DocumentPreviewFinalizer                , """"                                              , /Root/(apps)/GenericContent/DocumentPreviewFinalizer
  1157,     19,   1112,      0,    170,    170,   sys,      1, FinalizeBlobUpload                      , """"                                              , /Root/(apps)/GenericContent/FinalizeBlobUpload
  1158,     19,   1112,      0,    171,    171,   sys,      1, FinalizeContent                         , """"                                              , /Root/(apps)/GenericContent/FinalizeContent
  1159,     11,   1112,      0,    172,    172,   sys,      1, ForceUndoCheckOut                       , $Action,ForceUndoCheckOut                         , /Root/(apps)/GenericContent/ForceUndoCheckOut
  1160,     19,   1112,      0,    173,    173,   sys,      1, GetAllContentTypes                      , """"                                              , /Root/(apps)/GenericContent/GetAllContentTypes
  1161,     19,   1112,      0,    174,    174,   sys,      1, GetAllowedChildTypesFromCTD             , """"                                              , /Root/(apps)/GenericContent/GetAllowedChildTypesFromCTD
  1162,     19,   1112,      0,    175,    175,   sys,      1, GetAllowedUsers                         , """"                                              , /Root/(apps)/GenericContent/GetAllowedUsers
  1163,     19,   1112,      0,    176,    176,   sys,      1, GetBinaryToken                          , """"                                              , /Root/(apps)/GenericContent/GetBinaryToken
  1164,     19,   1112,      0,    177,    177,   sys,      1, GetChildrenPermissionInfo               , """"                                              , /Root/(apps)/GenericContent/GetChildrenPermissionInfo
  1165,     19,   1112,      0,    178,    178,   sys,      1, GetExistingPreviewImages                , $Action,GetExistingPreviewImages                  , /Root/(apps)/GenericContent/GetExistingPreviewImages
  1166,     19,   1112,      0,    179,    179,   sys,      1, GetNameFromDisplayName                  , """"                                              , /Root/(apps)/GenericContent/GetNameFromDisplayName
  1167,     19,   1112,      0,    180,    180,   sys,      1, GetPermissionInfo                       , """"                                              , /Root/(apps)/GenericContent/GetPermissionInfo
  1168,     19,   1112,      0,    181,    181,   sys,      1, GetPermissionOverview                   , """"                                              , /Root/(apps)/GenericContent/GetPermissionOverview
  1169,     20,   1112,      0,    182,    182,   sys,      1, GetPermissions                          , $Action,GetPermissions                            , /Root/(apps)/GenericContent/GetPermissions
  1170,     19,   1112,      0,    183,    183,   sys,      1, GetPreviewImages                        , $Action,GetPreviewImages                          , /Root/(apps)/GenericContent/GetPreviewImages
  1171,     19,   1112,      0,    184,    184,   sys,      1, GetQueries                              , """"                                              , /Root/(apps)/GenericContent/GetQueries
  1172,     19,   1112,      0,    185,    185,   sys,      1, GetQueryBuilderMetadata                 , """"                                              , /Root/(apps)/GenericContent/GetQueryBuilderMetadata
  1173,     19,   1112,      0,    186,    186,   sys,      1, GetRecentIndexingActivities             , """"                                              , /Root/(apps)/GenericContent/GetRecentIndexingActivities
  1174,     19,   1112,      0,    187,    187,   sys,      1, GetRecentSecurityActivities             , """"                                              , /Root/(apps)/GenericContent/GetRecentSecurityActivities
  1175,     19,   1112,      0,    188,    188,   sys,      1, TakeOwnership                           , """"                                              , /Root/(apps)/GenericContent/TakeOwnership
  1176,     11,   1112,      0,    189,    189,   sys,      1, UndoCheckOut                            , $Action,UndoCheckOut                              , /Root/(apps)/GenericContent/UndoCheckOut
  1177,      1,   1090,      0,    190,    190,   sys,      1, Group                                   , """"                                              , /Root/(apps)/Group
  1178,     19,   1177,      0,    191,    191,   sys,      1, AddMembers                              , Add members                                       , /Root/(apps)/Group/AddMembers
  1179,     19,   1177,      0,    192,    192,   sys,      1, GetParentGroups                         , """"                                              , /Root/(apps)/Group/GetParentGroups
  1180,     19,   1177,      0,    193,    193,   sys,      1, RemoveMembers                           , Remove members                                    , /Root/(apps)/Group/RemoveMembers
  1181,      1,   1090,      0,    194,    194,   sys,      1, Image                                   , """"                                              , /Root/(apps)/Image
  1182,     22,   1181,      0,    195,    195,   sys,      1, Thumbnail                               , """"                                              , /Root/(apps)/Image/Thumbnail
  1183,      1,   1090,      0,    196,    196,   sys,      1, Link                                    , """"                                              , /Root/(apps)/Link
  1184,     11,   1183,      0,    197,    197,   sys,      1, Browse                                  , $Action,OpenLink                                  , /Root/(apps)/Link/Browse
  1185,      1,   1090,      0,    198,    198,   sys,      1, PortalRoot                              , """"                                              , /Root/(apps)/PortalRoot
  1186,     19,   1185,      0,    199,    199,   sys,      1, GetSchema                               , """"                                              , /Root/(apps)/PortalRoot/GetSchema
  1187,     19,   1185,      0,    200,    200,   sys,      1, GetVersionInfo                          , """"                                              , /Root/(apps)/PortalRoot/GetVersionInfo
  1188,      1,   1090,      0,    201,    201,   sys,      1, PreviewImage                            , """"                                              , /Root/(apps)/PreviewImage
  1189,     19,   1188,      0,    202,    202,   sys,      1, SetInitialPreviewProperties             , Set initial preview properties                    , /Root/(apps)/PreviewImage/SetInitialPreviewProperties
  1190,      1,   1090,      0,    203,    203,   sys,      1, This                                    , """"                                              , /Root/(apps)/This
  1191,     19,   1190,      0,    204,    204,   sys,      1, Decrypt                                 , """"                                              , /Root/(apps)/This/Decrypt
  1192,     19,   1190,      0,    205,    205,   sys,      1, Encrypt                                 , """"                                              , /Root/(apps)/This/Encrypt
  1193,      1,   1090,      0,    206,    206,   sys,      1, User                                    , """"                                              , /Root/(apps)/User
  1194,     19,   1193,      0,    207,    207,   sys,      1, GetParentGroups                         , """"                                              , /Root/(apps)/User/GetParentGroups
  1195,     19,   1193,      0,    208,    208,   sys,      1, Profile                                 , """"                                              , /Root/(apps)/User/Profile
  1196,     50,      1,      0,    209,    209,   ---,      1, Admin.png                               , Admin.png                                         , /Root/IMS/BuiltIn/Portal/Admin/Admin.png
  1197,      2,      5,      0,    210,    210,   ---,      1, ContentExplorers                        , ContentExplorers                                  , /Root/IMS/BuiltIn/Portal/ContentExplorers
  1198,      2,      5,      0,    211,    211,   ---,      1, Developers                              , Developers                                        , /Root/IMS/BuiltIn/Portal/Developers
  1199,      2,      5,      0,    212,    212,   ---,      1, Editors                                 , Editors                                           , /Root/IMS/BuiltIn/Portal/Editors
  1200,      2,      5,      0,    213,    213,   ---,      1, HR                                      , HR                                                , /Root/IMS/BuiltIn/Portal/HR
  1201,      2,      5,      0,    214,    214,   ---,      1, IdentifiedUsers                         , IdentifiedUsers                                   , /Root/IMS/BuiltIn/Portal/IdentifiedUsers
  1202,      2,      5,      0,    215,    215,   ---,      1, PageEditors                             , PageEditors                                       , /Root/IMS/BuiltIn/Portal/PageEditors
  1203,      2,      5,      0,    216,    216,   ---,      1, PRCViewers                              , PRCViewers                                        , /Root/IMS/BuiltIn/Portal/PRCViewers
  1204,      2,      5,      0,    217,    217,   ---,      1, RegisteredUsers                         , RegisteredUsers                                   , /Root/IMS/BuiltIn/Portal/RegisteredUsers
  1205,      3,      5,      0,    218,    218,   ---,      1, VirtualADUser                           , """"                                              , /Root/IMS/BuiltIn/Portal/VirtualADUser
  1206,     55,      2,      0,    219,    219,   sys,      1, Localization                            , """"                                              , /Root/Localization
  1207,     69,   1206,      0,    220,    220,   sys,      1, Content.xml                             , """"                                              , /Root/Localization/Content.xml
  1208,     69,   1206,      0,    221,    221,   sys,      1, CtdResourcesAB.xml                      , CtdResourcesAB.xml                                , /Root/Localization/CtdResourcesAB.xml
  1209,     69,   1206,      0,    222,    222,   sys,      1, CtdResourcesCD.xml                      , CtdResourcesCD.xml                                , /Root/Localization/CtdResourcesCD.xml
  1210,     69,   1206,      0,    223,    223,   sys,      1, CtdResourcesEF.xml                      , CtdResourcesEF.xml                                , /Root/Localization/CtdResourcesEF.xml
  1211,     69,   1206,      0,    224,    224,   sys,      1, CtdResourcesGH.xml                      , CtdResourcesGH.xml                                , /Root/Localization/CtdResourcesGH.xml
  1212,     69,   1206,      0,    225,    225,   sys,      1, CtdResourcesIJK.xml                     , CtdResourcesIJK.xml                               , /Root/Localization/CtdResourcesIJK.xml
  1213,     69,   1206,      0,    226,    226,   sys,      1, CtdResourcesLM.xml                      , CtdResourcesLM.xml                                , /Root/Localization/CtdResourcesLM.xml
  1214,     69,   1206,      0,    227,    227,   sys,      1, CtdResourcesNOP.xml                     , CtdResourcesNOP.xml                               , /Root/Localization/CtdResourcesNOP.xml
  1215,     69,   1206,      0,    228,    228,   sys,      1, CtdResourcesQ.xml                       , CtdResourcesQ.xml                                 , /Root/Localization/CtdResourcesQ.xml
  1216,     69,   1206,      0,    229,    229,   sys,      1, CtdResourcesRS.xml                      , CtdResourcesRS.xml                                , /Root/Localization/CtdResourcesRS.xml
  1217,     69,   1206,      0,    230,    230,   sys,      1, CtdResourcesTZ.xml                      , CtdResourcesTZ.xml                                , /Root/Localization/CtdResourcesTZ.xml
  1218,     69,   1206,      0,    231,    231,   sys,      1, Exceptions.xml                          , """"                                              , /Root/Localization/Exceptions.xml
  1219,     69,   1206,      0,    232,    232,   sys,      1, Sharing.xml                             , """"                                              , /Root/Localization/Sharing.xml
  1220,     69,   1206,      0,    233,    233,   sys,      1, Trash.xml                               , """"                                              , /Root/Localization/Trash.xml
  1221,      5,   1000,      0,    234,    234,   sys,      1, ErrorMessages                           , """"                                              , /Root/System/ErrorMessages
  1222,      5,   1221,      0,    235,    235,   sys,      1, Default                                 , """"                                              , /Root/System/ErrorMessages/Default
  1223,     15,   1222,      0,    236,    236,   sys,      1, Global.html                             , """"                                              , /Root/System/ErrorMessages/Default/Global.html
  1224,     15,   1222,      0,    237,    237,   sys,      1, UserGlobal.html                         , """"                                              , /Root/System/ErrorMessages/Default/UserGlobal.html
  1225,      5,   1001,      0,    238,    238,   sys,      1, Metadata                                , Metadata                                          , /Root/System/Schema/Metadata
  1226,      5,   1225,      0,    239,    239,   sys,      1, TypeScript                              , TypeScript                                        , /Root/System/Schema/Metadata/TypeScript
  1227,     54,   1226,      0,    240,    240,   sys,      1, complextypes.ts                         , """"                                              , /Root/System/Schema/Metadata/TypeScript/complextypes.ts
  1228,     54,   1226,      0,    241,    241,   sys,      1, contenttypes.ts                         , """"                                              , /Root/System/Schema/Metadata/TypeScript/contenttypes.ts
  1229,     54,   1226,      0,    242,    242,   sys,      1, enums.ts                                , """"                                              , /Root/System/Schema/Metadata/TypeScript/enums.ts
  1230,     54,   1226,      0,    243,    243,   sys,      1, fieldsettings.ts                        , """"                                              , /Root/System/Schema/Metadata/TypeScript/fieldsettings.ts
  1231,     54,   1226,      0,    244,    244,   sys,      1, meta.zip                                , """"                                              , /Root/System/Schema/Metadata/TypeScript/meta.zip
  1232,     54,   1226,      0,    245,    245,   sys,      1, resources.ts                            , """"                                              , /Root/System/Schema/Metadata/TypeScript/resources.ts
  1233,     54,   1226,      0,    246,    246,   sys,      1, schemas.ts                              , """"                                              , /Root/System/Schema/Metadata/TypeScript/schemas.ts
  1234,      5,   1000,      0,    247,    247,   sys,      1, WebRoot                                 , """"                                              , /Root/System/WebRoot
  1235,     48,   1234,      0,    248,    248,   sys,      1, binaryhandler.ashx                      , binaryhandler.ashx                                , /Root/System/WebRoot/binaryhandler.ashx
  1236,      1,   1234,      0,    249,    249,   sys,      1, DWS                                     , DWS                                               , /Root/System/WebRoot/DWS
  1237,     48,   1236,      0,    250,    250,   sys,      1, Dws.asmx                                , """"                                              , /Root/System/WebRoot/DWS/Dws.asmx
  1238,     48,   1236,      0,    251,    251,   sys,      1, Fpp.ashx                                , """"                                              , /Root/System/WebRoot/DWS/Fpp.ashx
  1239,     48,   1236,      0,    252,    252,   sys,      1, Lists.asmx                              , """"                                              , /Root/System/WebRoot/DWS/Lists.asmx
  1240,     48,   1236,      0,    253,    253,   sys,      1, owssvr.aspx                             , """"                                              , /Root/System/WebRoot/DWS/owssvr.aspx
  1241,     48,   1236,      0,    254,    254,   sys,      1, Versions.asmx                           , """"                                              , /Root/System/WebRoot/DWS/Versions.asmx
  1242,     48,   1236,      0,    255,    255,   sys,      1, Webs.asmx                               , """"                                              , /Root/System/WebRoot/DWS/Webs.asmx
  1243,     48,   1234,      0,    256,    256,   sys,      1, vsshandler.ashx                         , vsshandler.ashx                                   , /Root/System/WebRoot/vsshandler.ashx
  1244,     60,      2,      0,    257,    257,   ---,      1, Trash                                   , """"                                              , /Root/Trash
  1245,      5,   1244,      0,    258,    258,   sys,      1, (apps)                                  , """"                                              , /Root/Trash/(apps)
  1246,      1,   1245,      0,    259,    259,   sys,      1, TrashBag                                , """"                                              , /Root/Trash/(apps)/TrashBag
  1247,     11,   1246,      0,    260,    260,   sys,      1, Restore                                 , $Action,Restore                                   , /Root/Trash/(apps)/TrashBag/Restore
";
        #endregion

        #region private static string _versions = @"
        private static string _versions = @"
VersionId, NodeId,  Version
---------- ------- ---------
        1,      1,  V1.0.A
        2,      2,  V1.0.A
        3,      3,  V1.0.A
        4,      4,  V1.0.A
        5,      5,  V1.0.A
        6,      6,  V1.0.A
        7,      7,  V1.0.A
        8,      8,  V1.0.A
        9,      9,  V1.0.A
       10,     10,  V1.0.A
       11,     11,  V1.0.A
       12,     12,  V1.0.A
       13,   1000,  V1.0.A
       14,   1001,  V1.0.A
       15,   1002,  V1.0.A
       16,   1003,  V1.0.A
       17,   1004,  V1.0.A
       18,   1005,  V1.0.A
       19,   1006,  V1.0.A
       20,   1007,  V1.0.A
       21,   1008,  V1.0.A
       22,   1009,  V1.0.A
       23,   1010,  V1.0.A
       24,   1011,  V1.0.A
       25,   1012,  V1.0.A
       26,   1013,  V1.0.A
       27,   1014,  V1.0.A
       28,   1015,  V1.0.A
       29,   1016,  V1.0.A
       30,   1017,  V1.0.A
       31,   1018,  V1.0.A
       32,   1019,  V1.0.A
       33,   1020,  V1.0.A
       34,   1021,  V1.0.A
       35,   1022,  V1.0.A
       36,   1023,  V1.0.A
       37,   1024,  V1.0.A
       38,   1025,  V1.0.A
       39,   1026,  V1.0.A
       40,   1027,  V1.0.A
       41,   1028,  V1.0.A
       42,   1029,  V1.0.A
       43,   1030,  V1.0.A
       44,   1031,  V1.0.A
       45,   1032,  V1.0.A
       46,   1033,  V1.0.A
       47,   1034,  V1.0.A
       48,   1035,  V1.0.A
       49,   1036,  V1.0.A
       50,   1037,  V1.0.A
       51,   1038,  V1.0.A
       52,   1039,  V1.0.A
       53,   1040,  V1.0.A
       54,   1041,  V1.0.A
       55,   1042,  V1.0.A
       56,   1043,  V1.0.A
       57,   1044,  V1.0.A
       58,   1045,  V1.0.A
       59,   1046,  V1.0.A
       60,   1047,  V1.0.A
       61,   1048,  V1.0.A
       62,   1049,  V1.0.A
       63,   1050,  V1.0.A
       64,   1051,  V1.0.A
       65,   1052,  V1.0.A
       66,   1053,  V1.0.A
       67,   1054,  V1.0.A
       68,   1055,  V1.0.A
       69,   1056,  V1.0.A
       70,   1057,  V1.0.A
       71,   1058,  V1.0.A
       72,   1059,  V1.0.A
       73,   1060,  V1.0.A
       74,   1061,  V1.0.A
       75,   1062,  V1.0.A
       76,   1063,  V1.0.A
       77,   1064,  V1.0.A
       78,   1065,  V1.0.A
       79,   1066,  V1.0.A
       80,   1067,  V1.0.A
       81,   1068,  V1.0.A
       82,   1069,  V1.0.A
       83,   1070,  V1.0.A
       84,   1071,  V1.0.A
       85,   1072,  V1.0.A
       86,   1073,  V1.0.A
       87,   1074,  V1.0.A
       88,   1075,  V1.0.A
       89,   1076,  V1.0.A
       90,   1077,  V1.0.A
       91,   1078,  V1.0.A
       92,   1079,  V1.0.A
       93,   1080,  V1.0.A
       94,   1081,  V1.0.A
       95,   1082,  V1.0.A
       96,   1083,  V1.0.A
       97,   1084,  V1.0.A
       98,   1085,  V1.0.A
       99,   1086,  V1.0.A
      100,   1087,  V1.0.A
      101,   1088,  V1.0.A
      102,   1089,  V1.0.A
      103,   1090,  V1.0.A
      104,   1091,  V1.0.A
      105,   1092,  V1.0.A
      106,   1093,  V1.0.A
      107,   1094,  V1.0.A
      108,   1095,  V1.0.A
      109,   1096,  V1.0.A
      110,   1097,  V1.0.A
      111,   1098,  V1.0.A
      112,   1099,  V1.0.A
      113,   1100,  V1.0.A
      114,   1101,  V1.0.A
      115,   1102,  V1.0.A
      116,   1103,  V1.0.A
      117,   1104,  V1.0.A
      118,   1105,  V1.0.A
      119,   1106,  V1.0.A
      120,   1107,  V1.0.A
      121,   1108,  V1.0.A
      122,   1109,  V1.0.A
      123,   1110,  V1.0.A
      124,   1111,  V1.0.A
      125,   1112,  V1.0.A
      126,   1113,  V1.0.A
      127,   1114,  V1.0.A
      128,   1115,  V1.0.A
      129,   1116,  V1.0.A
      130,   1117,  V1.0.A
      131,   1118,  V1.0.A
      132,   1119,  V1.0.A
      133,   1120,  V1.0.A
      134,   1121,  V1.0.A
      135,   1122,  V1.0.A
      136,   1123,  V1.0.A
      137,   1124,  V1.0.A
      138,   1125,  V1.0.A
      139,   1126,  V1.0.A
      140,   1127,  V1.0.A
      141,   1128,  V1.0.A
      142,   1129,  V1.0.A
      143,   1130,  V1.0.A
      144,   1131,  V1.0.A
      145,   1132,  V1.0.A
      146,   1133,  V1.0.A
      147,   1134,  V1.0.A
      148,   1135,  V1.0.A
      149,   1136,  V1.0.A
      150,   1137,  V1.0.A
      151,   1138,  V1.0.A
      152,   1139,  V1.0.A
      153,   1140,  V1.0.A
      154,   1141,  V1.0.A
      155,   1142,  V1.0.A
      156,   1143,  V1.0.A
      157,   1144,  V1.0.A
      158,   1145,  V1.0.A
      159,   1146,  V1.0.A
      160,   1147,  V1.0.A
      161,   1148,  V1.0.A
      162,   1149,  V1.0.A
      163,   1150,  V1.0.A
      164,   1151,  V1.0.A
      165,   1152,  V1.0.A
      166,   1153,  V1.0.A
      167,   1154,  V1.0.A
      168,   1155,  V1.0.A
      169,   1156,  V1.0.A
      170,   1157,  V1.0.A
      171,   1158,  V1.0.A
      172,   1159,  V1.0.A
      173,   1160,  V1.0.A
      174,   1161,  V1.0.A
      175,   1162,  V1.0.A
      176,   1163,  V1.0.A
      177,   1164,  V1.0.A
      178,   1165,  V1.0.A
      179,   1166,  V1.0.A
      180,   1167,  V1.0.A
      181,   1168,  V1.0.A
      182,   1169,  V1.0.A
      183,   1170,  V1.0.A
      184,   1171,  V1.0.A
      185,   1172,  V1.0.A
      186,   1173,  V1.0.A
      187,   1174,  V1.0.A
      188,   1175,  V1.0.A
      189,   1176,  V1.0.A
      190,   1177,  V1.0.A
      191,   1178,  V1.0.A
      192,   1179,  V1.0.A
      193,   1180,  V1.0.A
      194,   1181,  V1.0.A
      195,   1182,  V1.0.A
      196,   1183,  V1.0.A
      197,   1184,  V1.0.A
      198,   1185,  V1.0.A
      199,   1186,  V1.0.A
      200,   1187,  V1.0.A
      201,   1188,  V1.0.A
      202,   1189,  V1.0.A
      203,   1190,  V1.0.A
      204,   1191,  V1.0.A
      205,   1192,  V1.0.A
      206,   1193,  V1.0.A
      207,   1194,  V1.0.A
      208,   1195,  V1.0.A
      209,   1196,  V1.0.A
      210,   1197,  V1.0.A
      211,   1198,  V1.0.A
      212,   1199,  V1.0.A
      213,   1200,  V1.0.A
      214,   1201,  V1.0.A
      215,   1202,  V1.0.A
      216,   1203,  V1.0.A
      217,   1204,  V1.0.A
      218,   1205,  V1.0.A
      219,   1206,  V1.0.A
      220,   1207,  V1.0.A
      221,   1208,  V1.0.A
      222,   1209,  V1.0.A
      223,   1210,  V1.0.A
      224,   1211,  V1.0.A
      225,   1212,  V1.0.A
      226,   1213,  V1.0.A
      227,   1214,  V1.0.A
      228,   1215,  V1.0.A
      229,   1216,  V1.0.A
      230,   1217,  V1.0.A
      231,   1218,  V1.0.A
      232,   1219,  V1.0.A
      233,   1220,  V1.0.A
      234,   1221,  V1.0.A
      235,   1222,  V1.0.A
      236,   1223,  V1.0.A
      237,   1224,  V1.0.A
      238,   1225,  V1.0.A
      239,   1226,  V1.0.A
      240,   1227,  V1.0.A
      241,   1228,  V1.0.A
      242,   1229,  V1.0.A
      243,   1230,  V1.0.A
      244,   1231,  V1.0.A
      245,   1232,  V1.0.A
      246,   1233,  V1.0.A
      247,   1234,  V1.0.A
      248,   1235,  V1.0.A
      249,   1236,  V1.0.A
      250,   1237,  V1.0.A
      251,   1238,  V1.0.A
      252,   1239,  V1.0.A
      253,   1240,  V1.0.A
      254,   1241,  V1.0.A
      255,   1242,  V1.0.A
      256,   1243,  V1.0.A
      257,   1244,  V1.0.A
      258,   1245,  V1.0.A
      259,   1246,  V1.0.A
      260,   1247,  V1.0.A
";
        #endregion

        #region private static string _dynamicData = @"
        private static string _dynamicData = @"
VersionId: 1
    DynamicProperties
        Enabled: 1
        Domain: BuiltIn
        FullName: Admin
        PasswordHash: $2a$10$PpzkmffYtUA5XV5nekcqVOKIZUpB8HUczoFcCmTkAUtCqUH5dS5Ki
        LoginName: Admin
        LastLoggedOut: 2018-11-14T02:54:02.0000000Z
        OldPasswords: <?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">    <OldPasswordData>      <ModificationDate>2018-11-14T02:54:02.7522515Z</ModificationDate>      <Hash>$2a$10$PpzkmffYtUA5XV5nekcqVOKIZUpB8HUczoFcCmTkAUtCqUH5dS5Ki</Hash>    </OldPasswordData>  </ArrayOfOldPasswordData>
VersionId: 6
    DynamicProperties
        Enabled: 1
        Domain: BuiltIn
        FullName: Visitor
        LoginName: Visitor
        OldPasswords: <?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" />
VersionId: 7
    DynamicProperties
        Members: [1,1198]
VersionId: 10
    DynamicProperties
        Enabled: 1
        Domain: BuiltIn
        FullName: Somebody
        PasswordHash: $2a$10$4l2GIJAN16.vVsBDbGXeEuQVWC2KvOrBpzvk97S32SgeyWq1Tm3ke
        LoginName: Somebody
        LastLoggedOut: 2018-11-14T02:54:03.0000000Z
        OldPasswords: <?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">    <OldPasswordData>      <ModificationDate>2018-11-14T02:54:03.2735274Z</ModificationDate>      <Hash>$2a$10$4l2GIJAN16.vVsBDbGXeEuQVWC2KvOrBpzvk97S32SgeyWq1Tm3ke</Hash>    </OldPasswordData>  </ArrayOfOldPasswordData>
VersionId: 11
    DynamicProperties
        Members: [7]
        Description: Members of this group are able to perform administrative tasks in the Content Repository - e.g. importing the creation date of content.
VersionId: 12
    DynamicProperties
        Enabled: 1
        Domain: BuiltIn
        FullName: Startup User
        PasswordHash: $2a$10$Ji1vBnecMjLLL7x70y8hOOwpUsvEQf.Xyjv1D9DQ1L/G/BOiYHS2G
        LoginName: Startup
        LastLoggedOut: 2018-11-14T02:54:03.0000000Z
        OldPasswords: <?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">    <OldPasswordData>      <ModificationDate>2018-11-14T02:54:03.4916714Z</ModificationDate>      <Hash>$2a$10$Ji1vBnecMjLLL7x70y8hOOwpUsvEQf.Xyjv1D9DQ1L/G/BOiYHS2G</Hash>    </OldPasswordData>  </ArrayOfOldPasswordData>
VersionId: 13
    DynamicProperties
        Hidden: 1
VersionId: 14
    DynamicProperties
        AllowedChildTypes: SystemFolder
VersionId: 15
    DynamicProperties
        AllowedChildTypes: ContentType
VersionId: 17
    BinaryProperties
        Binary: #1, F1, 16010L, ContentType.ContentType, text/xml, /Root/System/Schema/ContentTypes/ContentType
VersionId: 18
    BinaryProperties
        Binary: #2, F2, 31386L, GenericContent.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent
VersionId: 19
    BinaryProperties
        Binary: #3, F3, 7628L, Application.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application
VersionId: 20
    BinaryProperties
        Binary: #4, F4, 421L, ApplicationOverride.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/ApplicationOverride
VersionId: 21
    BinaryProperties
        Binary: #5, F5, 1065L, Folder.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder
VersionId: 22
    BinaryProperties
        Binary: #6, F6, 8436L, ContentList.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList
VersionId: 23
    BinaryProperties
        Binary: #7, F7, 6916L, Aspect.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Aspect
VersionId: 24
    BinaryProperties
        Binary: #8, F8, 1297L, FieldSettingContent.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent
VersionId: 25
    BinaryProperties
        Binary: #9, F9, 381L, BinaryFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/BinaryFieldSetting
VersionId: 26
    BinaryProperties
        Binary: #10, F10, 390L, TextFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting
VersionId: 27
    BinaryProperties
        Binary: #11, F11, 397L, ShortTextFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting
VersionId: 28
    BinaryProperties
        Binary: #12, F12, 393L, ChoiceFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting
VersionId: 29
    BinaryProperties
        Binary: #13, F13, 1143L, ContentLink.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/ContentLink
VersionId: 30
    BinaryProperties
        Binary: #14, F14, 391L, NumberFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting
VersionId: 31
    BinaryProperties
        Binary: #15, F15, 396L, CurrencyFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting/CurrencyFieldSetting
VersionId: 32
    BinaryProperties
        Binary: #16, F16, 2002L, ItemList.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList
VersionId: 33
    BinaryProperties
        Binary: #17, F17, 472L, CustomList.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/CustomList
VersionId: 34
    BinaryProperties
        Binary: #18, F18, 1048L, ListItem.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/ListItem
VersionId: 35
    BinaryProperties
        Binary: #19, F19, 2348L, CustomListItem.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/ListItem/CustomListItem
VersionId: 36
    BinaryProperties
        Binary: #20, F20, 397L, DateTimeFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/DateTimeFieldSetting
VersionId: 37
    BinaryProperties
        Binary: #21, F21, 608L, Device.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Device
VersionId: 38
    BinaryProperties
        Binary: #22, F22, 1305L, Library.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library
VersionId: 39
    BinaryProperties
        Binary: #23, F23, 489L, DocumentLibrary.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary
VersionId: 40
    BinaryProperties
        Binary: #24, F24, 2081L, Domain.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Domain
VersionId: 41
    BinaryProperties
        Binary: #25, F25, 449L, Domains.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Domains
VersionId: 42
    BinaryProperties
        Binary: #26, F26, 5147L, File.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File
VersionId: 43
    BinaryProperties
        Binary: #27, F27, 343L, DynamicJsonContent.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/DynamicJsonContent
VersionId: 44
    BinaryProperties
        Binary: #28, F28, 1434L, Email.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Email
VersionId: 45
    BinaryProperties
        Binary: #29, F29, 419L, ExecutableFile.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/ExecutableFile
VersionId: 46
    BinaryProperties
        Binary: #30, F30, 445L, ExportToCsvApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/ExportToCsvApplication
VersionId: 47
    BinaryProperties
        Binary: #31, F31, 1173L, GenericODataApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/GenericODataApplication
VersionId: 48
    BinaryProperties
        Binary: #32, F32, 454L, HttpHandlerApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication
VersionId: 49
    BinaryProperties
        Binary: #33, F33, 350L, GetMetadataApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication/GetMetadataApplication
VersionId: 50
    BinaryProperties
        Binary: #34, F34, 1979L, Group.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Group
VersionId: 51
    BinaryProperties
        Binary: #35, F35, 841L, HtmlTemplate.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/HtmlTemplate
VersionId: 52
    BinaryProperties
        Binary: #36, F36, 1582L, HttpStatusApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/HttpStatusApplication
VersionId: 53
    BinaryProperties
        Binary: #37, F37, 400L, HyperLinkFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/HyperLinkFieldSetting
VersionId: 54
    BinaryProperties
        Binary: #38, F38, 1573L, Image.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/Image
VersionId: 55
    BinaryProperties
        Binary: #39, F39, 1031L, ImageLibrary.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/ImageLibrary
VersionId: 56
    BinaryProperties
        Binary: #40, F40, 8975L, ImgResizeApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/ImgResizeApplication
VersionId: 57
    BinaryProperties
        Binary: #41, F41, 1638L, Settings.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/Settings
VersionId: 58
    BinaryProperties
        Binary: #42, F42, 854L, IndexingSettings.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/Settings/IndexingSettings
VersionId: 59
    BinaryProperties
        Binary: #43, F43, 393L, IntegerFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/IntegerFieldSetting
VersionId: 60
    BinaryProperties
        Binary: #44, F44, 387L, LoggingSettings.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/Settings/LoggingSettings
VersionId: 61
    BinaryProperties
        Binary: #45, F45, 395L, LongTextFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/LongTextFieldSetting
VersionId: 62
    BinaryProperties
        Binary: #46, F46, 1874L, Memo.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/ListItem/Memo
VersionId: 63
    BinaryProperties
        Binary: #47, F47, 462L, MemoList.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList
VersionId: 64
    BinaryProperties
        Binary: #48, F48, 377L, NullFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NullFieldSetting
VersionId: 65
    BinaryProperties
        Binary: #49, F49, 2100L, OrganizationalUnit.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/OrganizationalUnit
VersionId: 66
    BinaryProperties
        Binary: #50, F50, 400L, PasswordFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/PasswordFieldSetting
VersionId: 67
    BinaryProperties
        Binary: #51, F51, 400L, PermissionChoiceFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/PermissionChoiceFieldSetting
VersionId: 68
    BinaryProperties
        Binary: #52, F52, 799L, PortalRoot.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/PortalRoot
VersionId: 69
    BinaryProperties
        Binary: #53, F53, 378L, PortalSettings.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/Settings/PortalSettings
VersionId: 70
    BinaryProperties
        Binary: #54, F54, 508L, PreviewImage.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/Image/PreviewImage
VersionId: 71
    BinaryProperties
        Binary: #55, F55, 1262L, ProfileDomain.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ProfileDomain
VersionId: 72
    BinaryProperties
        Binary: #56, F56, 459L, Profiles.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Profiles
VersionId: 73
    BinaryProperties
        Binary: #57, F57, 1267L, Query.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Query
VersionId: 74
    BinaryProperties
        Binary: #58, F58, 400L, ReferenceFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/ReferenceFieldSetting
VersionId: 75
    BinaryProperties
        Binary: #59, F59, 630L, SystemFile.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/SystemFile
VersionId: 76
    BinaryProperties
        Binary: #60, F60, 1682L, Resource.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/Resource
VersionId: 77
    BinaryProperties
        Binary: #61, F61, 622L, SystemFolder.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder
VersionId: 78
    BinaryProperties
        Binary: #62, F62, 469L, Resources.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Resources
VersionId: 79
    BinaryProperties
        Binary: #63, F63, 413L, RssApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/RssApplication
VersionId: 80
    BinaryProperties
        Binary: #64, F64, 448L, RuntimeContentContainer.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/RuntimeContentContainer
VersionId: 81
    BinaryProperties
        Binary: #65, F65, 1162L, SharingGroup.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Group/SharingGroup
VersionId: 82
    BinaryProperties
        Binary: #66, F66, 4337L, Workspace.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace
VersionId: 83
    BinaryProperties
        Binary: #67, F67, 6791L, Site.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Site
VersionId: 84
    BinaryProperties
        Binary: #68, F68, 439L, Sites.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Sites
VersionId: 85
    BinaryProperties
        Binary: #69, F69, 1712L, SmartFolder.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder
VersionId: 86
    BinaryProperties
        Binary: #70, F70, 4383L, Task.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/ListItem/Task
VersionId: 87
    BinaryProperties
        Binary: #71, F71, 508L, TaskList.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList
VersionId: 88
    BinaryProperties
        Binary: #72, F72, 2662L, TrashBag.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/TrashBag
VersionId: 89
    BinaryProperties
        Binary: #73, F73, 4688L, TrashBin.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TrashBin
VersionId: 90
    BinaryProperties
        Binary: #74, F74, 13645L, User.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/User
VersionId: 91
    BinaryProperties
        Binary: #75, F75, 2151L, UserProfile.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/UserProfile
VersionId: 92
    BinaryProperties
        Binary: #76, F76, 620L, WebServiceApplication.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/Application/WebServiceApplication
VersionId: 93
    BinaryProperties
        Binary: #77, F77, 382L, XmlFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/XmlFieldSetting
VersionId: 94
    BinaryProperties
        Binary: #78, F78, 387L, YesNoFieldSetting.ContentType, text/xml, /Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/YesNoFieldSetting
VersionId: 95
    BinaryProperties
        Binary: #79, F79, 0L, Indexing.settings, application/octet-stream, /Root/System/Settings/Indexing.settings
    DynamicProperties
        PageCount: -4
        GlobalOnly: 1
VersionId: 96
    BinaryProperties
        Binary: #80, F80, 0L, Logging.settings, application/octet-stream, /Root/System/Settings/Logging.settings
    DynamicProperties
        PageCount: -4
        GlobalOnly: 1
VersionId: 97
    BinaryProperties
        Binary: #81, F81, 0L, MailProcessor.settings, application/octet-stream, /Root/System/Settings/MailProcessor.settings
    DynamicProperties
        PageCount: -4
VersionId: 98
    BinaryProperties
        Binary: #82, F82, 0L, OAuth.settings, application/octet-stream, /Root/System/Settings/OAuth.settings
    DynamicProperties
        PageCount: -4
        GlobalOnly: 1
VersionId: 99
    BinaryProperties
        Binary: #83, F83, 0L, Portal.settings, application/octet-stream, /Root/System/Settings/Portal.settings
    DynamicProperties
        PageCount: -4
VersionId: 100
    BinaryProperties
        Binary: #84, F84, 0L, Sharing.settings, application/octet-stream, /Root/System/Settings/Sharing.settings
    DynamicProperties
        PageCount: -4
VersionId: 101
    BinaryProperties
        Binary: #85, F85, 0L, TaskManagement.settings, application/octet-stream, /Root/System/Settings/TaskManagement.settings
    DynamicProperties
        PageCount: -4
        GlobalOnly: 1
VersionId: 102
    BinaryProperties
        Binary: #86, F86, 0L, UserProfile.settings, application/octet-stream, /Root/System/Settings/UserProfile.settings
    DynamicProperties
        PageCount: -4
        GlobalOnly: 1
VersionId: 103
    DynamicProperties
        Hidden: 1
VersionId: 105
    DynamicProperties
        AppName: DeleteField
        ActionTypeName: DeleteFieldAction
        RequiredPermissions: _________________________________________________________***___*
        CacheControl: Nondefined
        StoredIcon: delete
VersionId: 106
    DynamicProperties
        AppName: EditField
        ActionTypeName: EditFieldAction
        RequiredPermissions: _________________________________________________________***___*
        CacheControl: Nondefined
        StoredIcon: edit
VersionId: 107
    BinaryProperties
        Binary: #87, F87, 0L, ExchangeService.asmx, application/octet-stream, /Root/(apps)/ContentList/ExchangeService.asmx
    DynamicProperties
        AppName: ExchangeService
        CacheControl: Nondefined
        StoredIcon: application
VersionId: 109
    DynamicProperties
        AppName: CheckPreviews
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: CheckPreviews
        Parameters: bool generateMissing
VersionId: 110
    DynamicProperties
        AppName: EditInMicrosoftOffice
        Scenario: ListItem
        ActionTypeName: WebdavOpenAction
        RequiredPermissions: _________________________________________________________***___*
        CacheControl: Nondefined
        StoredIcon: application
VersionId: 111
    DynamicProperties
        AppName: ExportToPdf
        Scenario: ListItem;DocumentDetails
        ActionTypeName: ExportToPdfAction
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        StoredIcon: acrobat
        Description: 
VersionId: 112
    DynamicProperties
        AppName: GetPageCount
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: GetPageCount
VersionId: 113
    DynamicProperties
        AppName: GetPreviewsFolder
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: GetPreviewsFolder
        Parameters: bool empty
VersionId: 114
    DynamicProperties
        AppName: PreviewAvailable
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: PreviewAvailable
        Parameters: int page
VersionId: 115
    DynamicProperties
        AppName: RegeneratePreviews
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: RegeneratePreviews
VersionId: 116
    DynamicProperties
        AppName: SetPageCount
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: SetPageCount
        Parameters: int pageCount
VersionId: 117
    DynamicProperties
        AppName: SetPreviewStatus
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: SetPreviewStatus
        Parameters: SenseNet.Preview.PreviewStatus status
VersionId: 118
    DynamicProperties
        AppName: UploadResume
        Scenario: ListItem
        ActionTypeName: UploadResumeAction
        RequiredPermissions: _________________________________________________________**_____
        CacheControl: Nondefined
        StoredIcon: upload
VersionId: 120
    DynamicProperties
        AppName: CopyBatch
        Scenario: 
        ActionTypeName: CopyBatchAction
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: copy
        Description: 
VersionId: 121
    DynamicProperties
        AppName: DeleteBatch
        Scenario: GridToolbar
        ActionTypeName: DeleteBatchAction
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: delete
        Description: 
VersionId: 122
    DynamicProperties
        AppName: ExportToCsv
        Scenario: ListActions;ExploreActions
        ActionTypeName: 
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: download
        Description: 
VersionId: 123
    DynamicProperties
        AppName: MoveBatch
        Scenario: GridToolbar
        ActionTypeName: MoveBatchAction
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: move
        Description: 
VersionId: 124
    DynamicProperties
        AppName: Upload
        ActionTypeName: UploadAction
        RequiredPermissions: ______________________________________________________*____*___*
        CacheControl: Nondefined
        StoredIcon: upload
VersionId: 126
    DynamicProperties
        AppName: AddAllowedChildTypes
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.GenericContent
        MethodName: AddAllowedChildTypes
        Parameters: string[] contentTypes
VersionId: 127
    DynamicProperties
        AppName: GetRelatedPermissions
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetRelatedPermissions
        Parameters:         string level,        bool explicitOnly,        string member,        string[] includedTypes      
VersionId: 128
    DynamicProperties
        AppName: GetSharing
        RequiredPermissions: ________________________________________________*________*______
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Sharing.SharingActions
        MethodName: GetSharing
VersionId: 129
    DynamicProperties
        AppName: GetTemplateScript
        CacheControl: Nondefined
        ClassName: SenseNet.Portal.UI.HtmlTemplate
        MethodName: GetTemplateScript
        Parameters: string skin, string category
VersionId: 130
    DynamicProperties
        AppName: HasPermission
        ActionTypeName: HasPermissionAction
        StyleHint: 
        RequiredPermissions: _________________________________________________*______________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 131
    DynamicProperties
        AppName: Login
        CacheControl: Nondefined
        ClassName: SenseNet.Portal.Virtualization.AuthenticationHelper
        MethodName: Login
        Parameters:         string username,      string password      
VersionId: 132
    DynamicProperties
        AppName: Logout
        Scenario: UserActions
        ActionTypeName: LogoutAction
        IncludeBackUrl: False
        CacheControl: Nondefined
        StoredIcon: logout
        Description: 
VersionId: 133
    DynamicProperties
        AppName: MoveTo
        Scenario: ListItem;ExploreActions;ManageViewsListItem
        ActionTypeName: MoveToAction
        StyleHint: 
        RequiredPermissions: _________________________________________________________**_____
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: move
        Description: 
VersionId: 134
    DynamicProperties
        AppName: Publish
        Scenario: ListItem;ExploreActions;SimpleApprovableListItem
        ActionTypeName: PublishAction
        RequiredPermissions: ________________________________________________________****___*
        CacheControl: Nondefined
        StoredIcon: publish
VersionId: 135
    DynamicProperties
        AppName: RebuildIndex
        RequiredPermissions: _________________________________________________________*______
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Content+Operations
        MethodName: RebuildIndex
        Parameters:      bool recursive,      SenseNet.ContentRepository.Search.Indexing.IndexRebuildLevel rebuildLevel      
VersionId: 136
    DynamicProperties
        AppName: RebuildIndexSubtree
        RequiredPermissions: _________________________________________________________*______
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Content+Operations
        MethodName: RebuildIndexSubtree
VersionId: 137
    DynamicProperties
        AppName: RefreshIndexSubtree
        RequiredPermissions: _________________________________________________________*______
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Content+Operations
        MethodName: RefreshIndexSubtree
VersionId: 138
    DynamicProperties
        AppName: Reject
        ActionTypeName: RejectAction
        IncludeBackUrl: Default
        CacheControl: Nondefined
VersionId: 139
    DynamicProperties
        AppName: RemoveAllAspects
        ActionTypeName: RemoveAllAspectsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 140
    DynamicProperties
        AppName: GetRelatedItemsOneLevel
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetRelatedItemsOneLevel
        Parameters:         string level,        string member,        string[] permissions      
VersionId: 141
    DynamicProperties
        AppName: RemoveAllFields
        ActionTypeName: RemoveAllFieldsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 142
    DynamicProperties
        AppName: RemoveAspects
        ActionTypeName: RemoveAspectsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 143
    DynamicProperties
        AppName: RemoveFields
        ActionTypeName: RemoveFieldsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 144
    DynamicProperties
        AppName: RemoveSharing
        RequiredPermissions: ________________________________________________*________*______
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Sharing.SharingActions
        MethodName: RemoveSharing
        Parameters:         string id      
VersionId: 145
    DynamicProperties
        AppName: ResetRecentIndexingActivities
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: ResetRecentIndexingActivities
        Description: 
        Parameters: 
VersionId: 146
    DynamicProperties
        AppName: RestoreVersion
        ActionTypeName: RestoreVersionAction
        RequiredPermissions: ___________________________________________________*_____*______
        CacheControl: Nondefined
        StoredIcon: restoreversion
VersionId: 147
    DynamicProperties
        AppName: RetrieveFields
        ActionTypeName: RetrieveFieldsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 148
    DynamicProperties
        AppName: Rss
        Scenario: ListActions
        ActionTypeName: 
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: rss
        Description: 
VersionId: 149
    DynamicProperties
        AppName: SaveQuery
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.Portal.QueryBuilder
        MethodName: SaveQuery
        Parameters:         string query,        string displayName,        string queryType      
VersionId: 150
    DynamicProperties
        AppName: SetPermissions
        Scenario: WorkspaceActions;ListItem;ExploreActions
        ActionTypeName: SetPermissionsAction
        RequiredPermissions: ________________________________________________**_________*____
        CacheControl: Nondefined
        StoredIcon: security
VersionId: 151
    DynamicProperties
        AppName: Share
        RequiredPermissions: ________________________________________________*________*______
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Sharing.SharingActions
        MethodName: Share
        Parameters:         string token, SenseNet.ContentRepository.Sharing.SharingLevel level,         SenseNet.ContentRepository.Sharing.SharingMode mode, bool sendNotification      
VersionId: 152
    DynamicProperties
        AppName: StartBlobUpload
        CacheControl: Nondefined
        ClassName: SenseNet.ApplicationModel.UploadAction
        MethodName: StartBlobUpload
        Parameters: long fullSize, string fieldName
VersionId: 153
    DynamicProperties
        AppName: StartBlobUploadToParent
        CacheControl: Nondefined
        ClassName: SenseNet.ApplicationModel.UploadAction
        MethodName: StartBlobUploadToParent
        Parameters: string name, string contentType, long fullSize, string fieldName
VersionId: 154
    DynamicProperties
        AppName: TakeLockOver
        RequiredPermissions: _______________________________________________________*________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: TakeLockOver
        Parameters:         string user      
VersionId: 155
    DynamicProperties
        AppName: RemoveAllowedChildTypes
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.GenericContent
        MethodName: RemoveAllowedChildTypes
        Parameters: string[] contentTypes
VersionId: 156
    DynamicProperties
        AppName: GetRelatedItems
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetRelatedItems
        Parameters:         string level,        bool explicitOnly,        string member,        string[] permissions,      
VersionId: 157
    DynamicProperties
        AppName: GetRelatedIdentitiesByPermissions
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetRelatedIdentities
        Parameters:         string level,        string kind,        string[] permissions      
VersionId: 158
    DynamicProperties
        AppName: GetRelatedIdentities
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetRelatedIdentities
        Parameters:         string level,        string kind      
VersionId: 159
    DynamicProperties
        AppName: AddAspects
        ActionTypeName: AddAspectsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 160
    DynamicProperties
        AppName: AddFields
        ActionTypeName: AddFieldsAction
        RequiredPermissions: ______________________________________________*_________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 161
    DynamicProperties
        AppName: Ancestors
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: Ancestors
VersionId: 162
    DynamicProperties
        AppName: Approve
        Scenario: ListItem;ExploreActions;SimpleApprovableListItem
        ActionTypeName: ApproveAction
        RequiredPermissions: _____________________________________________________*___***___*
        CacheControl: Nondefined
        StoredIcon: approve
VersionId: 163
    DynamicProperties
        AppName: CheckIn
        Scenario: ListItem;ExploreActions;SimpleApprovableListItem
        ActionTypeName: CheckInAction
        RequiredPermissions: _________________________________________________________***___*
        CacheControl: Nondefined
        StoredIcon: checkin
VersionId: 164
    DynamicProperties
        AppName: CheckIndexIntegrity
        CacheControl: Nondefined
        ClassName: SenseNet.Search.Indexing.IntegrityChecker
        MethodName: CheckIndexIntegrity
        Parameters:         bool recurse      
VersionId: 165
    DynamicProperties
        AppName: CheckOut
        Scenario: ListItem;ExploreActions
        ActionTypeName: CheckOutAction
        RequiredPermissions: _________________________________________________________***___*
        CacheControl: Nondefined
        StoredIcon: checkout
VersionId: 166
    DynamicProperties
        AppName: CheckSecurityConsistency
        Scenario: 
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: CheckSecurityConsistency
        Description: 
        Parameters: 
VersionId: 167
    DynamicProperties
        AppName: CopyTo
        Scenario: ListItem;ExploreActions;ManageViewsListItem
        ActionTypeName: CopyToAction
        StyleHint: 
        RequiredPermissions: _________________________________________________________*______
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: copy
        Description: 
VersionId: 168
    DynamicProperties
        AppName: Delete
        Scenario: WorkspaceActions;ListItem;ExploreActions;ListActions;ManageViewsListItem;SimpleListItem;SimpleApprovableListItem;ReadOnlyListItem;DocumentDetails
        ActionTypeName: DeleteAction
        StyleHint: 
        RequiredPermissions: ____________________________________________________*___________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: delete
        Description: 
VersionId: 169
    DynamicProperties
        AppName: DocumentPreviewFinalizer
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: DocumentPreviewFinalizer
        Parameters: SenseNet.TaskManagement.Core.SnTaskResult result
VersionId: 170
    DynamicProperties
        AppName: FinalizeBlobUpload
        CacheControl: Nondefined
        ClassName: SenseNet.ApplicationModel.UploadAction
        MethodName: FinalizeBlobUpload
        Parameters: string token, long fullSize, string fieldName, string fileName
VersionId: 171
    DynamicProperties
        AppName: FinalizeContent
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ApplicationModel.UploadAction
        MethodName: FinalizeContent
VersionId: 172
    DynamicProperties
        AppName: ForceUndoCheckOut
        Scenario: ListItem;ExploreActions
        ActionTypeName: ForceUndoCheckOutAction
        RequiredPermissions: _______________________________________________________*_***___*
        CacheControl: Nondefined
        StoredIcon: undocheckout
VersionId: 173
    DynamicProperties
        AppName: GetAllContentTypes
        Scenario: 
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: GetListOfAllContentTypes
        Description: 
        Parameters: 
VersionId: 174
    DynamicProperties
        AppName: GetAllowedChildTypesFromCTD
        Scenario: 
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: GetAllowedChildTypesFromCTD
        Description: 
        Parameters: 
VersionId: 175
    DynamicProperties
        AppName: GetAllowedUsers
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetAllowedUsers
        Description: 
        Parameters:         string[] permissions      
VersionId: 176
    DynamicProperties
        AppName: GetBinaryToken
        CacheControl: Nondefined
        ClassName: SenseNet.ApplicationModel.UploadAction
        MethodName: GetBinaryToken
        Parameters: string fieldName
VersionId: 177
    DynamicProperties
        AppName: GetChildrenPermissionInfo
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetChildrenPermissionInfo
        Parameters:         string identity      
VersionId: 178
    DynamicProperties
        AppName: GetExistingPreviewImages
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: GetExistingPreviewImagesForOData
VersionId: 179
    DynamicProperties
        AppName: GetNameFromDisplayName
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.ContentNamingProvider
        MethodName: GetNameFromDisplayName
        Parameters:         string displayName      
VersionId: 180
    DynamicProperties
        AppName: GetPermissionInfo
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetPermissionInfo
        Parameters:         string identity      
VersionId: 181
    DynamicProperties
        AppName: GetPermissionOverview
        CacheControl: Nondefined
        ClassName: SenseNet.Portal.PermissionQuery
        MethodName: GetPermissionOverview
        Parameters:         string identity      
VersionId: 182
    DynamicProperties
        AppName: GetPermissions
        ActionTypeName: GetPermissionsAction
        StyleHint: 
        IncludeBackUrl: Default
        CacheControl: Nondefined
        Description: 
VersionId: 183
    DynamicProperties
        AppName: GetPreviewImages
        RequiredPermissions: ______________________________________________________________*_
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: GetPreviewImagesForOData
VersionId: 184
    DynamicProperties
        AppName: GetQueries
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.Portal.QueryBuilder
        MethodName: GetQueries
        Parameters:         bool onlyPublic      
VersionId: 185
    DynamicProperties
        AppName: GetQueryBuilderMetadata
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.Portal.QueryBuilder
        MethodName: GetMetadata
        Parameters: 
VersionId: 186
    DynamicProperties
        AppName: GetRecentIndexingActivities
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: GetRecentIndexingActivities
        Description: 
        Parameters: 
VersionId: 187
    DynamicProperties
        AppName: GetRecentSecurityActivities
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: GetRecentSecurityActivities
        Description: 
        Parameters: 
VersionId: 188
    DynamicProperties
        AppName: TakeOwnership
        Scenario: 
        StyleHint: 
        RequiredPermissions: _____________________________________________*__________________
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        ClassName: SenseNet.ContentRepository.RepositoryTools
        MethodName: TakeOwnership
        Description: 
        Parameters: string userOrGroup
VersionId: 189
    DynamicProperties
        AppName: UndoCheckOut
        Scenario: ListItem;ExploreActions
        ActionTypeName: UndoCheckOutAction
        RequiredPermissions: _________________________________________________________***___*
        CacheControl: Nondefined
        StoredIcon: undocheckout
VersionId: 191
    DynamicProperties
        AppName: AddMembers
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Group
        MethodName: AddMembers
        Parameters: int[] contentIds
VersionId: 192
    DynamicProperties
        AppName: GetParentGroups
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetParentGroups
        Description: 
        Parameters:         bool directOnly      
VersionId: 193
    DynamicProperties
        AppName: RemoveMembers
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Group
        MethodName: RemoveMembers
        Parameters: int[] contentIds
VersionId: 195
    DynamicProperties
        AppName: Thumbnail
        Scenario: 
        ActionTypeName: 
        StyleHint: 
        RequiredPermissions: ___________________________________________________________*___*
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: application
        Width: 120
        Height: 120
        ImageType: Binary
        ImageFieldName: Binary
        SmoothingMode: antialias
        InterpolationMode: highqualitybicubic
        PixelOffsetMode: highquality
        ResizeTypeMode: crop
        CropVAlign: Center
        CropHAlign: Center
        Description: 
VersionId: 197
    DynamicProperties
        AppName: Browse
        Scenario: ListItem;ExploreToolbar
        ActionTypeName: OpenLinkAction
        StyleHint: 
        RequiredPermissions: ___________________________________________________________*___*
        IncludeBackUrl: Default
        CacheControl: Nondefined
        MaxAge: 
        CustomUrlParameters: 
        StoredIcon: link
        Description: 
VersionId: 199
    DynamicProperties
        AppName: GetSchema
        CacheControl: Nondefined
        ClassName: SenseNet.Services.Metadata.ClientMetadataProvider
        MethodName: GetSchema
        Parameters: string contentTypeName
VersionId: 200
    DynamicProperties
        AppName: GetVersionInfo
        IncludeBackUrl: Default
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Repository
        MethodName: GetVersionInfo
        Parameters: 
VersionId: 202
    DynamicProperties
        AppName: SetInitialPreviewProperties
        CacheControl: Nondefined
        ClassName: SenseNet.Preview.DocumentPreviewProvider
        MethodName: SetInitialPreviewProperties
VersionId: 204
    DynamicProperties
        AppName: Decrypt
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.Cryptography.CryptoServiceProvider
        MethodName: Decrypt
        Parameters: string text
VersionId: 205
    DynamicProperties
        AppName: Encrypt
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.Cryptography.CryptoServiceProvider
        MethodName: Encrypt
        Parameters: string text
VersionId: 207
    DynamicProperties
        AppName: GetParentGroups
        CacheControl: Nondefined
        ClassName: SenseNet.ContentRepository.Security.PermissionQueryForRest
        MethodName: GetParentGroups
        Description: 
        Parameters:         bool directOnly      
VersionId: 208
    DynamicProperties
        AppName: Profile
        Scenario: UserActions
        CacheControl: Nondefined
        StoredIcon: userprofile
        ClassName: SenseNet.Services.IdentityTools
        MethodName: BrowseProfile
        Description: 
        Parameters: string back
VersionId: 209
    BinaryProperties
        Binary: #88, F88, 0L, Admin.png, image/png, /Root/IMS/BuiltIn/Portal/Admin/Admin.png
    DynamicProperties
        PageCount: -4
        Width: 32
        Height: 32
VersionId: 210
    DynamicProperties
        SyncGuid: 
        Members: [11,1200,1198]
        Description: 
VersionId: 211
    DynamicProperties
        Description: 
VersionId: 212
    DynamicProperties
        SyncGuid: 
        Members: [7]
        Description: 
VersionId: 213
    DynamicProperties
        Description: 
VersionId: 214
    DynamicProperties
        SyncGuid: 
        Members: [7]
        Description: 
VersionId: 215
    DynamicProperties
        Description: 
VersionId: 216
    DynamicProperties
        SyncGuid: 
        Members: [1197,1202,11,1200,1198]
        Description: 
VersionId: 217
    DynamicProperties
        SyncGuid: 
        Description: 
VersionId: 218
    DynamicProperties
        Domain: BuiltIn
        FullName: VirtualADUser
        LoginName: VirtualADUser
        OldPasswords: <?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" />
VersionId: 219
    DynamicProperties
        Hidden: 1
VersionId: 220
    BinaryProperties
        Binary: #89, F89, 19797L, Content.xml, text/xml, /Root/Localization/Content.xml
    DynamicProperties
        PageCount: -4
VersionId: 221
    BinaryProperties
        Binary: #90, F90, 25460L, CtdResourcesAB.xml, text/xml, /Root/Localization/CtdResourcesAB.xml
    DynamicProperties
        PageCount: -4
VersionId: 222
    BinaryProperties
        Binary: #91, F91, 30391L, CtdResourcesCD.xml, text/xml, /Root/Localization/CtdResourcesCD.xml
    DynamicProperties
        PageCount: -4
VersionId: 223
    BinaryProperties
        Binary: #92, F92, 12811L, CtdResourcesEF.xml, text/xml, /Root/Localization/CtdResourcesEF.xml
    DynamicProperties
        PageCount: -4
VersionId: 224
    BinaryProperties
        Binary: #93, F93, 48767L, CtdResourcesGH.xml, text/xml, /Root/Localization/CtdResourcesGH.xml
    DynamicProperties
        PageCount: -4
VersionId: 225
    BinaryProperties
        Binary: #94, F94, 22739L, CtdResourcesIJK.xml, text/xml, /Root/Localization/CtdResourcesIJK.xml
    DynamicProperties
        PageCount: -4
VersionId: 226
    BinaryProperties
        Binary: #95, F95, 7361L, CtdResourcesLM.xml, text/xml, /Root/Localization/CtdResourcesLM.xml
    DynamicProperties
        PageCount: -4
VersionId: 227
    BinaryProperties
        Binary: #96, F96, 5966L, CtdResourcesNOP.xml, text/xml, /Root/Localization/CtdResourcesNOP.xml
    DynamicProperties
        PageCount: -4
VersionId: 228
    BinaryProperties
        Binary: #97, F97, 2624L, CtdResourcesQ.xml, text/xml, /Root/Localization/CtdResourcesQ.xml
    DynamicProperties
        PageCount: -4
VersionId: 229
    BinaryProperties
        Binary: #98, F98, 20272L, CtdResourcesRS.xml, text/xml, /Root/Localization/CtdResourcesRS.xml
    DynamicProperties
        PageCount: -4
VersionId: 230
    BinaryProperties
        Binary: #99, F99, 39392L, CtdResourcesTZ.xml, text/xml, /Root/Localization/CtdResourcesTZ.xml
    DynamicProperties
        PageCount: -4
VersionId: 231
    BinaryProperties
        Binary: #100, F100, 19617L, Exceptions.xml, text/xml, /Root/Localization/Exceptions.xml
    DynamicProperties
        PageCount: -4
VersionId: 232
    BinaryProperties
        Binary: #101, F101, 0L, Sharing.xml, text/xml, /Root/Localization/Sharing.xml
    DynamicProperties
        PageCount: -4
VersionId: 233
    BinaryProperties
        Binary: #102, F102, 6728L, Trash.xml, text/xml, /Root/Localization/Trash.xml
    DynamicProperties
        PageCount: -4
VersionId: 236
    BinaryProperties
        Binary: #103, F103, 0L, Global.html, text/html, /Root/System/ErrorMessages/Default/Global.html
    DynamicProperties
        PageCount: -4
VersionId: 237
    BinaryProperties
        Binary: #104, F104, 0L, UserGlobal.html, text/html, /Root/System/ErrorMessages/Default/UserGlobal.html
    DynamicProperties
        PageCount: -4
VersionId: 238
    DynamicProperties
        AllowedChildTypes: GetMetadataApplication SystemFolder Folder
VersionId: 239
    DynamicProperties
        AllowedChildTypes: GetMetadataApplication SystemFolder Folder
VersionId: 240
    DynamicProperties
        AppName: complextypes
        CacheControl: Nondefined
VersionId: 241
    DynamicProperties
        AppName: contenttypes
        CacheControl: Nondefined
VersionId: 242
    DynamicProperties
        AppName: enums
        CacheControl: Nondefined
VersionId: 243
    DynamicProperties
        AppName: fieldsettings
        CacheControl: Nondefined
VersionId: 244
    DynamicProperties
        AppName: meta
        CacheControl: Nondefined
VersionId: 245
    DynamicProperties
        AppName: resources
        CacheControl: Nondefined
VersionId: 246
    DynamicProperties
        AppName: schemas
        CacheControl: Nondefined
VersionId: 248
    BinaryProperties
        Binary: #105, F105, 0L, binaryhandler.ashx, application/octet-stream, /Root/System/WebRoot/binaryhandler.ashx
    DynamicProperties
        PageCount: -4
VersionId: 250
    BinaryProperties
        Binary: #106, F106, 0L, Dws.asmx, application/octet-stream, /Root/System/WebRoot/DWS/Dws.asmx
    DynamicProperties
        PageCount: -4
VersionId: 251
    BinaryProperties
        Binary: #107, F107, 0L, Fpp.ashx, application/octet-stream, /Root/System/WebRoot/DWS/Fpp.ashx
    DynamicProperties
        PageCount: -4
VersionId: 252
    BinaryProperties
        Binary: #108, F108, 0L, Lists.asmx, application/octet-stream, /Root/System/WebRoot/DWS/Lists.asmx
    DynamicProperties
        PageCount: -4
VersionId: 253
    BinaryProperties
        Binary: #109, F109, 0L, owssvr.aspx, text/asp, /Root/System/WebRoot/DWS/owssvr.aspx
    DynamicProperties
        PageCount: -4
VersionId: 254
    BinaryProperties
        Binary: #110, F110, 0L, Versions.asmx, application/octet-stream, /Root/System/WebRoot/DWS/Versions.asmx
    DynamicProperties
        PageCount: -4
VersionId: 255
    BinaryProperties
        Binary: #111, F111, 0L, Webs.asmx, application/octet-stream, /Root/System/WebRoot/DWS/Webs.asmx
    DynamicProperties
        PageCount: -4
VersionId: 256
    BinaryProperties
        Binary: #112, F112, 0L, vsshandler.ashx, application/octet-stream, /Root/System/WebRoot/vsshandler.ashx
    DynamicProperties
        PageCount: -4
        Description: Http handler for serving Lucene index file paths. This content can be invoked only from the local machine.
VersionId: 257
    DynamicProperties
        TrashDisabled: 1
        IsActive: 1
        BagCapacity: 100
VersionId: 258
    DynamicProperties
        Hidden: 1
VersionId: 260
    DynamicProperties
        AppName: Restore
        Scenario: ListItem;ExploreToolbar
        ActionTypeName: RestoreAction
        RequiredPermissions: _________________________________________________________*______
        CacheControl: Nondefined
        StoredIcon: restore
";
        #endregion
    }
}
