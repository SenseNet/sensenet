using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal partial class InMemoryDataProvider
    {
        // Readme: Getting data
        //
        // 1 - Create a new web application
        // 2 - Extend with the SenseNet.Services.Install package (see the package's readme.txt)
        // 3 - Getting table data from the database
        //     3.1 - Run the MS SQL Management Studio
        //     3.2 - Connect to the installed web application's database
        //     3.3 - Execute scripts and copy the data with headers into the following sections (use verbatim strings (@""
        //           and replace single quotation marks to double)):
        //         3.3.1 - NODES:
        //                 SELECT NodeId, COALESCE(ParentNodeId, 0) ParentNodeId, NodeTypeId, LastMajorVersionId, LastMinorVersionId, [Index], IsSystem, Name, COALESCE(DisplayName, '""""'), [Path] FROM Nodes
        //         3.3.2 - BINARYPROPERTIES:
        //                 SELECT * FROM BinaryProperties
        //         3.3.3 - FILES:
        //                 SELECT FileId, ContentType, FileNameWithoutExtension, Extension, Size FROM Files
        //         3.3.4 - TEXTPROPERTIES
        //                 SELECT TextPropertyNVarcharId, VersionId, PropertyTypeId, Value FROM TextPropertiesNVarchar
        //                 (After install the TextPropertiesNText table is empty)
        //                 This result may contains multiline string values in the Value column. Line feeds need to be replaced
        //                 to "\n" and tabs to "\t" sequences to ensure correct data for parsing table data.
        // 4 - Getting schema
        //     4.1 - Write this code at the end of the Application_Start method of a web application's Global.asax.cs
        //           that is extended with SenseNet.Services.Install nuget package:
        //
        //           var editor = new SchemaEditor();
        //           editor.Load();
        //           using (var writer = new StreamWriter(@"C:\schema.xml", false))
        //               writer.WriteLine(editor.ToXml());
        //
        //     4.2 - Run application and wait for the start screen
        //     4.3 - Copy the schema file content (C:\schema.xml in this example) into the SCHEMA section's
        //           TestSchema variable's string literal (replace quotation marks to apostrophes).
        // ---------------------------------------------------------------------------------------------------------------
        //
        // IMPORTANT:
        //
        // - Initial data contains only the CTD's binaries that are loaded form disk (see the static constructor of this object).


        #region NODES

        // SELECT NodeId, COALESCE(ParentNodeId, 0) ParentNodeId, NodeTypeId, LastMajorVersionId, LastMinorVersionId, [Index], IsSystem, Name, COALESCE(DisplayName, '""""'), [Path] FROM Nodes

        private static string _initialNodes = @"NodeId	ParentNodeId	NodeTypeId	LastMajorVersionId	LastMinorVersionId	Index	IsSystem	Name	(No column name)	Path
1	5	3	1	1	0	0	Admin	""""	/Root/IMS/BuiltIn/Portal/Admin
2	0	4	2	2	1	0	Root	""""	/Root
3	2	6	3	3	3	0	IMS	Users and Groups	/Root/IMS
4	3	7	4	4	0	0	BuiltIn	""""	/Root/IMS/BuiltIn
5	4	8	5	5	0	0	Portal	""""	/Root/IMS/BuiltIn/Portal
6	5	3	6	6	4	0	Visitor	""""	/Root/IMS/BuiltIn/Portal/Visitor
7	5	2	7	7	2	0	Administrators	""""	/Root/IMS/BuiltIn/Portal/Administrators
8	5	2	8	8	3	0	Everyone	""""	/Root/IMS/BuiltIn/Portal/Everyone
9	5	2	9	9	5	0	Owners	""""	/Root/IMS/BuiltIn/Portal/Owners
10	5	3	10	10	7	0	Somebody	""""	/Root/IMS/BuiltIn/Portal/Somebody
11	5	2	11	11	7	0	Operators	""""	/Root/IMS/BuiltIn/Portal/Operators
1000	2	5	12	12	3	1	System	""""	/Root/System
1001	1000	5	13	13	1	1	Schema	Schema	/Root/System/Schema
1002	1001	5	14	14	1	1	ContentTypes	ContentTypes	/Root/System/Schema/ContentTypes
1003	1000	5	15	15	2	1	Settings	Settings	/Root/System/Settings
1004	1002	9	16	16	0	1	ContentType	$Ctd-ContentType,DisplayName	/Root/System/Schema/ContentTypes/ContentType
1005	1002	9	17	17	0	1	GenericContent	$Ctd-GenericContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent
1006	1005	9	18	18	0	1	Application	$Ctd-Application,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application
1007	1006	9	19	19	0	1	ApplicationOverride	$Ctd-ApplicationOverride,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ApplicationOverride
1008	1005	9	20	20	0	1	Folder	$Ctd-Folder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder
1009	1008	9	21	21	0	1	ContentList	$Ctd-ContentList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList
1010	1009	9	22	22	0	1	Aspect	$Ctd-Aspect,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Aspect
1011	1005	9	23	23	0	1	FieldSettingContent	$Ctd-FieldSettingContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent
1012	1011	9	24	24	0	1	BinaryFieldSetting	$Ctd-BinaryFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/BinaryFieldSetting
1013	1011	9	25	25	0	1	TextFieldSetting	$Ctd-TextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting
1014	1013	9	26	26	0	1	ShortTextFieldSetting	$Ctd-ShortTextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting
1015	1014	9	27	27	0	1	ChoiceFieldSetting	$Ctd-ChoiceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting
1016	1005	9	28	28	0	1	ContentLink	$Ctd-ContentLink,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ContentLink
1017	1011	9	29	29	0	1	NumberFieldSetting	$Ctd-NumberFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting
1018	1017	9	30	30	0	1	CurrencyFieldSetting	$Ctd-CurrencyFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting/CurrencyFieldSetting
1019	1009	9	31	31	0	1	ItemList	$Ctd-ItemList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList
1020	1019	9	32	32	0	1	CustomList	$Ctd-CustomList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/CustomList
1021	1005	9	33	33	0	1	ListItem	$Ctd-ListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem
1022	1021	9	34	34	0	1	CustomListItem	$Ctd-CustomListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/CustomListItem
1023	1011	9	35	35	0	1	DateTimeFieldSetting	$Ctd-DateTimeFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/DateTimeFieldSetting
1024	1008	9	36	36	0	1	Device	$Ctd-Device,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Device
1025	1009	9	37	37	0	1	Library	$Ctd-Library,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library
1026	1025	9	38	38	0	1	DocumentLibrary	$Ctd-DocumentLibrary,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary
1027	1008	9	39	39	0	1	Domain	$Ctd-Domain,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Domain
1028	1008	9	40	40	0	1	Domains	$Ctd-Domains,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Domains
1029	1005	9	41	41	0	1	File	$Ctd-File,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File
1030	1029	9	42	42	0	1	DynamicJsonContent	Dynamic JSON content	/Root/System/Schema/ContentTypes/GenericContent/File/DynamicJsonContent
1031	1008	9	43	43	0	1	Email	$Ctd-Email,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Email
1032	1029	9	44	44	0	1	ExecutableFile	$Ctd-ExecutableFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/ExecutableFile
1033	1006	9	45	45	0	1	ExportToCsvApplication	$Ctd-ExportToCsvApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ExportToCsvApplication
1034	1006	9	46	46	0	1	GenericODataApplication	$Ctd-GenericODataApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/GenericODataApplication
1035	1006	9	47	47	0	1	HttpHandlerApplication	$Ctd-HttpHandlerApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication
1036	1035	9	48	48	0	1	GetMetadataApplication	GetMetadataApplication	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication/GetMetadataApplication
1037	1005	9	49	49	0	1	Group	$Ctd-Group,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Group
1038	1029	9	50	50	0	1	HtmlTemplate	$Ctd-HtmlTemplate,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/HtmlTemplate
1039	1006	9	51	51	0	1	HttpStatusApplication	$Ctd-HttpStatusApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpStatusApplication
1040	1011	9	52	52	0	1	HyperLinkFieldSetting	$Ctd-HyperLinkFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/HyperLinkFieldSetting
1041	1029	9	53	53	0	1	Image	$Ctd-Image,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Image
1042	1025	9	54	54	0	1	ImageLibrary	$Ctd-ImageLibrary,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/ImageLibrary
1043	1006	9	55	55	0	1	ImgResizeApplication	$Ctd-ImgResizeApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ImgResizeApplication
1044	1029	9	56	56	0	1	Settings	$Ctd-Settings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings
1045	1044	9	57	57	0	1	IndexingSettings	$Ctd-IndexingSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/IndexingSettings
1046	1011	9	58	58	0	1	IntegerFieldSetting	$Ctd-IntegerFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/IntegerFieldSetting
1047	1044	9	59	59	0	1	LoggingSettings	$Ctd-LoggingSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/LoggingSettings
1048	1013	9	60	60	0	1	LongTextFieldSetting	$Ctd-LongTextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/LongTextFieldSetting
1049	1021	9	61	61	0	1	Memo	$Ctd-Memo,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Memo
1050	1019	9	62	62	0	1	MemoList	$Ctd-MemoList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList
1051	1011	9	63	63	0	1	NullFieldSetting	$Ctd-NullFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NullFieldSetting
1052	1008	9	64	64	0	1	OrganizationalUnit	$Ctd-OrganizationalUnit,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/OrganizationalUnit
1053	1014	9	65	65	0	1	PasswordFieldSetting	$Ctd-PasswordFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/PasswordFieldSetting
1054	1015	9	66	66	0	1	PermissionChoiceFieldSetting	$Ctd-PermissionChoiceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/PermissionChoiceFieldSetting
1055	1008	9	67	67	0	1	PortalRoot	$Ctd-PortalRoot,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/PortalRoot
1056	1044	9	68	68	0	1	PortalSettings	$Ctd-PortalSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/PortalSettings
1057	1041	9	69	69	0	1	PreviewImage	$Ctd-PreviewImage,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Image/PreviewImage
1058	1008	9	70	70	0	1	ProfileDomain	$Ctd-ProfileDomain,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ProfileDomain
1059	1008	9	71	71	0	1	Profiles	$Ctd-Profiles,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Profiles
1060	1005	9	72	72	0	1	Query	$Ctd-Query,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Query
1061	1011	9	73	73	0	1	ReferenceFieldSetting	$Ctd-ReferenceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/ReferenceFieldSetting
1062	1029	9	74	74	0	1	SystemFile	$Ctd-SystemFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile
1063	1062	9	75	75	0	1	Resource	$Ctd-Resource,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/Resource
1064	1008	9	76	76	0	1	SystemFolder	$Ctd-SystemFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder
1065	1064	9	77	77	0	1	Resources	$Ctd-Resources,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Resources
1066	1006	9	78	78	0	1	RssApplication	$Ctd-RssApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/RssApplication
1067	1008	9	79	79	0	1	RuntimeContentContainer	$Ctd-RuntimeContentContainer,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/RuntimeContentContainer
1068	1008	9	80	80	0	1	Workspace	$Ctd-Workspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace
1069	1068	9	81	81	0	1	Site	$Ctd-Site,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Site
1070	1008	9	82	82	0	1	Sites	$Ctd-Sites,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Sites
1071	1008	9	83	83	0	1	SmartFolder	$Ctd-SmartFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder
1072	1021	9	84	84	0	1	Task	$Ctd-Task,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Task
1073	1019	9	85	85	0	1	TaskList	$Ctd-TaskList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList
1074	1008	9	86	86	0	1	TrashBag	$Ctd-TrashBag,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/TrashBag
1075	1068	9	87	87	0	1	TrashBin	$Ctd-TrashBin,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TrashBin
1076	1005	9	88	88	0	1	User	$Ctd-User,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/User
1077	1068	9	89	89	0	1	UserProfile	$Ctd-UserProfile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/UserProfile
1078	1006	9	90	90	0	1	WebServiceApplication	$Ctd-WebServiceApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/WebServiceApplication
1079	1011	9	91	91	0	1	XmlFieldSetting	$Ctd-XmlFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/XmlFieldSetting
1080	1015	9	92	92	0	1	YesNoFieldSetting	$Ctd-YesNoFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/YesNoFieldSetting
1081	1003	65	93	93	0	1	Indexing.settings	""""	/Root/System/Settings/Indexing.settings
1082	1003	66	94	94	0	1	Logging.settings	""""	/Root/System/Settings/Logging.settings
1083	1003	51	95	95	0	1	MailProcessor.settings	""""	/Root/System/Settings/MailProcessor.settings
1084	1003	67	96	96	0	1	Portal.settings	""""	/Root/System/Settings/Portal.settings
1085	1003	51	97	97	0	1	TaskManagement.settings	""""	/Root/System/Settings/TaskManagement.settings
1086	1003	51	98	98	0	1	UserProfile.settings	""""	/Root/System/Settings/UserProfile.settings
1087	2	5	99	99	0	1	(apps)	(apps)	/Root/(apps)
1088	1087	1	100	100	0	1	ContentList	""""	/Root/(apps)/ContentList
1089	1088	11	101	101	0	1	DeleteField	$Action,DeleteField	/Root/(apps)/ContentList/DeleteField
1090	1088	11	102	102	0	1	EditField	$Action,EditField	/Root/(apps)/ContentList/EditField
1091	1088	24	103	103	0	1	ExchangeService.asmx	""""	/Root/(apps)/ContentList/ExchangeService.asmx
1092	1087	1	104	104	0	1	File	""""	/Root/(apps)/File
1093	1092	19	105	105	0	1	CheckPreviews	Check preview images	/Root/(apps)/File/CheckPreviews
1094	1092	11	106	106	0	1	EditInMicrosoftOffice	$Action,Edit-in-Microsoft-Office	/Root/(apps)/File/EditInMicrosoftOffice
1095	1092	20	107	107	0	1	ExportToPdf	$Action,ExportToPdf	/Root/(apps)/File/ExportToPdf
1096	1092	19	108	108	0	1	GetPageCount	Get page count	/Root/(apps)/File/GetPageCount
1097	1092	19	109	109	0	1	GetPreviewsFolder	Get previews folder	/Root/(apps)/File/GetPreviewsFolder
1098	1092	19	110	110	0	1	PreviewAvailable	""""	/Root/(apps)/File/PreviewAvailable
1099	1092	19	111	111	0	1	RegeneratePreviews	Regenerate preview images	/Root/(apps)/File/RegeneratePreviews
1100	1092	19	112	112	0	1	SetPageCount	Set page count	/Root/(apps)/File/SetPageCount
1101	1092	19	113	113	0	1	SetPreviewStatus	Set preview status	/Root/(apps)/File/SetPreviewStatus
1102	1092	11	114	114	250	1	UploadResume	$Action,UploadResume	/Root/(apps)/File/UploadResume
1103	1087	1	115	115	0	1	Folder	""""	/Root/(apps)/Folder
1104	1103	11	116	116	3800	1	CopyBatch	$Action,CopyBatch	/Root/(apps)/Folder/CopyBatch
1105	1103	11	117	117	3800	1	DeleteBatch	$Action,DeleteBatch	/Root/(apps)/Folder/DeleteBatch
1106	1103	18	118	118	5400	1	ExportToCsv	$Action,ExportToCsv	/Root/(apps)/Folder/ExportToCsv
1107	1103	11	119	119	3800	1	MoveBatch	$Action,MoveBatch	/Root/(apps)/Folder/MoveBatch
1108	1103	11	120	120	0	1	Upload	$Action,Upload	/Root/(apps)/Folder/Upload
1109	1087	1	121	121	0	1	GenericContent	""""	/Root/(apps)/GenericContent
1110	1109	19	122	122	0	1	AddAllowedChildTypes	""""	/Root/(apps)/GenericContent/AddAllowedChildTypes
1111	1109	19	123	123	0	1	GetRelatedItemsOneLevel	""""	/Root/(apps)/GenericContent/GetRelatedItemsOneLevel
1112	1109	19	124	124	0	1	GetRelatedPermissions	""""	/Root/(apps)/GenericContent/GetRelatedPermissions
1113	1109	19	125	125	0	1	GetTemplateScript	""""	/Root/(apps)/GenericContent/GetTemplateScript
1114	1109	20	126	126	0	1	HasPermission	$Action,HasPermission	/Root/(apps)/GenericContent/HasPermission
1115	1109	19	127	127	0	1	Login	""""	/Root/(apps)/GenericContent/Login
1116	1109	11	128	128	9000	1	Logout	$Action,Logout	/Root/(apps)/GenericContent/Logout
1117	1109	11	129	129	3800	1	MoveTo	$Action,MoveTo	/Root/(apps)/GenericContent/MoveTo
1118	1109	11	130	130	0	1	Publish	$Action,Publish	/Root/(apps)/GenericContent/Publish
1119	1109	19	131	131	0	1	RebuildIndex	""""	/Root/(apps)/GenericContent/RebuildIndex
1120	1109	19	132	132	0	1	RebuildIndexSubtree	""""	/Root/(apps)/GenericContent/RebuildIndexSubtree
1121	1109	19	133	133	0	1	RefreshIndexSubtree	""""	/Root/(apps)/GenericContent/RefreshIndexSubtree
1122	1109	11	134	134	0	1	Reject	$Action,Reject	/Root/(apps)/GenericContent/Reject
1123	1109	19	135	135	0	1	GetRelatedItems	""""	/Root/(apps)/GenericContent/GetRelatedItems
1124	1109	20	136	136	0	1	RemoveAllAspects	$Action,RemoveAllAspects	/Root/(apps)/GenericContent/RemoveAllAspects
1125	1109	19	137	137	0	1	RemoveAllowedChildTypes	""""	/Root/(apps)/GenericContent/RemoveAllowedChildTypes
1126	1109	20	138	138	0	1	RemoveAspects	$Action,RemoveAspects	/Root/(apps)/GenericContent/RemoveAspects
1127	1109	20	139	139	0	1	RemoveFields	$Action,RemoveFields	/Root/(apps)/GenericContent/RemoveFields
1128	1109	19	140	140	0	1	ResetRecentIndexingActivities	""""	/Root/(apps)/GenericContent/ResetRecentIndexingActivities
1129	1109	11	141	141	0	1	RestoreVersion	$Action,RestoreVersion	/Root/(apps)/GenericContent/RestoreVersion
1130	1109	20	142	142	0	1	RetrieveFields	$Action,RetrieveFields	/Root/(apps)/GenericContent/RetrieveFields
1131	1109	23	143	143	0	1	Rss	$Action,Rss	/Root/(apps)/GenericContent/Rss
1132	1109	19	144	144	0	1	SaveQuery	""""	/Root/(apps)/GenericContent/SaveQuery
1133	1109	11	145	145	0	1	SetPermissions	$Action,SetPermissions	/Root/(apps)/GenericContent/SetPermissions
1134	1109	19	146	146	0	1	StartBlobUpload	""""	/Root/(apps)/GenericContent/StartBlobUpload
1135	1109	19	147	147	0	1	StartBlobUploadToParent	""""	/Root/(apps)/GenericContent/StartBlobUploadToParent
1136	1109	19	148	148	0	1	TakeLockOver	""""	/Root/(apps)/GenericContent/TakeLockOver
1137	1109	20	149	149	0	1	RemoveAllFields	$Action,RemoveAllFields	/Root/(apps)/GenericContent/RemoveAllFields
1138	1109	19	150	150	0	1	GetRelatedIdentitiesByPermissions	""""	/Root/(apps)/GenericContent/GetRelatedIdentitiesByPermissions
1139	1109	19	151	151	0	1	GetRelatedIdentities	""""	/Root/(apps)/GenericContent/GetRelatedIdentities
1140	1109	19	152	152	0	1	GetRecentSecurityActivities	""""	/Root/(apps)/GenericContent/GetRecentSecurityActivities
1141	1109	20	153	153	0	1	AddAspects	$Action,AddAspects	/Root/(apps)/GenericContent/AddAspects
1142	1109	20	154	154	0	1	AddFields	$Action,AddFields	/Root/(apps)/GenericContent/AddFields
1143	1109	11	155	155	0	1	Approve	$Action,Approve	/Root/(apps)/GenericContent/Approve
1144	1109	11	156	156	0	1	CheckIn	$Action,CheckIn	/Root/(apps)/GenericContent/CheckIn
1145	1109	19	157	157	0	1	CheckIndexIntegrity	""""	/Root/(apps)/GenericContent/CheckIndexIntegrity
1146	1109	11	158	158	0	1	CheckOut	$Action,CheckOut	/Root/(apps)/GenericContent/CheckOut
1147	1109	19	159	159	0	1	CheckSecurityConsistency	""""	/Root/(apps)/GenericContent/CheckSecurityConsistency
1148	1109	11	160	160	3800	1	CopyTo	$Action,CopyTo	/Root/(apps)/GenericContent/CopyTo
1149	1109	11	161	161	6000	1	Delete	$Action,Delete	/Root/(apps)/GenericContent/Delete
1150	1109	19	162	162	0	1	DocumentPreviewFinalizer	""""	/Root/(apps)/GenericContent/DocumentPreviewFinalizer
1151	1109	19	163	163	0	1	FinalizeBlobUpload	""""	/Root/(apps)/GenericContent/FinalizeBlobUpload
1152	1109	19	164	164	0	1	FinalizeContent	""""	/Root/(apps)/GenericContent/FinalizeContent
1153	1109	11	165	165	0	1	ForceUndoCheckOut	$Action,ForceUndoCheckOut	/Root/(apps)/GenericContent/ForceUndoCheckOut
1154	1109	19	166	166	0	1	GetAllContentTypes	""""	/Root/(apps)/GenericContent/GetAllContentTypes
1155	1109	19	167	167	0	1	GetAllowedChildTypesFromCTD	""""	/Root/(apps)/GenericContent/GetAllowedChildTypesFromCTD
1156	1109	19	168	168	0	1	GetAllowedUsers	""""	/Root/(apps)/GenericContent/GetAllowedUsers
1157	1109	19	169	169	0	1	GetBinaryToken	""""	/Root/(apps)/GenericContent/GetBinaryToken
1158	1109	19	170	170	0	1	GetChildrenPermissionInfo	""""	/Root/(apps)/GenericContent/GetChildrenPermissionInfo
1159	1109	19	171	171	0	1	GetExistingPreviewImages	$Action,GetExistingPreviewImages	/Root/(apps)/GenericContent/GetExistingPreviewImages
1160	1109	19	172	172	0	1	GetNameFromDisplayName	""""	/Root/(apps)/GenericContent/GetNameFromDisplayName
1161	1109	19	173	173	0	1	GetPermissionInfo	""""	/Root/(apps)/GenericContent/GetPermissionInfo
1162	1109	19	174	174	0	1	GetPermissionOverview	""""	/Root/(apps)/GenericContent/GetPermissionOverview
1163	1109	20	175	175	0	1	GetPermissions	$Action,GetPermissions	/Root/(apps)/GenericContent/GetPermissions
1164	1109	19	176	176	0	1	GetPreviewImages	$Action,GetPreviewImages	/Root/(apps)/GenericContent/GetPreviewImages
1165	1109	19	177	177	0	1	GetQueries	""""	/Root/(apps)/GenericContent/GetQueries
1166	1109	19	178	178	0	1	GetQueryBuilderMetadata	""""	/Root/(apps)/GenericContent/GetQueryBuilderMetadata
1167	1109	19	179	179	0	1	GetRecentIndexingActivities	""""	/Root/(apps)/GenericContent/GetRecentIndexingActivities
1168	1109	19	180	180	0	1	TakeOwnership	""""	/Root/(apps)/GenericContent/TakeOwnership
1169	1109	11	181	181	0	1	UndoCheckOut	$Action,UndoCheckOut	/Root/(apps)/GenericContent/UndoCheckOut
1170	1087	1	182	182	0	1	Group	""""	/Root/(apps)/Group
1171	1170	19	183	183	0	1	AddMembers	Add members	/Root/(apps)/Group/AddMembers
1172	1170	19	184	184	0	1	GetParentGroups	""""	/Root/(apps)/Group/GetParentGroups
1173	1170	19	185	185	0	1	RemoveMembers	Remove members	/Root/(apps)/Group/RemoveMembers
1174	1087	1	186	186	0	1	Image	""""	/Root/(apps)/Image
1175	1174	22	187	187	0	1	Thumbnail	""""	/Root/(apps)/Image/Thumbnail
1176	1087	1	188	188	0	1	Link	""""	/Root/(apps)/Link
1177	1176	11	189	189	0	1	Browse	$Action,OpenLink	/Root/(apps)/Link/Browse
1178	1087	1	190	190	0	1	PortalRoot	""""	/Root/(apps)/PortalRoot
1179	1178	19	191	191	0	1	GetVersionInfo	""""	/Root/(apps)/PortalRoot/GetVersionInfo
1180	1087	1	192	192	0	1	PreviewImage	""""	/Root/(apps)/PreviewImage
1181	1180	19	193	193	0	1	SetInitialPreviewProperties	Set initial preview properties	/Root/(apps)/PreviewImage/SetInitialPreviewProperties
1182	1087	1	194	194	0	1	This	""""	/Root/(apps)/This
1183	1182	19	195	195	0	1	Decrypt	""""	/Root/(apps)/This/Decrypt
1184	1182	19	196	196	0	1	Encrypt	""""	/Root/(apps)/This/Encrypt
1185	1087	1	197	197	0	1	User	""""	/Root/(apps)/User
1186	1185	19	198	198	0	1	GetParentGroups	""""	/Root/(apps)/User/GetParentGroups
1187	1185	19	199	199	0	1	Profile	""""	/Root/(apps)/User/Profile
1188	1	50	200	200	0	0	Admin.png	Admin.png	/Root/IMS/BuiltIn/Portal/Admin/Admin.png
1189	5	2	201	201	0	0	ContentExplorers	ContentExplorers	/Root/IMS/BuiltIn/Portal/ContentExplorers
1190	5	2	202	202	0	0	Developers	Developers	/Root/IMS/BuiltIn/Portal/Developers
1191	5	2	203	203	0	0	Editors	Editors	/Root/IMS/BuiltIn/Portal/Editors
1192	5	2	204	204	0	0	HR	HR	/Root/IMS/BuiltIn/Portal/HR
1193	5	2	205	205	0	0	IdentifiedUsers	IdentifiedUsers	/Root/IMS/BuiltIn/Portal/IdentifiedUsers
1194	5	2	206	206	0	0	PageEditors	PageEditors	/Root/IMS/BuiltIn/Portal/PageEditors
1195	5	2	207	207	0	0	PRCViewers	PRCViewers	/Root/IMS/BuiltIn/Portal/PRCViewers
1196	5	2	208	208	0	0	RegisteredUsers	RegisteredUsers	/Root/IMS/BuiltIn/Portal/RegisteredUsers
1197	5	3	209	209	0	0	VirtualADUser	""""	/Root/IMS/BuiltIn/Portal/VirtualADUser
1198	2	54	210	210	0	1	Localization	""""	/Root/Localization
1199	1198	68	211	211	0	1	Content.xml	""""	/Root/Localization/Content.xml
1200	1198	68	212	212	0	1	CtdResourcesAB.xml	CtdResourcesAB.xml	/Root/Localization/CtdResourcesAB.xml
1201	1198	68	213	213	0	1	CtdResourcesCD.xml	CtdResourcesCD.xml	/Root/Localization/CtdResourcesCD.xml
1202	1198	68	214	214	0	1	CtdResourcesEF.xml	CtdResourcesEF.xml	/Root/Localization/CtdResourcesEF.xml
1203	1198	68	215	215	0	1	CtdResourcesGH.xml	CtdResourcesGH.xml	/Root/Localization/CtdResourcesGH.xml
1204	1198	68	216	216	0	1	CtdResourcesIJK.xml	CtdResourcesIJK.xml	/Root/Localization/CtdResourcesIJK.xml
1205	1198	68	217	217	0	1	CtdResourcesLM.xml	CtdResourcesLM.xml	/Root/Localization/CtdResourcesLM.xml
1206	1198	68	218	218	0	1	CtdResourcesNOP.xml	CtdResourcesNOP.xml	/Root/Localization/CtdResourcesNOP.xml
1207	1198	68	219	219	0	1	CtdResourcesQ.xml	CtdResourcesQ.xml	/Root/Localization/CtdResourcesQ.xml
1208	1198	68	220	220	0	1	CtdResourcesRS.xml	CtdResourcesRS.xml	/Root/Localization/CtdResourcesRS.xml
1209	1198	68	221	221	0	1	CtdResourcesTZ.xml	CtdResourcesTZ.xml	/Root/Localization/CtdResourcesTZ.xml
1210	1198	68	222	222	0	1	Exceptions.xml	""""	/Root/Localization/Exceptions.xml
1211	1198	68	223	223	0	1	Trash.xml	""""	/Root/Localization/Trash.xml
1212	1000	5	224	224	0	1	ErrorMessages	""""	/Root/System/ErrorMessages
1213	1212	5	225	225	0	1	Default	""""	/Root/System/ErrorMessages/Default
1214	1213	15	226	226	0	1	Global.html	""""	/Root/System/ErrorMessages/Default/Global.html
1215	1213	15	227	227	0	1	UserGlobal.html	""""	/Root/System/ErrorMessages/Default/UserGlobal.html
1216	1001	5	228	228	0	1	Metadata	Metadata	/Root/System/Schema/Metadata
1217	1216	5	229	229	0	1	TypeScript	TypeScript	/Root/System/Schema/Metadata/TypeScript
1218	1217	53	230	230	0	1	complextypes.ts	""""	/Root/System/Schema/Metadata/TypeScript/complextypes.ts
1219	1217	53	231	231	0	1	contenttypes.ts	""""	/Root/System/Schema/Metadata/TypeScript/contenttypes.ts
1220	1217	53	232	232	0	1	enums.ts	""""	/Root/System/Schema/Metadata/TypeScript/enums.ts
1221	1217	53	233	233	0	1	fieldsettings.ts	""""	/Root/System/Schema/Metadata/TypeScript/fieldsettings.ts
1222	1217	53	234	234	0	1	meta.zip	""""	/Root/System/Schema/Metadata/TypeScript/meta.zip
1223	1217	53	235	235	0	1	resources.ts	""""	/Root/System/Schema/Metadata/TypeScript/resources.ts
1224	1217	53	236	236	0	1	schemas.ts	""""	/Root/System/Schema/Metadata/TypeScript/schemas.ts
1225	1000	5	237	237	0	1	WebRoot	""""	/Root/System/WebRoot
1226	1225	48	238	238	0	1	binaryhandler.ashx	binaryhandler.ashx	/Root/System/WebRoot/binaryhandler.ashx
1227	1225	1	239	239	0	1	DWS	DWS	/Root/System/WebRoot/DWS
1228	1227	48	240	240	0	1	Dws.asmx	""""	/Root/System/WebRoot/DWS/Dws.asmx
1229	1227	48	241	241	0	1	Fpp.ashx	""""	/Root/System/WebRoot/DWS/Fpp.ashx
1230	1227	48	242	242	0	1	Lists.asmx	""""	/Root/System/WebRoot/DWS/Lists.asmx
1231	1227	48	243	243	0	1	owssvr.aspx	""""	/Root/System/WebRoot/DWS/owssvr.aspx
1232	1227	48	244	244	0	1	Versions.asmx	""""	/Root/System/WebRoot/DWS/Versions.asmx
1233	1227	48	245	245	0	1	Webs.asmx	""""	/Root/System/WebRoot/DWS/Webs.asmx
1234	1225	48	246	246	0	1	vsshandler.ashx	vsshandler.ashx	/Root/System/WebRoot/vsshandler.ashx
1235	2	59	247	247	0	0	Trash	""""	/Root/Trash
1236	1235	5	248	248	0	1	(apps)	""""	/Root/Trash/(apps)
1237	1236	1	249	249	0	1	TrashBag	""""	/Root/Trash/(apps)/TrashBag
1238	1237	11	250	250	0	1	Restore	$Action,Restore	/Root/Trash/(apps)/TrashBag/Restore
";
        #endregion

        #region BINARYPROPERTIES

        // SELECT * FROM BinaryProperties

        private static string _initialBinaryProperties = @"BinaryPropertyId	VersionId	PropertyTypeId	FileId
1	16	1	1
2	17	1	2
3	18	1	3
4	19	1	4
5	20	1	5
6	21	1	6
7	22	1	7
8	23	1	8
9	24	1	9
10	25	1	10
11	26	1	11
12	27	1	12
13	28	1	13
14	29	1	14
15	30	1	15
16	31	1	16
17	32	1	17
18	33	1	18
19	34	1	19
20	35	1	20
21	36	1	21
22	37	1	22
23	38	1	23
24	39	1	24
25	40	1	25
26	41	1	26
27	42	1	27
28	43	1	28
29	44	1	29
30	45	1	30
31	46	1	31
32	47	1	32
33	48	1	33
34	49	1	34
35	50	1	35
36	51	1	36
37	52	1	37
38	53	1	38
39	54	1	39
40	55	1	40
41	56	1	41
42	57	1	42
43	58	1	43
44	59	1	44
45	60	1	45
46	61	1	46
47	62	1	47
48	63	1	48
49	64	1	49
50	65	1	50
51	66	1	51
52	67	1	52
53	68	1	53
54	69	1	54
55	70	1	55
56	71	1	56
57	72	1	57
58	73	1	58
59	74	1	59
60	75	1	60
61	76	1	61
62	77	1	62
63	78	1	63
64	79	1	64
65	80	1	65
66	81	1	66
67	82	1	67
68	83	1	68
69	84	1	69
70	85	1	70
71	86	1	71
72	87	1	72
73	88	1	73
74	89	1	74
75	90	1	75
76	91	1	76
77	92	1	77
78	93	1	78
79	94	1	79
80	95	1	80
81	96	1	81
82	97	1	82
83	98	1	83
84	103	1	84
85	200	1	85
86	211	1	86
87	212	1	87
88	213	1	88
89	214	1	89
90	215	1	90
91	216	1	91
92	217	1	92
93	218	1	93
94	219	1	94
95	220	1	95
96	221	1	96
97	222	1	97
98	223	1	98
99	226	1	99
100	227	1	100
101	238	1	101
102	240	1	102
103	241	1	103
104	242	1	104
105	243	1	105
106	244	1	106
107	245	1	107
108	246	1	108
";

        #endregion

        #region FILES

        // SELECT FileId, ContentType, FileNameWithoutExtension, Extension, Size FROM Files

        private static string _initialFiles = @"FileId	ContentType	FileNameWithoutExtension	Extension	Size
1	text/xml	ContentType	.ContentType	17616
2	text/xml	GenericContent	.ContentType	27634
3	text/xml	Application	.ContentType	7587
4	text/xml	ApplicationOverride	.ContentType	379
5	text/xml	Folder	.ContentType	1025
6	text/xml	ContentList	.ContentType	8434
7	text/xml	Aspect	.ContentType	6914
8	text/xml	FieldSettingContent	.ContentType	1257
9	text/xml	BinaryFieldSetting	.ContentType	341
10	text/xml	TextFieldSetting	.ContentType	350
11	text/xml	ShortTextFieldSetting	.ContentType	357
12	text/xml	ChoiceFieldSetting	.ContentType	353
13	text/xml	ContentLink	.ContentType	1104
14	text/xml	NumberFieldSetting	.ContentType	351
15	text/xml	CurrencyFieldSetting	.ContentType	356
16	text/xml	ItemList	.ContentType	1960
17	text/xml	CustomList	.ContentType	432
18	text/xml	ListItem	.ContentType	1008
19	text/xml	CustomListItem	.ContentType	2308
20	text/xml	DateTimeFieldSetting	.ContentType	357
21	text/xml	Device	.ContentType	568
22	text/xml	Library	.ContentType	1263
23	text/xml	DocumentLibrary	.ContentType	449
24	text/xml	Domain	.ContentType	2041
25	text/xml	Domains	.ContentType	409
26	text/xml	File	.ContentType	5144
27	text/xml	DynamicJsonContent	.ContentType	343
28	text/xml	Email	.ContentType	1434
29	text/xml	ExecutableFile	.ContentType	379
30	text/xml	ExportToCsvApplication	.ContentType	405
31	text/xml	GenericODataApplication	.ContentType	1133
32	text/xml	HttpHandlerApplication	.ContentType	412
33	text/xml	GetMetadataApplication	.ContentType	308
34	text/xml	Group	.ContentType	1939
35	text/xml	HtmlTemplate	.ContentType	799
36	text/xml	HttpStatusApplication	.ContentType	1542
37	text/xml	HyperLinkFieldSetting	.ContentType	360
38	text/xml	Image	.ContentType	1533
39	text/xml	ImageLibrary	.ContentType	1031
40	text/xml	ImgResizeApplication	.ContentType	8975
41	text/xml	Settings	.ContentType	1636
42	text/xml	IndexingSettings	.ContentType	852
43	text/xml	IntegerFieldSetting	.ContentType	353
44	text/xml	LoggingSettings	.ContentType	387
45	text/xml	LongTextFieldSetting	.ContentType	355
46	text/xml	Memo	.ContentType	1834
47	text/xml	MemoList	.ContentType	422
48	text/xml	NullFieldSetting	.ContentType	337
49	text/xml	OrganizationalUnit	.ContentType	2060
50	text/xml	PasswordFieldSetting	.ContentType	360
51	text/xml	PermissionChoiceFieldSetting	.ContentType	360
52	text/xml	PortalRoot	.ContentType	759
53	text/xml	PortalSettings	.ContentType	372
54	text/xml	PreviewImage	.ContentType	460
55	text/xml	ProfileDomain	.ContentType	1222
56	text/xml	Profiles	.ContentType	419
57	text/xml	Query	.ContentType	1227
58	text/xml	ReferenceFieldSetting	.ContentType	360
59	text/xml	SystemFile	.ContentType	590
60	text/xml	Resource	.ContentType	1642
61	text/xml	SystemFolder	.ContentType	590
62	text/xml	Resources	.ContentType	429
63	text/xml	RssApplication	.ContentType	373
64	text/xml	RuntimeContentContainer	.ContentType	408
65	text/xml	Workspace	.ContentType	4297
66	text/xml	Site	.ContentType	6747
67	text/xml	Sites	.ContentType	399
68	text/xml	SmartFolder	.ContentType	1672
69	text/xml	Task	.ContentType	4343
70	text/xml	TaskList	.ContentType	468
71	text/xml	TrashBag	.ContentType	2622
72	text/xml	TrashBin	.ContentType	4648
73	text/xml	User	.ContentType	13253
74	text/xml	UserProfile	.ContentType	2111
75	text/xml	WebServiceApplication	.ContentType	696
76	text/xml	XmlFieldSetting	.ContentType	342
77	text/xml	YesNoFieldSetting	.ContentType	347
78	application/octet-stream	Indexing	.settings	74
79	application/octet-stream	Logging	.settings	347
80	application/octet-stream	MailProcessor	.settings	321
81	application/octet-stream	Portal	.settings	717
82	application/octet-stream	TaskManagement	.settings	121
83	application/octet-stream	UserProfile	.settings	65
84	application/octet-stream	ExchangeService	.asmx	90
85	image/png	Admin	.png	731
86	text/xml	Content	.xml	19797
87	text/xml	CtdResourcesAB	.xml	25460
88	text/xml	CtdResourcesCD	.xml	30391
89	text/xml	CtdResourcesEF	.xml	12811
90	text/xml	CtdResourcesGH	.xml	48767
91	text/xml	CtdResourcesIJK	.xml	22739
92	text/xml	CtdResourcesLM	.xml	7361
93	text/xml	CtdResourcesNOP	.xml	5966
94	text/xml	CtdResourcesQ	.xml	2624
95	text/xml	CtdResourcesRS	.xml	20272
96	text/xml	CtdResourcesTZ	.xml	38763
97	text/xml	Exceptions	.xml	19340
98	text/xml	Trash	.xml	6728
99	text/html	Global	.html	15642
100	text/html	UserGlobal	.html	15125
101	application/octet-stream	binaryhandler	.ashx	83
102	application/octet-stream	Dws	.asmx	75
103	application/octet-stream	Fpp	.ashx	75
104	application/octet-stream	Lists	.asmx	77
105	text/asp	owssvr	.aspx	9520
106	application/octet-stream	Versions	.asmx	80
107	application/octet-stream	Webs	.asmx	76
108	application/octet-stream	vsshandler	.ashx	90
";

        #endregion

        #region TEXTPROPERTIES

        private static string _initialTextProperties = @"TextPropertyNVarcharId	VersionId	PropertyTypeId	Value
1	105	70	bool generateMissing
2	107	3	
3	109	70	bool empty
4	110	70	int page
5	112	70	int pageCount
6	113	70	SenseNet.Preview.PreviewStatus status
7	116	3	
8	117	3	
9	118	3	
10	119	3	
11	122	70	string[] contentTypes
12	123	70	\n      string level,\n      string member,\n      string[] permissions\n    
13	124	70	\n      string level,\n      bool explicitOnly,\n      string member,\n      string[] includedTypes\n    
14	125	70	string skin, string category
15	126	3	
16	127	70	\n      string username, \n      string password\n    
17	128	3	
18	129	3	
19	131	70	\n      bool recursive, \n      SenseNet.ContentRepository.Storage.Search.IndexRebuildLevel rebuildLevel\n    
20	135	70	\n      string level,\n      bool explicitOnly,\n      string member,\n      string[] permissions,\n    
21	136	3	
22	137	70	string[] contentTypes
23	138	3	
24	139	3	
25	140	3	
26	140	70	
27	142	3	
28	143	3	
29	144	70	\n      string query,\n      string displayName,\n      string queryType\n    
30	146	70	long fullSize, string fieldName
31	147	70	string name, string contentType, long fullSize, string fieldName
32	148	70	\n      string user\n    
33	149	3	
34	150	70	\n      string level,\n      string kind,\n      string[] permissions\n    
35	151	70	\n      string level,\n      string kind\n    
36	152	3	
37	152	70	
38	153	3	
39	154	3	
40	157	70	\n      bool recurse\n    
41	159	3	
42	159	70	
43	160	3	
44	161	3	
45	162	70	SenseNet.TaskManagement.Core.SnTaskResult result
46	163	70	string token, long fullSize, string fieldName, string fileName
47	166	3	
48	166	70	
49	167	3	
50	167	70	
51	168	3	
52	168	70	\n      string[] permissions\n    
53	169	70	string fieldName
54	170	70	\n      string identity\n    
55	172	70	\n      string displayName\n    
56	173	70	\n      string identity\n    
57	174	70	\n      string identity\n    
58	175	3	
59	177	70	\n      bool onlyPublic\n    
60	178	70	
61	179	3	
62	179	70	
63	180	3	
64	180	70	string userOrGroup
65	183	70	int[] contentIds
66	184	3	
67	184	70	\n      bool directOnly\n    
68	185	70	int[] contentIds
69	187	3	
70	189	3	
71	191	70	
72	195	70	string text
73	196	70	string text
74	198	3	
75	198	70	\n      bool directOnly\n    
76	199	3	
77	199	70	string back
78	1	129	<?xml version=""1.0"" encoding=""utf-16""?>\n<ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">\n  <OldPasswordData>\n    <ModificationDate>2017-08-15T22:30:26.4952014Z</ModificationDate>\n    <Hash>$2a$10$q/.2oJCHigVA3mOeMiNTCOyUEfgaMmPJeIRHd9IszeXkac/x8lwCe</Hash>\n  </OldPasswordData>\n</ArrayOfOldPasswordData>
79	201	3	
80	202	3	
81	203	3	
82	204	3	
83	205	3	
84	11	3	Members of this group are able to perform administrative tasks in the Content Repository - e.g. importing the creation date of content.
85	206	3	
86	207	3	
87	208	3	
88	10	129	<?xml version=""1.0"" encoding=""utf-16""?>\n<ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">\n  <OldPasswordData>\n    <ModificationDate>2017-08-15T22:30:27.3772574Z</ModificationDate>\n    <Hash>$2a$10$bkZT7Pv22O0QSFyPmGkVjuZy5HcQ.6O7MjJ1SOQKCHVowuv6WAMdi</Hash>\n  </OldPasswordData>\n</ArrayOfOldPasswordData>
89	209	129	<?xml version=""1.0"" encoding=""utf-16""?>\n<ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" />
90	6	129	<?xml version=""1.0"" encoding=""utf-16""?>\n<ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" />
91	13	8	SystemFolder
92	14	8	ContentType
93	228	8	GetMetadataApplication SystemFolder Folder
94	229	8	GetMetadataApplication SystemFolder Folder
95	246	3	Http handler for serving Lucene index file paths. This content can be invoked only from the local machine.";

        #endregion

        #region SCHEMA

        private static readonly string TestSchema = @"<?xml version='1.0' encoding='utf-8' ?>
<StorageSchema xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/Storage/Schema'>
	<UsedPropertyTypes>
		<PropertyType itemID='1' name='Binary' dataType='Binary' mapping='0' />
		<PropertyType itemID='2' name='VersioningMode' dataType='Int' mapping='0' />
		<PropertyType itemID='3' name='Description' dataType='Text' mapping='0' />
		<PropertyType itemID='4' name='Hidden' dataType='Int' mapping='1' />
		<PropertyType itemID='5' name='InheritableVersioningMode' dataType='Int' mapping='2' />
		<PropertyType itemID='6' name='ApprovingMode' dataType='Int' mapping='3' />
		<PropertyType itemID='7' name='InheritableApprovingMode' dataType='Int' mapping='4' />
		<PropertyType itemID='8' name='AllowedChildTypes' dataType='Text' mapping='1' />
		<PropertyType itemID='9' name='TrashDisabled' dataType='Int' mapping='5' />
		<PropertyType itemID='10' name='EnableLifespan' dataType='Int' mapping='6' />
		<PropertyType itemID='11' name='ValidFrom' dataType='DateTime' mapping='0' />
		<PropertyType itemID='12' name='ValidTill' dataType='DateTime' mapping='1' />
		<PropertyType itemID='13' name='Aspects' dataType='Reference' mapping='0' />
		<PropertyType itemID='14' name='AspectData' dataType='Text' mapping='2' />
		<PropertyType itemID='15' name='BrowseApplication' dataType='Reference' mapping='1' />
		<PropertyType itemID='16' name='ExtensionData' dataType='Text' mapping='3' />
		<PropertyType itemID='17' name='IsTaggable' dataType='Int' mapping='7' />
		<PropertyType itemID='18' name='Tags' dataType='Text' mapping='4' />
		<PropertyType itemID='19' name='IsRateable' dataType='Int' mapping='8' />
		<PropertyType itemID='20' name='RateStr' dataType='String' mapping='0' />
		<PropertyType itemID='21' name='RateAvg' dataType='Currency' mapping='0' />
		<PropertyType itemID='22' name='RateCount' dataType='Int' mapping='9' />
		<PropertyType itemID='23' name='CheckInComments' dataType='Text' mapping='5' />
		<PropertyType itemID='24' name='RejectReason' dataType='Text' mapping='6' />
		<PropertyType itemID='25' name='AppName' dataType='String' mapping='1' />
		<PropertyType itemID='26' name='Disabled' dataType='Int' mapping='10' />
		<PropertyType itemID='27' name='IsModal' dataType='Int' mapping='11' />
		<PropertyType itemID='28' name='Clear' dataType='Int' mapping='12' />
		<PropertyType itemID='29' name='Scenario' dataType='String' mapping='2' />
		<PropertyType itemID='30' name='ActionTypeName' dataType='String' mapping='3' />
		<PropertyType itemID='31' name='StyleHint' dataType='String' mapping='4' />
		<PropertyType itemID='32' name='RequiredPermissions' dataType='String' mapping='5' />
		<PropertyType itemID='33' name='DeepPermissionCheck' dataType='Int' mapping='13' />
		<PropertyType itemID='34' name='IncludeBackUrl' dataType='String' mapping='6' />
		<PropertyType itemID='35' name='CacheControl' dataType='String' mapping='7' />
		<PropertyType itemID='36' name='MaxAge' dataType='String' mapping='8' />
		<PropertyType itemID='37' name='CustomUrlParameters' dataType='String' mapping='9' />
		<PropertyType itemID='38' name='StoredIcon' dataType='String' mapping='10' />
		<PropertyType itemID='39' name='ContentListBindings' dataType='Text' mapping='7' />
		<PropertyType itemID='40' name='ContentListDefinition' dataType='Text' mapping='8' />
		<PropertyType itemID='41' name='DefaultView' dataType='String' mapping='11' />
		<PropertyType itemID='42' name='AvailableViews' dataType='Reference' mapping='2' />
		<PropertyType itemID='43' name='AvailableContentTypeFields' dataType='Reference' mapping='3' />
		<PropertyType itemID='44' name='ListEmail' dataType='String' mapping='12' />
		<PropertyType itemID='45' name='ExchangeSubscriptionId' dataType='String' mapping='13' />
		<PropertyType itemID='46' name='OverwriteFiles' dataType='Int' mapping='14' />
		<PropertyType itemID='47' name='GroupAttachments' dataType='String' mapping='14' />
		<PropertyType itemID='48' name='SaveOriginalEmail' dataType='Int' mapping='15' />
		<PropertyType itemID='49' name='IncomingEmailWorkflow' dataType='Reference' mapping='4' />
		<PropertyType itemID='50' name='OnlyFromLocalGroups' dataType='Int' mapping='16' />
		<PropertyType itemID='51' name='InboxFolder' dataType='String' mapping='15' />
		<PropertyType itemID='52' name='OwnerWhenVisitor' dataType='Reference' mapping='5' />
		<PropertyType itemID='53' name='AspectDefinition' dataType='Text' mapping='9' />
		<PropertyType itemID='54' name='FieldSettingContents' dataType='Reference' mapping='6' />
		<PropertyType itemID='55' name='Link' dataType='Reference' mapping='7' />
		<PropertyType itemID='56' name='WorkflowsRunning' dataType='Int' mapping='17' />
		<PropertyType itemID='57' name='UserAgentPattern' dataType='String' mapping='16' />
		<PropertyType itemID='58' name='SyncGuid' dataType='String' mapping='17' />
		<PropertyType itemID='59' name='LastSync' dataType='DateTime' mapping='2' />
		<PropertyType itemID='60' name='Watermark' dataType='String' mapping='18' />
		<PropertyType itemID='61' name='PageCount' dataType='Int' mapping='18' />
		<PropertyType itemID='62' name='MimeType' dataType='String' mapping='19' />
		<PropertyType itemID='63' name='Shapes' dataType='Text' mapping='10' />
		<PropertyType itemID='64' name='PageAttributes' dataType='Text' mapping='11' />
		<PropertyType itemID='65' name='From' dataType='String' mapping='20' />
		<PropertyType itemID='66' name='Body' dataType='Text' mapping='12' />
		<PropertyType itemID='67' name='Sent' dataType='DateTime' mapping='3' />
		<PropertyType itemID='68' name='ClassName' dataType='String' mapping='21' />
		<PropertyType itemID='69' name='MethodName' dataType='String' mapping='22' />
		<PropertyType itemID='70' name='Parameters' dataType='Text' mapping='13' />
		<PropertyType itemID='71' name='Members' dataType='Reference' mapping='8' />
		<PropertyType itemID='72' name='StatusCode' dataType='String' mapping='23' />
		<PropertyType itemID='73' name='RedirectUrl' dataType='String' mapping='24' />
		<PropertyType itemID='74' name='Width' dataType='Int' mapping='19' />
		<PropertyType itemID='75' name='Height' dataType='Int' mapping='20' />
		<PropertyType itemID='76' name='Keywords' dataType='Text' mapping='14' />
		<PropertyType itemID='77' name='DateTaken' dataType='DateTime' mapping='4' />
		<PropertyType itemID='78' name='CoverImage' dataType='Reference' mapping='9' />
		<PropertyType itemID='79' name='ImageType' dataType='String' mapping='25' />
		<PropertyType itemID='80' name='ImageFieldName' dataType='String' mapping='26' />
		<PropertyType itemID='81' name='Stretch' dataType='Int' mapping='21' />
		<PropertyType itemID='82' name='OutputFormat' dataType='String' mapping='27' />
		<PropertyType itemID='83' name='SmoothingMode' dataType='String' mapping='28' />
		<PropertyType itemID='84' name='InterpolationMode' dataType='String' mapping='29' />
		<PropertyType itemID='85' name='PixelOffsetMode' dataType='String' mapping='30' />
		<PropertyType itemID='86' name='ResizeTypeMode' dataType='String' mapping='31' />
		<PropertyType itemID='87' name='CropVAlign' dataType='String' mapping='32' />
		<PropertyType itemID='88' name='CropHAlign' dataType='String' mapping='33' />
		<PropertyType itemID='89' name='GlobalOnly' dataType='Int' mapping='22' />
		<PropertyType itemID='90' name='Date' dataType='DateTime' mapping='5' />
		<PropertyType itemID='91' name='MemoType' dataType='String' mapping='34' />
		<PropertyType itemID='92' name='SeeAlso' dataType='Reference' mapping='10' />
		<PropertyType itemID='93' name='Query' dataType='Text' mapping='15' />
		<PropertyType itemID='94' name='Downloads' dataType='Currency' mapping='1' />
		<PropertyType itemID='95' name='IsActive' dataType='Int' mapping='23' />
		<PropertyType itemID='96' name='IsWallContainer' dataType='Int' mapping='24' />
		<PropertyType itemID='97' name='WorkspaceSkin' dataType='Reference' mapping='11' />
		<PropertyType itemID='98' name='Manager' dataType='Reference' mapping='12' />
		<PropertyType itemID='99' name='Deadline' dataType='DateTime' mapping='6' />
		<PropertyType itemID='100' name='IsCritical' dataType='Int' mapping='25' />
		<PropertyType itemID='101' name='PendingUserLang' dataType='String' mapping='35' />
		<PropertyType itemID='102' name='Language' dataType='String' mapping='36' />
		<PropertyType itemID='103' name='EnableClientBasedCulture' dataType='Int' mapping='26' />
		<PropertyType itemID='104' name='EnableUserBasedCulture' dataType='Int' mapping='27' />
		<PropertyType itemID='105' name='UrlList' dataType='Text' mapping='16' />
		<PropertyType itemID='106' name='StartPage' dataType='Reference' mapping='13' />
		<PropertyType itemID='107' name='LoginPage' dataType='Reference' mapping='14' />
		<PropertyType itemID='108' name='SiteSkin' dataType='Reference' mapping='15' />
		<PropertyType itemID='109' name='DenyCrossSiteAccess' dataType='Int' mapping='28' />
		<PropertyType itemID='110' name='EnableAutofilters' dataType='String' mapping='37' />
		<PropertyType itemID='111' name='EnableLifespanFilter' dataType='String' mapping='38' />
		<PropertyType itemID='112' name='StartDate' dataType='DateTime' mapping='7' />
		<PropertyType itemID='113' name='DueDate' dataType='DateTime' mapping='8' />
		<PropertyType itemID='114' name='AssignedTo' dataType='Reference' mapping='16' />
		<PropertyType itemID='115' name='Priority' dataType='String' mapping='39' />
		<PropertyType itemID='116' name='Status' dataType='String' mapping='40' />
		<PropertyType itemID='117' name='TaskCompletion' dataType='Int' mapping='29' />
		<PropertyType itemID='118' name='KeepUntil' dataType='DateTime' mapping='9' />
		<PropertyType itemID='119' name='OriginalPath' dataType='String' mapping='41' />
		<PropertyType itemID='120' name='WorkspaceId' dataType='Int' mapping='30' />
		<PropertyType itemID='121' name='WorkspaceRelativePath' dataType='String' mapping='42' />
		<PropertyType itemID='122' name='MinRetentionTime' dataType='Int' mapping='31' />
		<PropertyType itemID='123' name='SizeQuota' dataType='Int' mapping='32' />
		<PropertyType itemID='124' name='BagCapacity' dataType='Int' mapping='33' />
		<PropertyType itemID='125' name='Enabled' dataType='Int' mapping='34' />
		<PropertyType itemID='126' name='Domain' dataType='String' mapping='43' />
		<PropertyType itemID='127' name='Email' dataType='String' mapping='44' />
		<PropertyType itemID='128' name='FullName' dataType='String' mapping='45' />
		<PropertyType itemID='129' name='OldPasswords' dataType='Text' mapping='17' />
		<PropertyType itemID='130' name='PasswordHash' dataType='String' mapping='46' />
		<PropertyType itemID='131' name='LoginName' dataType='String' mapping='47' />
		<PropertyType itemID='132' name='Profile' dataType='Reference' mapping='17' />
		<PropertyType itemID='133' name='FollowedWorkspaces' dataType='Reference' mapping='18' />
		<PropertyType itemID='134' name='JobTitle' dataType='String' mapping='48' />
		<PropertyType itemID='135' name='ImageRef' dataType='Reference' mapping='19' />
		<PropertyType itemID='136' name='ImageData' dataType='Binary' mapping='1' />
		<PropertyType itemID='137' name='Captcha' dataType='String' mapping='49' />
		<PropertyType itemID='138' name='Department' dataType='String' mapping='50' />
		<PropertyType itemID='139' name='Languages' dataType='String' mapping='51' />
		<PropertyType itemID='140' name='Phone' dataType='String' mapping='52' />
		<PropertyType itemID='141' name='Gender' dataType='String' mapping='53' />
		<PropertyType itemID='142' name='MaritalStatus' dataType='String' mapping='54' />
		<PropertyType itemID='143' name='BirthDate' dataType='DateTime' mapping='10' />
		<PropertyType itemID='144' name='Education' dataType='Text' mapping='18' />
		<PropertyType itemID='145' name='TwitterAccount' dataType='String' mapping='55' />
		<PropertyType itemID='146' name='FacebookURL' dataType='String' mapping='56' />
		<PropertyType itemID='147' name='LinkedInURL' dataType='String' mapping='57' />
	</UsedPropertyTypes>
	<NodeTypeHierarchy>
		<NodeType itemID='10' name='GenericContent' className='SenseNet.ContentRepository.GenericContent'>
			<PropertyType name='VersioningMode' />
			<PropertyType name='Description' />
			<PropertyType name='Hidden' />
			<PropertyType name='InheritableVersioningMode' />
			<PropertyType name='ApprovingMode' />
			<PropertyType name='InheritableApprovingMode' />
			<PropertyType name='AllowedChildTypes' />
			<PropertyType name='TrashDisabled' />
			<PropertyType name='EnableLifespan' />
			<PropertyType name='ValidFrom' />
			<PropertyType name='ValidTill' />
			<PropertyType name='Aspects' />
			<PropertyType name='AspectData' />
			<PropertyType name='BrowseApplication' />
			<PropertyType name='ExtensionData' />
			<PropertyType name='IsTaggable' />
			<PropertyType name='Tags' />
			<PropertyType name='IsRateable' />
			<PropertyType name='RateStr' />
			<PropertyType name='RateAvg' />
			<PropertyType name='RateCount' />
			<PropertyType name='CheckInComments' />
			<PropertyType name='RejectReason' />
			<NodeType itemID='3' name='User' className='SenseNet.ContentRepository.User'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Description' />
				<PropertyType name='Hidden' />
				<PropertyType name='InheritableVersioningMode' />
				<PropertyType name='ApprovingMode' />
				<PropertyType name='InheritableApprovingMode' />
				<PropertyType name='AllowedChildTypes' />
				<PropertyType name='TrashDisabled' />
				<PropertyType name='EnableLifespan' />
				<PropertyType name='ValidFrom' />
				<PropertyType name='ValidTill' />
				<PropertyType name='Aspects' />
				<PropertyType name='AspectData' />
				<PropertyType name='BrowseApplication' />
				<PropertyType name='SyncGuid' />
				<PropertyType name='LastSync' />
				<PropertyType name='Manager' />
				<PropertyType name='Language' />
				<PropertyType name='Enabled' />
				<PropertyType name='Domain' />
				<PropertyType name='Email' />
				<PropertyType name='FullName' />
				<PropertyType name='OldPasswords' />
				<PropertyType name='PasswordHash' />
				<PropertyType name='LoginName' />
				<PropertyType name='Profile' />
				<PropertyType name='FollowedWorkspaces' />
				<PropertyType name='JobTitle' />
				<PropertyType name='ImageRef' />
				<PropertyType name='ImageData' />
				<PropertyType name='Captcha' />
				<PropertyType name='Department' />
				<PropertyType name='Languages' />
				<PropertyType name='Phone' />
				<PropertyType name='Gender' />
				<PropertyType name='MaritalStatus' />
				<PropertyType name='BirthDate' />
				<PropertyType name='Education' />
				<PropertyType name='TwitterAccount' />
				<PropertyType name='FacebookURL' />
				<PropertyType name='LinkedInURL' />
			</NodeType>
			<NodeType itemID='2' name='Group' className='SenseNet.ContentRepository.Group'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Description' />
				<PropertyType name='Hidden' />
				<PropertyType name='InheritableVersioningMode' />
				<PropertyType name='ApprovingMode' />
				<PropertyType name='InheritableApprovingMode' />
				<PropertyType name='AllowedChildTypes' />
				<PropertyType name='TrashDisabled' />
				<PropertyType name='EnableLifespan' />
				<PropertyType name='ValidFrom' />
				<PropertyType name='ValidTill' />
				<PropertyType name='Aspects' />
				<PropertyType name='AspectData' />
				<PropertyType name='BrowseApplication' />
				<PropertyType name='SyncGuid' />
				<PropertyType name='LastSync' />
				<PropertyType name='Members' />
			</NodeType>
			<NodeType itemID='1' name='Folder' className='SenseNet.ContentRepository.Folder'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Description' />
				<PropertyType name='Hidden' />
				<PropertyType name='InheritableVersioningMode' />
				<PropertyType name='ApprovingMode' />
				<PropertyType name='InheritableApprovingMode' />
				<PropertyType name='TrashDisabled' />
				<PropertyType name='EnableLifespan' />
				<PropertyType name='ValidFrom' />
				<PropertyType name='ValidTill' />
				<PropertyType name='Aspects' />
				<PropertyType name='AspectData' />
				<PropertyType name='BrowseApplication' />
				<NodeType itemID='34' name='TrashBag' className='SenseNet.ContentRepository.TrashBag'>
					<PropertyType name='Link' />
					<PropertyType name='KeepUntil' />
					<PropertyType name='OriginalPath' />
					<PropertyType name='WorkspaceId' />
					<PropertyType name='WorkspaceRelativePath' />
				</NodeType>
				<NodeType itemID='33' name='SmartFolder' className='SenseNet.ContentRepository.SmartFolder'>
					<PropertyType name='Query' />
					<PropertyType name='EnableAutofilters' />
					<PropertyType name='EnableLifespanFilter' />
				</NodeType>
				<NodeType itemID='32' name='Sites' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='31' name='Workspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
					<PropertyType name='IsActive' />
					<PropertyType name='IsWallContainer' />
					<PropertyType name='WorkspaceSkin' />
					<PropertyType name='Manager' />
					<PropertyType name='Deadline' />
					<PropertyType name='IsCritical' />
					<NodeType itemID='60' name='UserProfile' className='SenseNet.ContentRepository.UserProfile' />
					<NodeType itemID='59' name='TrashBin' className='SenseNet.ContentRepository.TrashBin'>
						<PropertyType name='MinRetentionTime' />
						<PropertyType name='SizeQuota' />
						<PropertyType name='BagCapacity' />
					</NodeType>
					<NodeType itemID='58' name='Site' className='SenseNet.Portal.Site'>
						<PropertyType name='PendingUserLang' />
						<PropertyType name='Language' />
						<PropertyType name='EnableClientBasedCulture' />
						<PropertyType name='EnableUserBasedCulture' />
						<PropertyType name='UrlList' />
						<PropertyType name='StartPage' />
						<PropertyType name='LoginPage' />
						<PropertyType name='SiteSkin' />
						<PropertyType name='DenyCrossSiteAccess' />
					</NodeType>
				</NodeType>
				<NodeType itemID='30' name='RuntimeContentContainer' className='SenseNet.ContentRepository.RuntimeContentContainer' />
				<NodeType itemID='29' name='Profiles' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='28' name='ProfileDomain' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='27' name='Email' className='SenseNet.ContentRepository.Folder'>
					<PropertyType name='From' />
					<PropertyType name='Body' />
					<PropertyType name='Sent' />
				</NodeType>
				<NodeType itemID='26' name='Device' className='SenseNet.ApplicationModel.Device'>
					<PropertyType name='UserAgentPattern' />
				</NodeType>
				<NodeType itemID='25' name='ContentList' className='SenseNet.ContentRepository.ContentList'>
					<PropertyType name='ContentListBindings' />
					<PropertyType name='ContentListDefinition' />
					<PropertyType name='DefaultView' />
					<PropertyType name='AvailableViews' />
					<PropertyType name='AvailableContentTypeFields' />
					<PropertyType name='ListEmail' />
					<PropertyType name='ExchangeSubscriptionId' />
					<PropertyType name='OverwriteFiles' />
					<PropertyType name='GroupAttachments' />
					<PropertyType name='SaveOriginalEmail' />
					<PropertyType name='IncomingEmailWorkflow' />
					<PropertyType name='OnlyFromLocalGroups' />
					<PropertyType name='InboxFolder' />
					<PropertyType name='OwnerWhenVisitor' />
					<NodeType itemID='57' name='Library' className='SenseNet.ContentRepository.ContentList'>
						<NodeType itemID='73' name='ImageLibrary' className='SenseNet.ContentRepository.ContentList'>
							<PropertyType name='CoverImage' />
						</NodeType>
						<NodeType itemID='72' name='DocumentLibrary' className='SenseNet.ContentRepository.ContentList' />
					</NodeType>
					<NodeType itemID='56' name='ItemList' className='SenseNet.ContentRepository.ContentList'>
						<NodeType itemID='71' name='TaskList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='70' name='MemoList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='69' name='CustomList' className='SenseNet.ContentRepository.ContentList' />
					</NodeType>
					<NodeType itemID='55' name='Aspect' className='SenseNet.ContentRepository.Aspect'>
						<PropertyType name='AspectDefinition' />
						<PropertyType name='FieldSettingContents' />
					</NodeType>
				</NodeType>
				<NodeType itemID='8' name='OrganizationalUnit' className='SenseNet.ContentRepository.OrganizationalUnit'>
					<PropertyType name='SyncGuid' />
					<PropertyType name='LastSync' />
				</NodeType>
				<NodeType itemID='7' name='Domain' className='SenseNet.ContentRepository.Domain'>
					<PropertyType name='SyncGuid' />
					<PropertyType name='LastSync' />
				</NodeType>
				<NodeType itemID='6' name='Domains' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='5' name='SystemFolder' className='SenseNet.ContentRepository.SystemFolder'>
					<NodeType itemID='54' name='Resources' className='SenseNet.ContentRepository.SystemFolder' />
				</NodeType>
				<NodeType itemID='4' name='PortalRoot' className='SenseNet.ContentRepository.PortalRoot'>
					<PropertyType name='VersioningMode' />
					<PropertyType name='Description' />
					<PropertyType name='Hidden' />
					<PropertyType name='InheritableVersioningMode' />
					<PropertyType name='ApprovingMode' />
					<PropertyType name='InheritableApprovingMode' />
					<PropertyType name='TrashDisabled' />
					<PropertyType name='EnableLifespan' />
					<PropertyType name='ValidFrom' />
					<PropertyType name='ValidTill' />
					<PropertyType name='Aspects' />
					<PropertyType name='AspectData' />
					<PropertyType name='BrowseApplication' />
				</NodeType>
			</NodeType>
			<NodeType itemID='16' name='Query' className='SenseNet.ContentRepository.QueryContent'>
				<PropertyType name='Query' />
			</NodeType>
			<NodeType itemID='15' name='File' className='SenseNet.ContentRepository.File'>
				<PropertyType name='Binary' />
				<PropertyType name='Watermark' />
				<PropertyType name='PageCount' />
				<PropertyType name='MimeType' />
				<PropertyType name='Shapes' />
				<PropertyType name='PageAttributes' />
				<NodeType itemID='52' name='SystemFile' className='SenseNet.ContentRepository.File'>
					<NodeType itemID='68' name='Resource' className='SenseNet.ContentRepository.i18n.Resource'>
						<PropertyType name='Downloads' />
					</NodeType>
				</NodeType>
				<NodeType itemID='51' name='Settings' className='SenseNet.ContentRepository.Settings'>
					<PropertyType name='GlobalOnly' />
					<NodeType itemID='67' name='PortalSettings' className='SenseNet.Portal.PortalSettings' />
					<NodeType itemID='66' name='LoggingSettings' className='SenseNet.ContentRepository.LoggingSettings' />
					<NodeType itemID='65' name='IndexingSettings' className='SenseNet.Search.IndexingSettings' />
				</NodeType>
				<NodeType itemID='50' name='Image' className='SenseNet.ContentRepository.Image'>
					<PropertyType name='Width' />
					<PropertyType name='Height' />
					<PropertyType name='Keywords' />
					<PropertyType name='DateTaken' />
					<NodeType itemID='64' name='PreviewImage' className='SenseNet.ContentRepository.Image' />
				</NodeType>
				<NodeType itemID='49' name='HtmlTemplate' className='SenseNet.Portal.UI.HtmlTemplate' />
				<NodeType itemID='48' name='ExecutableFile' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='47' name='DynamicJsonContent' className='SenseNet.Portal.Handlers.DynamicJsonContent' />
			</NodeType>
			<NodeType itemID='14' name='ListItem' className='SenseNet.ContentRepository.GenericContent'>
				<NodeType itemID='46' name='Task' className='SenseNet.ContentRepository.Task'>
					<PropertyType name='StartDate' />
					<PropertyType name='DueDate' />
					<PropertyType name='AssignedTo' />
					<PropertyType name='Priority' />
					<PropertyType name='Status' />
					<PropertyType name='TaskCompletion' />
				</NodeType>
				<NodeType itemID='45' name='Memo' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Date' />
					<PropertyType name='MemoType' />
					<PropertyType name='SeeAlso' />
				</NodeType>
				<NodeType itemID='44' name='CustomListItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='WorkflowsRunning' />
				</NodeType>
			</NodeType>
			<NodeType itemID='13' name='ContentLink' className='SenseNet.ContentRepository.ContentLink'>
				<PropertyType name='Link' />
			</NodeType>
			<NodeType itemID='12' name='FieldSettingContent' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
				<NodeType itemID='43' name='XmlFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='42' name='ReferenceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='41' name='NullFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='40' name='IntegerFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='39' name='HyperLinkFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='38' name='DateTimeFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='37' name='NumberFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
					<NodeType itemID='63' name='CurrencyFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				</NodeType>
				<NodeType itemID='36' name='TextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
					<NodeType itemID='62' name='LongTextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
					<NodeType itemID='61' name='ShortTextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
						<NodeType itemID='75' name='PasswordFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
						<NodeType itemID='74' name='ChoiceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
							<NodeType itemID='77' name='YesNoFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
							<NodeType itemID='76' name='PermissionChoiceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
						</NodeType>
					</NodeType>
				</NodeType>
				<NodeType itemID='35' name='BinaryFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
			</NodeType>
			<NodeType itemID='11' name='Application' className='SenseNet.ApplicationModel.Application'>
				<PropertyType name='AppName' />
				<PropertyType name='Disabled' />
				<PropertyType name='IsModal' />
				<PropertyType name='Clear' />
				<PropertyType name='Scenario' />
				<PropertyType name='ActionTypeName' />
				<PropertyType name='StyleHint' />
				<PropertyType name='RequiredPermissions' />
				<PropertyType name='DeepPermissionCheck' />
				<PropertyType name='IncludeBackUrl' />
				<PropertyType name='CacheControl' />
				<PropertyType name='MaxAge' />
				<PropertyType name='CustomUrlParameters' />
				<PropertyType name='StoredIcon' />
				<NodeType itemID='24' name='WebServiceApplication' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Binary' />
				</NodeType>
				<NodeType itemID='23' name='RssApplication' className='SenseNet.Services.RssApplication' />
				<NodeType itemID='22' name='ImgResizeApplication' className='SenseNet.Portal.ApplicationModel.ImgResizeApplication'>
					<PropertyType name='Width' />
					<PropertyType name='Height' />
					<PropertyType name='ImageType' />
					<PropertyType name='ImageFieldName' />
					<PropertyType name='Stretch' />
					<PropertyType name='OutputFormat' />
					<PropertyType name='SmoothingMode' />
					<PropertyType name='InterpolationMode' />
					<PropertyType name='PixelOffsetMode' />
					<PropertyType name='ResizeTypeMode' />
					<PropertyType name='CropVAlign' />
					<PropertyType name='CropHAlign' />
				</NodeType>
				<NodeType itemID='21' name='HttpStatusApplication' className='SenseNet.Portal.AppModel.HttpStatusApplication'>
					<PropertyType name='StatusCode' />
					<PropertyType name='RedirectUrl' />
				</NodeType>
				<NodeType itemID='20' name='HttpHandlerApplication' className='SenseNet.Portal.Handlers.HttpHandlerApplication'>
					<NodeType itemID='53' name='GetMetadataApplication' className='SenseNet.Portal.Handlers.GetMetadataApplication' />
				</NodeType>
				<NodeType itemID='19' name='GenericODataApplication' className='SenseNet.Portal.ApplicationModel.GenericODataApplication'>
					<PropertyType name='ClassName' />
					<PropertyType name='MethodName' />
					<PropertyType name='Parameters' />
				</NodeType>
				<NodeType itemID='18' name='ExportToCsvApplication' className='SenseNet.Services.ExportToCsvApplication' />
				<NodeType itemID='17' name='ApplicationOverride' className='SenseNet.ApplicationModel.Application' />
			</NodeType>
		</NodeType>
		<NodeType itemID='9' name='ContentType' className='SenseNet.ContentRepository.Schema.ContentType'>
			<PropertyType name='Binary' />
		</NodeType>
	</NodeTypeHierarchy>
</StorageSchema>
";
        #endregion
    }
}
