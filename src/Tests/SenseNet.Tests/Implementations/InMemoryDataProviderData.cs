namespace SenseNet.Tests.Implementations
{
    public partial class InMemoryDataProvider
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
        //         3.3.5 - FLATPROPERTIES STRING
        //                 SELECT Id, VersionId, Page, nvarchar_1, nvarchar_2, nvarchar_3, nvarchar_4, nvarchar_5, nvarchar_6, nvarchar_7, nvarchar_8, nvarchar_9, nvarchar_10, nvarchar_11, nvarchar_12, nvarchar_13, nvarchar_14, nvarchar_15, nvarchar_16, nvarchar_17, nvarchar_18, nvarchar_19, nvarchar_20, nvarchar_21, nvarchar_22, nvarchar_23, nvarchar_24, nvarchar_25, nvarchar_26, nvarchar_27, nvarchar_28, nvarchar_29, nvarchar_30, nvarchar_31, nvarchar_32, nvarchar_33, nvarchar_34, nvarchar_35, nvarchar_36, nvarchar_37, nvarchar_38, nvarchar_39, nvarchar_40, nvarchar_41, nvarchar_42, nvarchar_43, nvarchar_44, nvarchar_45, nvarchar_46, nvarchar_47, nvarchar_48, nvarchar_49, nvarchar_50, nvarchar_51, nvarchar_52, nvarchar_53, nvarchar_54, nvarchar_55, nvarchar_56, nvarchar_57, nvarchar_58, nvarchar_59, nvarchar_60, nvarchar_61, nvarchar_62, nvarchar_63, nvarchar_64, nvarchar_65, nvarchar_66, nvarchar_67, nvarchar_68, nvarchar_69, nvarchar_70, nvarchar_71, nvarchar_72, nvarchar_73, nvarchar_74, nvarchar_75, nvarchar_76, nvarchar_77, nvarchar_78, nvarchar_79, nvarchar_80 FROM FlatProperties
        //         3.3.6 - FLATPROPERTIES INT
        //                 SELECT Id, VersionId, Page, int_1, int_2, int_3, int_4, int_5, int_6, int_7, int_8, int_9, int_10, int_11, int_12, int_13, int_14, int_15, int_16, int_17, int_18, int_19, int_20, int_21, int_22, int_23, int_24, int_25, int_26, int_27, int_28, int_29, int_30, int_31, int_32, int_33, int_34, int_35, int_36, int_37, int_38, int_39, int_40 FROM FlatProperties
        //         3.3.7 - FLATPROPERTIES DATETIME
        //                 SELECT Id, VersionId, Page, datetime_1, datetime_2, datetime_3, datetime_4, datetime_5, datetime_6, datetime_7, datetime_8, datetime_9, datetime_10, datetime_11, datetime_12, datetime_13, datetime_14, datetime_15, datetime_16, datetime_17, datetime_18, datetime_19, datetime_20, datetime_21, datetime_22, datetime_23, datetime_24, datetime_25 FROM FlatProperties
        //         3.3.8 - FLATPROPERTIES DECIMAL
        //                 SELECT Id, VersionId, Page, money_1, money_2, money_3, money_4, money_5, money_6, money_7, money_8, money_9, money_10, money_11, money_12, money_13, money_14, money_15 FROM FlatProperties
        //         3.3.9 - REFERENCEPROPERTIES
        //                 SELECT ReferencePropertyId, VersionId, PropertyTypeId, ReferredNodeId FROM ReferenceProperties
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

        private static string _prototypeNodes = @"NodeId	ParentNodeId	NodeTypeId	LastMajorVersionId	LastMinorVersionId	Index	IsSystem	Name	(No column name)	Path
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
12	5	3	12	12	8	0	Startup	""""	/Root/IMS/BuiltIn/Portal/Startup
1000	2	5	13	13	3	1	System	""""	/Root/System
1001	1000	5	14	14	1	1	Schema	Schema	/Root/System/Schema
1002	1001	5	15	15	1	1	ContentTypes	ContentTypes	/Root/System/Schema/ContentTypes
1003	1000	5	16	16	2	1	Settings	Settings	/Root/System/Settings
1004	1002	9	17	17	0	1	ContentType	$Ctd-ContentType,DisplayName	/Root/System/Schema/ContentTypes/ContentType
1005	1002	9	18	18	0	1	GenericContent	$Ctd-GenericContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent
1006	1005	9	19	19	0	1	Application	$Ctd-Application,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application
1007	1006	9	20	20	0	1	ApplicationOverride	$Ctd-ApplicationOverride,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ApplicationOverride
1008	1005	9	21	21	0	1	Folder	$Ctd-Folder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder
1009	1008	9	22	22	0	1	ContentList	$Ctd-ContentList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList
1010	1009	9	23	23	0	1	Aspect	$Ctd-Aspect,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Aspect
1011	1005	9	24	24	0	1	FieldSettingContent	$Ctd-FieldSettingContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent
1012	1011	9	25	25	0	1	BinaryFieldSetting	$Ctd-BinaryFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/BinaryFieldSetting
1013	1011	9	26	26	0	1	TextFieldSetting	$Ctd-TextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting
1014	1013	9	27	27	0	1	ShortTextFieldSetting	$Ctd-ShortTextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting
1015	1014	9	28	28	0	1	ChoiceFieldSetting	$Ctd-ChoiceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting
1016	1005	9	29	29	0	1	ContentLink	$Ctd-ContentLink,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ContentLink
1017	1011	9	30	30	0	1	NumberFieldSetting	$Ctd-NumberFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting
1018	1017	9	31	31	0	1	CurrencyFieldSetting	$Ctd-CurrencyFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting/CurrencyFieldSetting
1019	1009	9	32	32	0	1	ItemList	$Ctd-ItemList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList
1020	1019	9	33	33	0	1	CustomList	$Ctd-CustomList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/CustomList
1021	1005	9	34	34	0	1	ListItem	$Ctd-ListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem
1022	1021	9	35	35	0	1	CustomListItem	$Ctd-CustomListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/CustomListItem
1023	1011	9	36	36	0	1	DateTimeFieldSetting	$Ctd-DateTimeFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/DateTimeFieldSetting
1024	1008	9	37	37	0	1	Device	$Ctd-Device,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Device
1025	1009	9	38	38	0	1	Library	$Ctd-Library,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library
1026	1025	9	39	39	0	1	DocumentLibrary	$Ctd-DocumentLibrary,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary
1027	1008	9	40	40	0	1	Domain	$Ctd-Domain,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Domain
1028	1008	9	41	41	0	1	Domains	$Ctd-Domains,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Domains
1029	1005	9	42	42	0	1	File	$Ctd-File,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File
1030	1029	9	43	43	0	1	DynamicJsonContent	Dynamic JSON content	/Root/System/Schema/ContentTypes/GenericContent/File/DynamicJsonContent
1031	1008	9	44	44	0	1	Email	$Ctd-Email,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Email
1032	1029	9	45	45	0	1	ExecutableFile	$Ctd-ExecutableFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/ExecutableFile
1033	1006	9	46	46	0	1	ExportToCsvApplication	$Ctd-ExportToCsvApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ExportToCsvApplication
1034	1006	9	47	47	0	1	GenericODataApplication	$Ctd-GenericODataApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/GenericODataApplication
1035	1006	9	48	48	0	1	HttpHandlerApplication	$Ctd-HttpHandlerApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication
1036	1035	9	49	49	0	1	GetMetadataApplication	GetMetadataApplication	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication/GetMetadataApplication
1037	1005	9	50	50	0	1	Group	$Ctd-Group,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Group
1038	1029	9	51	51	0	1	HtmlTemplate	$Ctd-HtmlTemplate,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/HtmlTemplate
1039	1006	9	52	52	0	1	HttpStatusApplication	$Ctd-HttpStatusApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpStatusApplication
1040	1011	9	53	53	0	1	HyperLinkFieldSetting	$Ctd-HyperLinkFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/HyperLinkFieldSetting
1041	1029	9	54	54	0	1	Image	$Ctd-Image,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Image
1042	1025	9	55	55	0	1	ImageLibrary	$Ctd-ImageLibrary,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/ImageLibrary
1043	1006	9	56	56	0	1	ImgResizeApplication	$Ctd-ImgResizeApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ImgResizeApplication
1044	1029	9	57	57	0	1	Settings	$Ctd-Settings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings
1045	1044	9	58	58	0	1	IndexingSettings	$Ctd-IndexingSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/IndexingSettings
1046	1011	9	59	59	0	1	IntegerFieldSetting	$Ctd-IntegerFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/IntegerFieldSetting
1047	1044	9	60	60	0	1	LoggingSettings	$Ctd-LoggingSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/LoggingSettings
1048	1013	9	61	61	0	1	LongTextFieldSetting	$Ctd-LongTextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/LongTextFieldSetting
1049	1021	9	62	62	0	1	Memo	$Ctd-Memo,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Memo
1050	1019	9	63	63	0	1	MemoList	$Ctd-MemoList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList
1051	1011	9	64	64	0	1	NullFieldSetting	$Ctd-NullFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NullFieldSetting
1052	1008	9	65	65	0	1	OrganizationalUnit	$Ctd-OrganizationalUnit,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/OrganizationalUnit
1053	1014	9	66	66	0	1	PasswordFieldSetting	$Ctd-PasswordFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/PasswordFieldSetting
1054	1015	9	67	67	0	1	PermissionChoiceFieldSetting	$Ctd-PermissionChoiceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/PermissionChoiceFieldSetting
1055	1008	9	68	68	0	1	PortalRoot	$Ctd-PortalRoot,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/PortalRoot
1056	1044	9	69	69	0	1	PortalSettings	$Ctd-PortalSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/PortalSettings
1057	1041	9	70	70	0	1	PreviewImage	$Ctd-PreviewImage,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Image/PreviewImage
1058	1008	9	71	71	0	1	ProfileDomain	$Ctd-ProfileDomain,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ProfileDomain
1059	1008	9	72	72	0	1	Profiles	$Ctd-Profiles,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Profiles
1060	1005	9	73	73	0	1	Query	$Ctd-Query,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Query
1061	1011	9	74	74	0	1	ReferenceFieldSetting	$Ctd-ReferenceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/ReferenceFieldSetting
1062	1029	9	75	75	0	1	SystemFile	$Ctd-SystemFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile
1063	1062	9	76	76	0	1	Resource	$Ctd-Resource,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/Resource
1064	1008	9	77	77	0	1	SystemFolder	$Ctd-SystemFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder
1065	1064	9	78	78	0	1	Resources	$Ctd-Resources,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Resources
1066	1006	9	79	79	0	1	RssApplication	$Ctd-RssApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/RssApplication
1067	1008	9	80	80	0	1	RuntimeContentContainer	$Ctd-RuntimeContentContainer,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/RuntimeContentContainer
1068	1008	9	81	81	0	1	Workspace	$Ctd-Workspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace
1069	1068	9	82	82	0	1	Site	$Ctd-Site,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Site
1070	1008	9	83	83	0	1	Sites	$Ctd-Sites,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Sites
1071	1008	9	84	84	0	1	SmartFolder	$Ctd-SmartFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder
1072	1021	9	85	85	0	1	Task	$Ctd-Task,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Task
1073	1019	9	86	86	0	1	TaskList	$Ctd-TaskList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList
1074	1008	9	87	87	0	1	TrashBag	$Ctd-TrashBag,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/TrashBag
1075	1068	9	88	88	0	1	TrashBin	$Ctd-TrashBin,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TrashBin
1076	1005	9	89	89	0	1	User	$Ctd-User,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/User
1077	1068	9	90	90	0	1	UserProfile	$Ctd-UserProfile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/UserProfile
1078	1006	9	91	91	0	1	WebServiceApplication	$Ctd-WebServiceApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/WebServiceApplication
1079	1011	9	92	92	0	1	XmlFieldSetting	$Ctd-XmlFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/XmlFieldSetting
1080	1015	9	93	93	0	1	YesNoFieldSetting	$Ctd-YesNoFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/YesNoFieldSetting
1081	1003	65	94	94	0	1	Indexing.settings	""""	/Root/System/Settings/Indexing.settings
1082	1003	66	95	95	0	1	Logging.settings	""""	/Root/System/Settings/Logging.settings
1083	1003	51	96	96	0	1	MailProcessor.settings	""""	/Root/System/Settings/MailProcessor.settings
1084	1003	51	97	97	0	1	OAuth.settings	""""	/Root/System/Settings/OAuth.settings
1085	1003	67	98	98	0	1	Portal.settings	""""	/Root/System/Settings/Portal.settings
1086	1003	51	99	99	0	1	Sharing.settings	""""	/Root/System/Settings/Sharing.settings
1087	1003	51	100	100	0	1	TaskManagement.settings	""""	/Root/System/Settings/TaskManagement.settings
1088	1003	51	101	101	0	1	UserProfile.settings	""""	/Root/System/Settings/UserProfile.settings
1089	2	5	102	102	0	1	(apps)	(apps)	/Root/(apps)
1090	1089	1	103	103	0	1	ContentList	""""	/Root/(apps)/ContentList
1091	1090	11	104	104	0	1	DeleteField	$Action,DeleteField	/Root/(apps)/ContentList/DeleteField
1092	1090	11	105	105	0	1	EditField	$Action,EditField	/Root/(apps)/ContentList/EditField
1093	1090	24	106	106	0	1	ExchangeService.asmx	""""	/Root/(apps)/ContentList/ExchangeService.asmx
1094	1089	1	107	107	0	1	File	""""	/Root/(apps)/File
1095	1094	19	108	108	0	1	CheckPreviews	Check preview images	/Root/(apps)/File/CheckPreviews
1096	1094	11	109	109	0	1	EditInMicrosoftOffice	$Action,Edit-in-Microsoft-Office	/Root/(apps)/File/EditInMicrosoftOffice
1097	1094	20	110	110	0	1	ExportToPdf	$Action,ExportToPdf	/Root/(apps)/File/ExportToPdf
1098	1094	19	111	111	0	1	GetPageCount	Get page count	/Root/(apps)/File/GetPageCount
1099	1094	19	112	112	0	1	GetPreviewsFolder	Get previews folder	/Root/(apps)/File/GetPreviewsFolder
1100	1094	19	113	113	0	1	PreviewAvailable	""""	/Root/(apps)/File/PreviewAvailable
1101	1094	19	114	114	0	1	RegeneratePreviews	Regenerate preview images	/Root/(apps)/File/RegeneratePreviews
1102	1094	19	115	115	0	1	SetPageCount	Set page count	/Root/(apps)/File/SetPageCount
1103	1094	19	116	116	0	1	SetPreviewStatus	Set preview status	/Root/(apps)/File/SetPreviewStatus
1104	1094	11	117	117	250	1	UploadResume	$Action,UploadResume	/Root/(apps)/File/UploadResume
1105	1089	1	118	118	0	1	Folder	""""	/Root/(apps)/Folder
1106	1105	11	119	119	3800	1	CopyBatch	$Action,CopyBatch	/Root/(apps)/Folder/CopyBatch
1107	1105	11	120	120	3800	1	DeleteBatch	$Action,DeleteBatch	/Root/(apps)/Folder/DeleteBatch
1108	1105	18	121	121	5400	1	ExportToCsv	$Action,ExportToCsv	/Root/(apps)/Folder/ExportToCsv
1109	1105	11	122	122	3800	1	MoveBatch	$Action,MoveBatch	/Root/(apps)/Folder/MoveBatch
1110	1105	11	123	123	0	1	Upload	$Action,Upload	/Root/(apps)/Folder/Upload
1111	1089	1	124	124	0	1	GenericContent	""""	/Root/(apps)/GenericContent
1112	1111	19	125	125	0	1	AddAllowedChildTypes	""""	/Root/(apps)/GenericContent/AddAllowedChildTypes
1113	1111	19	126	126	0	1	GetRelatedPermissions	""""	/Root/(apps)/GenericContent/GetRelatedPermissions
1114	1111	19	127	127	0	1	GetSharing	""""	/Root/(apps)/GenericContent/GetSharing
1115	1111	19	128	128	0	1	GetTemplateScript	""""	/Root/(apps)/GenericContent/GetTemplateScript
1116	1111	20	129	129	0	1	HasPermission	$Action,HasPermission	/Root/(apps)/GenericContent/HasPermission
1117	1111	19	130	130	0	1	Login	""""	/Root/(apps)/GenericContent/Login
1118	1111	11	131	131	9000	1	Logout	$Action,Logout	/Root/(apps)/GenericContent/Logout
1119	1111	11	132	132	3800	1	MoveTo	$Action,MoveTo	/Root/(apps)/GenericContent/MoveTo
1120	1111	11	133	133	0	1	Publish	$Action,Publish	/Root/(apps)/GenericContent/Publish
1121	1111	19	134	134	0	1	RebuildIndex	""""	/Root/(apps)/GenericContent/RebuildIndex
1122	1111	19	135	135	0	1	RebuildIndexSubtree	""""	/Root/(apps)/GenericContent/RebuildIndexSubtree
1123	1111	19	136	136	0	1	RefreshIndexSubtree	""""	/Root/(apps)/GenericContent/RefreshIndexSubtree
1124	1111	11	137	137	0	1	Reject	$Action,Reject	/Root/(apps)/GenericContent/Reject
1125	1111	20	138	138	0	1	RemoveAllAspects	$Action,RemoveAllAspects	/Root/(apps)/GenericContent/RemoveAllAspects
1126	1111	19	139	139	0	1	GetRelatedItemsOneLevel	""""	/Root/(apps)/GenericContent/GetRelatedItemsOneLevel
1127	1111	20	140	140	0	1	RemoveAllFields	$Action,RemoveAllFields	/Root/(apps)/GenericContent/RemoveAllFields
1128	1111	20	141	141	0	1	RemoveAspects	$Action,RemoveAspects	/Root/(apps)/GenericContent/RemoveAspects
1129	1111	20	142	142	0	1	RemoveFields	$Action,RemoveFields	/Root/(apps)/GenericContent/RemoveFields
1130	1111	19	143	143	0	1	RemoveSharing	""""	/Root/(apps)/GenericContent/RemoveSharing
1131	1111	19	144	144	0	1	ResetRecentIndexingActivities	""""	/Root/(apps)/GenericContent/ResetRecentIndexingActivities
1132	1111	11	145	145	0	1	RestoreVersion	$Action,RestoreVersion	/Root/(apps)/GenericContent/RestoreVersion
1133	1111	20	146	146	0	1	RetrieveFields	$Action,RetrieveFields	/Root/(apps)/GenericContent/RetrieveFields
1134	1111	23	147	147	0	1	Rss	$Action,Rss	/Root/(apps)/GenericContent/Rss
1135	1111	19	148	148	0	1	SaveQuery	""""	/Root/(apps)/GenericContent/SaveQuery
1136	1111	11	149	149	0	1	SetPermissions	$Action,SetPermissions	/Root/(apps)/GenericContent/SetPermissions
1137	1111	19	150	150	0	1	Share	""""	/Root/(apps)/GenericContent/Share
1138	1111	19	151	151	0	1	StartBlobUpload	""""	/Root/(apps)/GenericContent/StartBlobUpload
1139	1111	19	152	152	0	1	StartBlobUploadToParent	""""	/Root/(apps)/GenericContent/StartBlobUploadToParent
1140	1111	19	153	153	0	1	TakeLockOver	""""	/Root/(apps)/GenericContent/TakeLockOver
1141	1111	19	154	154	0	1	RemoveAllowedChildTypes	""""	/Root/(apps)/GenericContent/RemoveAllowedChildTypes
1142	1111	19	155	155	0	1	GetRelatedItems	""""	/Root/(apps)/GenericContent/GetRelatedItems
1143	1111	19	156	156	0	1	GetRelatedIdentitiesByPermissions	""""	/Root/(apps)/GenericContent/GetRelatedIdentitiesByPermissions
1144	1111	19	157	157	0	1	GetRelatedIdentities	""""	/Root/(apps)/GenericContent/GetRelatedIdentities
1145	1111	20	158	158	0	1	AddAspects	$Action,AddAspects	/Root/(apps)/GenericContent/AddAspects
1146	1111	20	159	159	0	1	AddFields	$Action,AddFields	/Root/(apps)/GenericContent/AddFields
1147	1111	19	160	160	0	1	Ancestors	""""	/Root/(apps)/GenericContent/Ancestors
1148	1111	11	161	161	0	1	Approve	$Action,Approve	/Root/(apps)/GenericContent/Approve
1149	1111	11	162	162	0	1	CheckIn	$Action,CheckIn	/Root/(apps)/GenericContent/CheckIn
1150	1111	19	163	163	0	1	CheckIndexIntegrity	""""	/Root/(apps)/GenericContent/CheckIndexIntegrity
1151	1111	11	164	164	0	1	CheckOut	$Action,CheckOut	/Root/(apps)/GenericContent/CheckOut
1152	1111	19	165	165	0	1	CheckSecurityConsistency	""""	/Root/(apps)/GenericContent/CheckSecurityConsistency
1153	1111	11	166	166	3800	1	CopyTo	$Action,CopyTo	/Root/(apps)/GenericContent/CopyTo
1154	1111	11	167	167	6000	1	Delete	$Action,Delete	/Root/(apps)/GenericContent/Delete
1155	1111	19	168	168	0	1	DocumentPreviewFinalizer	""""	/Root/(apps)/GenericContent/DocumentPreviewFinalizer
1156	1111	19	169	169	0	1	FinalizeBlobUpload	""""	/Root/(apps)/GenericContent/FinalizeBlobUpload
1157	1111	19	170	170	0	1	FinalizeContent	""""	/Root/(apps)/GenericContent/FinalizeContent
1158	1111	11	171	171	0	1	ForceUndoCheckOut	$Action,ForceUndoCheckOut	/Root/(apps)/GenericContent/ForceUndoCheckOut
1159	1111	19	172	172	0	1	GetAllContentTypes	""""	/Root/(apps)/GenericContent/GetAllContentTypes
1160	1111	19	173	173	0	1	GetAllowedChildTypesFromCTD	""""	/Root/(apps)/GenericContent/GetAllowedChildTypesFromCTD
1161	1111	19	174	174	0	1	GetAllowedUsers	""""	/Root/(apps)/GenericContent/GetAllowedUsers
1162	1111	19	175	175	0	1	GetBinaryToken	""""	/Root/(apps)/GenericContent/GetBinaryToken
1163	1111	19	176	176	0	1	GetChildrenPermissionInfo	""""	/Root/(apps)/GenericContent/GetChildrenPermissionInfo
1164	1111	19	177	177	0	1	GetExistingPreviewImages	$Action,GetExistingPreviewImages	/Root/(apps)/GenericContent/GetExistingPreviewImages
1165	1111	19	178	178	0	1	GetNameFromDisplayName	""""	/Root/(apps)/GenericContent/GetNameFromDisplayName
1166	1111	19	179	179	0	1	GetPermissionInfo	""""	/Root/(apps)/GenericContent/GetPermissionInfo
1167	1111	19	180	180	0	1	GetPermissionOverview	""""	/Root/(apps)/GenericContent/GetPermissionOverview
1168	1111	20	181	181	0	1	GetPermissions	$Action,GetPermissions	/Root/(apps)/GenericContent/GetPermissions
1169	1111	19	182	182	0	1	GetPreviewImages	$Action,GetPreviewImages	/Root/(apps)/GenericContent/GetPreviewImages
1170	1111	19	183	183	0	1	GetQueries	""""	/Root/(apps)/GenericContent/GetQueries
1171	1111	19	184	184	0	1	GetQueryBuilderMetadata	""""	/Root/(apps)/GenericContent/GetQueryBuilderMetadata
1172	1111	19	185	185	0	1	GetRecentIndexingActivities	""""	/Root/(apps)/GenericContent/GetRecentIndexingActivities
1173	1111	19	186	186	0	1	GetRecentSecurityActivities	""""	/Root/(apps)/GenericContent/GetRecentSecurityActivities
1174	1111	19	187	187	0	1	TakeOwnership	""""	/Root/(apps)/GenericContent/TakeOwnership
1175	1111	11	188	188	0	1	UndoCheckOut	$Action,UndoCheckOut	/Root/(apps)/GenericContent/UndoCheckOut
1176	1089	1	189	189	0	1	Group	""""	/Root/(apps)/Group
1177	1176	19	190	190	0	1	AddMembers	Add members	/Root/(apps)/Group/AddMembers
1178	1176	19	191	191	0	1	GetParentGroups	""""	/Root/(apps)/Group/GetParentGroups
1179	1176	19	192	192	0	1	RemoveMembers	Remove members	/Root/(apps)/Group/RemoveMembers
1180	1089	1	193	193	0	1	Image	""""	/Root/(apps)/Image
1181	1180	22	194	194	0	1	Thumbnail	""""	/Root/(apps)/Image/Thumbnail
1182	1089	1	195	195	0	1	Link	""""	/Root/(apps)/Link
1183	1182	11	196	196	0	1	Browse	$Action,OpenLink	/Root/(apps)/Link/Browse
1184	1089	1	197	197	0	1	PortalRoot	""""	/Root/(apps)/PortalRoot
1185	1184	19	198	198	0	1	GetSchema	""""	/Root/(apps)/PortalRoot/GetSchema
1186	1184	19	199	199	0	1	GetVersionInfo	""""	/Root/(apps)/PortalRoot/GetVersionInfo
1187	1089	1	200	200	0	1	PreviewImage	""""	/Root/(apps)/PreviewImage
1188	1187	19	201	201	0	1	SetInitialPreviewProperties	Set initial preview properties	/Root/(apps)/PreviewImage/SetInitialPreviewProperties
1189	1089	1	202	202	0	1	This	""""	/Root/(apps)/This
1190	1189	19	203	203	0	1	Decrypt	""""	/Root/(apps)/This/Decrypt
1191	1189	19	204	204	0	1	Encrypt	""""	/Root/(apps)/This/Encrypt
1192	1089	1	205	205	0	1	User	""""	/Root/(apps)/User
1193	1192	19	206	206	0	1	GetParentGroups	""""	/Root/(apps)/User/GetParentGroups
1194	1192	19	207	207	0	1	Profile	""""	/Root/(apps)/User/Profile
1195	1	50	208	208	0	0	Admin.png	Admin.png	/Root/IMS/BuiltIn/Portal/Admin/Admin.png
1196	5	2	209	209	0	0	ContentExplorers	ContentExplorers	/Root/IMS/BuiltIn/Portal/ContentExplorers
1197	5	2	210	210	0	0	Developers	Developers	/Root/IMS/BuiltIn/Portal/Developers
1198	5	2	211	211	0	0	Editors	Editors	/Root/IMS/BuiltIn/Portal/Editors
1199	5	2	212	212	0	0	HR	HR	/Root/IMS/BuiltIn/Portal/HR
1200	5	2	213	213	0	0	IdentifiedUsers	IdentifiedUsers	/Root/IMS/BuiltIn/Portal/IdentifiedUsers
1201	5	2	214	214	0	0	PageEditors	PageEditors	/Root/IMS/BuiltIn/Portal/PageEditors
1202	5	2	215	215	0	0	PRCViewers	PRCViewers	/Root/IMS/BuiltIn/Portal/PRCViewers
1203	5	2	216	216	0	0	RegisteredUsers	RegisteredUsers	/Root/IMS/BuiltIn/Portal/RegisteredUsers
1204	5	3	217	217	0	0	VirtualADUser	""""	/Root/IMS/BuiltIn/Portal/VirtualADUser
1205	2	54	218	218	0	1	Localization	""""	/Root/Localization
1206	1205	68	219	219	0	1	Content.xml	""""	/Root/Localization/Content.xml
1207	1205	68	220	220	0	1	CtdResourcesAB.xml	CtdResourcesAB.xml	/Root/Localization/CtdResourcesAB.xml
1208	1205	68	221	221	0	1	CtdResourcesCD.xml	CtdResourcesCD.xml	/Root/Localization/CtdResourcesCD.xml
1209	1205	68	222	222	0	1	CtdResourcesEF.xml	CtdResourcesEF.xml	/Root/Localization/CtdResourcesEF.xml
1210	1205	68	223	223	0	1	CtdResourcesGH.xml	CtdResourcesGH.xml	/Root/Localization/CtdResourcesGH.xml
1211	1205	68	224	224	0	1	CtdResourcesIJK.xml	CtdResourcesIJK.xml	/Root/Localization/CtdResourcesIJK.xml
1212	1205	68	225	225	0	1	CtdResourcesLM.xml	CtdResourcesLM.xml	/Root/Localization/CtdResourcesLM.xml
1213	1205	68	226	226	0	1	CtdResourcesNOP.xml	CtdResourcesNOP.xml	/Root/Localization/CtdResourcesNOP.xml
1214	1205	68	227	227	0	1	CtdResourcesQ.xml	CtdResourcesQ.xml	/Root/Localization/CtdResourcesQ.xml
1215	1205	68	228	228	0	1	CtdResourcesRS.xml	CtdResourcesRS.xml	/Root/Localization/CtdResourcesRS.xml
1216	1205	68	229	229	0	1	CtdResourcesTZ.xml	CtdResourcesTZ.xml	/Root/Localization/CtdResourcesTZ.xml
1217	1205	68	230	230	0	1	Exceptions.xml	""""	/Root/Localization/Exceptions.xml
1218	1205	68	231	231	0	1	Sharing.xml	""""	/Root/Localization/Sharing.xml
1219	1205	68	232	232	0	1	Trash.xml	""""	/Root/Localization/Trash.xml
1220	1000	5	233	233	0	1	ErrorMessages	""""	/Root/System/ErrorMessages
1221	1220	5	234	234	0	1	Default	""""	/Root/System/ErrorMessages/Default
1222	1221	15	235	235	0	1	Global.html	""""	/Root/System/ErrorMessages/Default/Global.html
1223	1221	15	236	236	0	1	UserGlobal.html	""""	/Root/System/ErrorMessages/Default/UserGlobal.html
1224	1001	5	237	237	0	1	Metadata	Metadata	/Root/System/Schema/Metadata
1225	1224	5	238	238	0	1	TypeScript	TypeScript	/Root/System/Schema/Metadata/TypeScript
1226	1225	53	239	239	0	1	complextypes.ts	""""	/Root/System/Schema/Metadata/TypeScript/complextypes.ts
1227	1225	53	240	240	0	1	contenttypes.ts	""""	/Root/System/Schema/Metadata/TypeScript/contenttypes.ts
1228	1225	53	241	241	0	1	enums.ts	""""	/Root/System/Schema/Metadata/TypeScript/enums.ts
1229	1225	53	242	242	0	1	fieldsettings.ts	""""	/Root/System/Schema/Metadata/TypeScript/fieldsettings.ts
1230	1225	53	243	243	0	1	meta.zip	""""	/Root/System/Schema/Metadata/TypeScript/meta.zip
1231	1225	53	244	244	0	1	resources.ts	""""	/Root/System/Schema/Metadata/TypeScript/resources.ts
1232	1225	53	245	245	0	1	schemas.ts	""""	/Root/System/Schema/Metadata/TypeScript/schemas.ts
1233	1000	5	246	246	0	1	WebRoot	""""	/Root/System/WebRoot
1234	1233	48	247	247	0	1	binaryhandler.ashx	binaryhandler.ashx	/Root/System/WebRoot/binaryhandler.ashx
1235	1233	1	248	248	0	1	DWS	DWS	/Root/System/WebRoot/DWS
1236	1235	48	249	249	0	1	Dws.asmx	""""	/Root/System/WebRoot/DWS/Dws.asmx
1237	1235	48	250	250	0	1	Fpp.ashx	""""	/Root/System/WebRoot/DWS/Fpp.ashx
1238	1235	48	251	251	0	1	Lists.asmx	""""	/Root/System/WebRoot/DWS/Lists.asmx
1239	1235	48	252	252	0	1	owssvr.aspx	""""	/Root/System/WebRoot/DWS/owssvr.aspx
1240	1235	48	253	253	0	1	Versions.asmx	""""	/Root/System/WebRoot/DWS/Versions.asmx
1241	1235	48	254	254	0	1	Webs.asmx	""""	/Root/System/WebRoot/DWS/Webs.asmx
1242	1233	48	255	255	0	1	vsshandler.ashx	vsshandler.ashx	/Root/System/WebRoot/vsshandler.ashx
1243	2	59	256	256	0	0	Trash	""""	/Root/Trash
1244	1243	5	257	257	0	1	(apps)	""""	/Root/Trash/(apps)
1245	1244	1	258	258	0	1	TrashBag	""""	/Root/Trash/(apps)/TrashBag
1246	1245	11	259	259	0	1	Restore	$Action,Restore	/Root/Trash/(apps)/TrashBag/Restore
";
        #endregion

        #region BINARYPROPERTIES

        // SELECT * FROM BinaryProperties

        private static string _prototypeBinaryProperties = @"BinaryPropertyId	VersionId	PropertyTypeId	FileId
1	17	1	1
2	18	1	2
3	19	1	3
4	20	1	4
5	21	1	5
6	22	1	6
7	23	1	7
8	24	1	8
9	25	1	9
10	26	1	10
11	27	1	11
12	28	1	12
13	29	1	13
14	30	1	14
15	31	1	15
16	32	1	16
17	33	1	17
18	34	1	18
19	35	1	19
20	36	1	20
21	37	1	21
22	38	1	22
23	39	1	23
24	40	1	24
25	41	1	25
26	42	1	26
27	43	1	27
28	44	1	28
29	45	1	29
30	46	1	30
31	47	1	31
32	48	1	32
33	49	1	33
34	50	1	34
35	51	1	35
36	52	1	36
37	53	1	37
38	54	1	38
39	55	1	39
40	56	1	40
41	57	1	41
42	58	1	42
43	59	1	43
44	60	1	44
45	61	1	45
46	62	1	46
47	63	1	47
48	64	1	48
49	65	1	49
50	66	1	50
51	67	1	51
52	68	1	52
53	69	1	53
54	70	1	54
55	71	1	55
56	72	1	56
57	73	1	57
58	74	1	58
59	75	1	59
60	76	1	60
61	77	1	61
62	78	1	62
63	79	1	63
64	80	1	64
65	81	1	65
66	82	1	66
67	83	1	67
68	84	1	68
69	85	1	69
70	86	1	70
71	87	1	71
72	88	1	72
73	89	1	73
74	90	1	74
75	91	1	75
76	92	1	76
77	93	1	77
78	94	1	78
79	95	1	79
80	96	1	80
81	97	1	81
82	98	1	82
83	99	1	83
84	100	1	84
85	101	1	85
86	106	1	86
87	208	1	87
88	219	1	88
89	220	1	89
90	221	1	90
91	222	1	91
92	223	1	92
93	224	1	93
94	225	1	94
95	226	1	95
96	227	1	96
97	228	1	97
98	229	1	98
99	230	1	99
100	231	1	100
101	232	1	101
102	235	1	102
103	236	1	103
104	247	1	104
105	249	1	105
106	250	1	106
107	251	1	107
108	252	1	108
109	253	1	109
110	254	1	110
111	255	1	111
";
        #endregion

        #region FILES

        // SELECT FileId, ContentType, FileNameWithoutExtension, Extension, Size FROM Files

        private static string _prototypeFiles = @"FileId	ContentType	FileNameWithoutExtension	Extension	Size
1	text/xml	ContentType	.ContentType	17495
2	text/xml	GenericContent	.ContentType	31386
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
26	text/xml	File	.ContentType	5107
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
73	text/xml	User	.ContentType	13605
74	text/xml	UserProfile	.ContentType	2111
75	text/xml	WebServiceApplication	.ContentType	696
76	text/xml	XmlFieldSetting	.ContentType	342
77	text/xml	YesNoFieldSetting	.ContentType	347
78	application/octet-stream	Indexing	.settings	74
79	application/octet-stream	Logging	.settings	347
80	application/octet-stream	MailProcessor	.settings	321
81	application/octet-stream	OAuth	.settings	43
82	application/octet-stream	Portal	.settings	717
83	application/octet-stream	Sharing	.settings	37
84	application/octet-stream	TaskManagement	.settings	121
85	application/octet-stream	UserProfile	.settings	65
86	application/octet-stream	ExchangeService	.asmx	90
87	image/png	Admin	.png	731
88	text/xml	Content	.xml	19797
89	text/xml	CtdResourcesAB	.xml	25460
90	text/xml	CtdResourcesCD	.xml	30391
91	text/xml	CtdResourcesEF	.xml	12811
92	text/xml	CtdResourcesGH	.xml	51097
93	text/xml	CtdResourcesIJK	.xml	22739
94	text/xml	CtdResourcesLM	.xml	7361
95	text/xml	CtdResourcesNOP	.xml	5966
96	text/xml	CtdResourcesQ	.xml	2624
97	text/xml	CtdResourcesRS	.xml	20272
98	text/xml	CtdResourcesTZ	.xml	39392
99	text/xml	Exceptions	.xml	19617
100	text/xml	Sharing	.xml	1545
101	text/xml	Trash	.xml	6728
102	text/html	Global	.html	15642
103	text/html	UserGlobal	.html	15125
104	application/octet-stream	binaryhandler	.ashx	83
105	application/octet-stream	Dws	.asmx	75
106	application/octet-stream	Fpp	.ashx	75
107	application/octet-stream	Lists	.asmx	77
108	text/asp	owssvr	.aspx	9520
109	application/octet-stream	Versions	.asmx	80
110	application/octet-stream	Webs	.asmx	76
111	application/octet-stream	vsshandler	.ashx	90
";
        #endregion

        #region TEXTPROPERTIES

        private static string _prototypeTextProperties = @"TextPropertyNVarcharId	VersionId	PropertyTypeId	Value
1	108	71	bool generateMissing
2	110	3	
3	112	71	bool empty
4	113	71	int page
5	115	71	int pageCount
6	116	71	SenseNet.Preview.PreviewStatus status
7	119	3	
8	120	3	
9	121	3	
10	122	3	
11	125	71	string[] contentTypes
12	126	71	        string level,        bool explicitOnly,        string member,        string[] includedTypes      
13	128	71	string skin, string category
14	129	3	
15	130	71	        string username,      string password      
16	131	3	
17	132	3	
18	134	71	     bool recursive,      SenseNet.ContentRepository.Search.Indexing.IndexRebuildLevel rebuildLevel      
19	138	3	
20	139	71	        string level,        string member,        string[] permissions      
21	140	3	
22	141	3	
23	142	3	
24	143	71	        string id      
25	144	3	
26	144	71	
27	146	3	
28	147	3	
29	148	71	        string query,        string displayName,        string queryType      
30	150	71	        string token, SenseNet.ContentRepository.Sharing.SharingLevel level,         SenseNet.ContentRepository.Sharing.SharingMode mode, bool sendNotification      
31	151	71	long fullSize, string fieldName
32	152	71	string name, string contentType, long fullSize, string fieldName
33	153	71	        string user      
34	154	71	string[] contentTypes
35	155	71	        string level,        bool explicitOnly,        string member,        string[] permissions,      
36	156	71	        string level,        string kind,        string[] permissions      
37	157	71	        string level,        string kind      
38	158	3	
39	159	3	
40	163	71	        bool recurse      
41	165	3	
42	165	71	
43	166	3	
44	167	3	
45	168	71	SenseNet.TaskManagement.Core.SnTaskResult result
46	169	71	string token, long fullSize, string fieldName, string fileName
47	172	3	
48	172	71	
49	173	3	
50	173	71	
51	174	3	
52	174	71	        string[] permissions      
53	175	71	string fieldName
54	176	71	        string identity      
55	178	71	        string displayName      
56	179	71	        string identity      
57	180	71	        string identity      
58	181	3	
59	183	71	        bool onlyPublic      
60	184	71	
61	185	3	
62	185	71	
63	186	3	
64	186	71	
65	187	3	
66	187	71	string userOrGroup
67	190	71	int[] contentIds
68	191	3	
69	191	71	        bool directOnly      
70	192	71	int[] contentIds
71	194	3	
72	196	3	
73	198	71	string contentTypeName
74	199	71	
75	203	71	string text
76	204	71	string text
77	206	3	
78	206	71	        bool directOnly      
79	207	3	
80	207	71	string back
81	1	130	<?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">    <OldPasswordData>      <ModificationDate>2018-11-13T09:06:21.6430302Z</ModificationDate>      <Hash>$2a$10$bSsyM8JgVSHTvIWfy/qQPOIZj9jMvAVPP6S0BxGTLzwhShfLRWQVW</Hash>    </OldPasswordData>  </ArrayOfOldPasswordData>
82	209	3	
83	210	3	
84	211	3	
85	212	3	
86	213	3	
87	11	3	Members of this group are able to perform administrative tasks in the Content Repository - e.g. importing the creation date of content.
88	214	3	
89	215	3	
90	216	3	
91	10	130	<?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">    <OldPasswordData>      <ModificationDate>2018-11-13T09:06:22.2011492Z</ModificationDate>      <Hash>$2a$10$PgXRiF4q4LFrzvWwooRsfufypvJ6haMRIqhpOxSGqz5vT93YZjQMS</Hash>    </OldPasswordData>  </ArrayOfOldPasswordData>
92	12	130	<?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">    <OldPasswordData>      <ModificationDate>2018-11-13T09:06:22.4175706Z</ModificationDate>      <Hash>$2a$10$t3ieq26ODXo46vdN/LAfdug34jMiU9sqmjlcmpXBHL9F57k014q3m</Hash>    </OldPasswordData>  </ArrayOfOldPasswordData>
93	217	130	<?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" />
94	6	130	<?xml version=""1.0"" encoding=""utf-16""?>  <ArrayOfOldPasswordData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" />
95	14	8	SystemFolder
96	15	8	ContentType
97	237	8	GetMetadataApplication SystemFolder Folder
98	238	8	GetMetadataApplication SystemFolder Folder
99	255	3	Http handler for serving Lucene index file paths. This content can be invoked only from the local machine.
";
        #endregion

        #region FLATPROPERTIES STRING

        // SELECT Id, VersionId, Page, nvarchar_1, nvarchar_2, nvarchar_3, nvarchar_4, nvarchar_5, nvarchar_6, nvarchar_7, nvarchar_8, nvarchar_9, nvarchar_10, nvarchar_11, nvarchar_12, nvarchar_13, nvarchar_14, nvarchar_15, nvarchar_16, nvarchar_17, nvarchar_18, nvarchar_19, nvarchar_20, nvarchar_21, nvarchar_22, nvarchar_23, nvarchar_24, nvarchar_25, nvarchar_26, nvarchar_27, nvarchar_28, nvarchar_29, nvarchar_30, nvarchar_31, nvarchar_32, nvarchar_33, nvarchar_34, nvarchar_35, nvarchar_36, nvarchar_37, nvarchar_38, nvarchar_39, nvarchar_40, nvarchar_41, nvarchar_42, nvarchar_43, nvarchar_44, nvarchar_45, nvarchar_46, nvarchar_47, nvarchar_48, nvarchar_49, nvarchar_50, nvarchar_51, nvarchar_52, nvarchar_53, nvarchar_54, nvarchar_55, nvarchar_56, nvarchar_57, nvarchar_58, nvarchar_59, nvarchar_60, nvarchar_61, nvarchar_62, nvarchar_63, nvarchar_64, nvarchar_65, nvarchar_66, nvarchar_67, nvarchar_68, nvarchar_69, nvarchar_70, nvarchar_71, nvarchar_72, nvarchar_73, nvarchar_74, nvarchar_75, nvarchar_76, nvarchar_77, nvarchar_78, nvarchar_79, nvarchar_80 FROM FlatProperties

        private static string _prototypeFlatPropertiesNvarchar = @"Id	VersionId	Page	nvarchar_1	nvarchar_2	nvarchar_3	nvarchar_4	nvarchar_5	nvarchar_6	nvarchar_7	nvarchar_8	nvarchar_9	nvarchar_10	nvarchar_11	nvarchar_12	nvarchar_13	nvarchar_14	nvarchar_15	nvarchar_16	nvarchar_17	nvarchar_18	nvarchar_19	nvarchar_20	nvarchar_21	nvarchar_22	nvarchar_23	nvarchar_24	nvarchar_25	nvarchar_26	nvarchar_27	nvarchar_28	nvarchar_29	nvarchar_30	nvarchar_31	nvarchar_32	nvarchar_33	nvarchar_34	nvarchar_35	nvarchar_36	nvarchar_37	nvarchar_38	nvarchar_39	nvarchar_40	nvarchar_41	nvarchar_42	nvarchar_43	nvarchar_44	nvarchar_45	nvarchar_46	nvarchar_47	nvarchar_48	nvarchar_49	nvarchar_50	nvarchar_51	nvarchar_52	nvarchar_53	nvarchar_54	nvarchar_55	nvarchar_56	nvarchar_57	nvarchar_58	nvarchar_59	nvarchar_60	nvarchar_61	nvarchar_62	nvarchar_63	nvarchar_64	nvarchar_65	nvarchar_66	nvarchar_67	nvarchar_68	nvarchar_69	nvarchar_70	nvarchar_71	nvarchar_72	nvarchar_73	nvarchar_74	nvarchar_75	nvarchar_76	nvarchar_77	nvarchar_78	nvarchar_79	nvarchar_80
1	94	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
2	95	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
3	96	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
4	97	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
5	98	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
6	99	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
7	100	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
8	101	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
9	102	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
10	103	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
11	104	0	NULL	DeleteField	NULL	DeleteFieldAction	NULL	_________________________________________________________***___*	NULL	Nondefined	NULL	NULL	delete	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
12	105	0	NULL	EditField	NULL	EditFieldAction	NULL	_________________________________________________________***___*	NULL	Nondefined	NULL	NULL	edit	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
13	106	0	NULL	ExchangeService	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	application	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
14	107	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
15	108	0	NULL	CheckPreviews	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	CheckPreviews	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
16	109	0	NULL	EditInMicrosoftOffice	ListItem	WebdavOpenAction	NULL	_________________________________________________________***___*	NULL	Nondefined	NULL	NULL	application	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
17	110	0	NULL	ExportToPdf	ListItem;DocumentDetails	ExportToPdfAction		NULL	Default	Nondefined	NULL	NULL	acrobat	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
18	111	0	NULL	GetPageCount	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	GetPageCount	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
19	112	0	NULL	GetPreviewsFolder	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	GetPreviewsFolder	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
20	113	0	NULL	PreviewAvailable	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	PreviewAvailable	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
21	114	0	NULL	RegeneratePreviews	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	RegeneratePreviews	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
22	115	0	NULL	SetPageCount	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	SetPageCount	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
23	116	0	NULL	SetPreviewStatus	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	SetPreviewStatus	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
24	117	0	NULL	UploadResume	ListItem	UploadResumeAction	NULL	_________________________________________________________**_____	NULL	Nondefined	NULL	NULL	upload	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
25	118	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
26	119	0	NULL	CopyBatch		CopyBatchAction		NULL	Default	Nondefined			copy	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
27	120	0	NULL	DeleteBatch	GridToolbar	DeleteBatchAction		NULL	Default	Nondefined			delete	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
28	121	0	NULL	ExportToCsv	ListActions;ExploreActions			NULL	Default	Nondefined			download	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
29	122	0	NULL	MoveBatch	GridToolbar	MoveBatchAction		NULL	Default	Nondefined			move	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
30	123	0	NULL	Upload	NULL	UploadAction	NULL	______________________________________________________*____*___*	NULL	Nondefined	NULL	NULL	upload	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
31	124	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
32	125	0	NULL	AddAllowedChildTypes	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.GenericContent	AddAllowedChildTypes	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
33	126	0	NULL	GetRelatedPermissions	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetRelatedPermissions	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
34	127	0	NULL	GetSharing	NULL	NULL	NULL	________________________________________________*________*______	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Sharing.SharingActions	GetSharing	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
35	128	0	NULL	GetTemplateScript	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Portal.UI.HtmlTemplate	GetTemplateScript	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
36	129	0	NULL	HasPermission	NULL	HasPermissionAction		_________________________________________________*______________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
37	130	0	NULL	Login	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Portal.Virtualization.AuthenticationHelper	Login	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
38	131	0	NULL	Logout	UserActions	LogoutAction	NULL	NULL	False	Nondefined	NULL	NULL	logout	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
39	132	0	NULL	MoveTo	ListItem;ExploreActions;ManageViewsListItem	MoveToAction		_________________________________________________________**_____	Default	Nondefined			move	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
40	133	0	NULL	Publish	ListItem;ExploreActions;SimpleApprovableListItem	PublishAction	NULL	________________________________________________________****___*	NULL	Nondefined	NULL	NULL	publish	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
41	134	0	NULL	RebuildIndex	NULL	NULL	NULL	_________________________________________________________*______	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Content+Operations	RebuildIndex	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
42	135	0	NULL	RebuildIndexSubtree	NULL	NULL	NULL	_________________________________________________________*______	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Content+Operations	RebuildIndexSubtree	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
43	136	0	NULL	RefreshIndexSubtree	NULL	NULL	NULL	_________________________________________________________*______	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Content+Operations	RefreshIndexSubtree	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
44	137	0	NULL	Reject	NULL	RejectAction	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
45	138	0	NULL	RemoveAllAspects	NULL	RemoveAllAspectsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
46	139	0	NULL	GetRelatedItemsOneLevel	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetRelatedItemsOneLevel	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
47	140	0	NULL	RemoveAllFields	NULL	RemoveAllFieldsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
48	141	0	NULL	RemoveAspects	NULL	RemoveAspectsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
49	142	0	NULL	RemoveFields	NULL	RemoveFieldsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
50	143	0	NULL	RemoveSharing	NULL	NULL	NULL	________________________________________________*________*______	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Sharing.SharingActions	RemoveSharing	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
51	144	0	NULL	ResetRecentIndexingActivities	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	ResetRecentIndexingActivities	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
52	145	0	NULL	RestoreVersion	NULL	RestoreVersionAction	NULL	___________________________________________________*_____*______	NULL	Nondefined	NULL	NULL	restoreversion	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
53	146	0	NULL	RetrieveFields	NULL	RetrieveFieldsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
54	147	0	NULL	Rss	ListActions			NULL	Default	Nondefined			rss	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
55	148	0	NULL	SaveQuery	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Portal.QueryBuilder	SaveQuery	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
56	149	0	NULL	SetPermissions	WorkspaceActions;ListItem;ExploreActions	SetPermissionsAction	NULL	________________________________________________**_________*____	NULL	Nondefined	NULL	NULL	security	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
57	150	0	NULL	Share	NULL	NULL	NULL	________________________________________________*________*______	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Sharing.SharingActions	Share	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
58	151	0	NULL	StartBlobUpload	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ApplicationModel.UploadAction	StartBlobUpload	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
59	152	0	NULL	StartBlobUploadToParent	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ApplicationModel.UploadAction	StartBlobUploadToParent	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
60	153	0	NULL	TakeLockOver	NULL	NULL	NULL	_______________________________________________________*________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	TakeLockOver	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
61	154	0	NULL	RemoveAllowedChildTypes	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.GenericContent	RemoveAllowedChildTypes	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
62	155	0	NULL	GetRelatedItems	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetRelatedItems	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
63	156	0	NULL	GetRelatedIdentitiesByPermissions	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetRelatedIdentities	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
64	157	0	NULL	GetRelatedIdentities	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetRelatedIdentities	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
65	158	0	NULL	AddAspects	NULL	AddAspectsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
66	159	0	NULL	AddFields	NULL	AddFieldsAction	NULL	______________________________________________*_________________	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
67	160	0	NULL	Ancestors	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	Ancestors	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
68	161	0	NULL	Approve	ListItem;ExploreActions;SimpleApprovableListItem	ApproveAction	NULL	_____________________________________________________*___***___*	NULL	Nondefined	NULL	NULL	approve	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
69	162	0	NULL	CheckIn	ListItem;ExploreActions;SimpleApprovableListItem	CheckInAction	NULL	_________________________________________________________***___*	NULL	Nondefined	NULL	NULL	checkin	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
70	163	0	NULL	CheckIndexIntegrity	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Search.Indexing.IntegrityChecker	CheckIndexIntegrity	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
71	164	0	NULL	CheckOut	ListItem;ExploreActions	CheckOutAction	NULL	_________________________________________________________***___*	NULL	Nondefined	NULL	NULL	checkout	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
72	165	0	NULL	CheckSecurityConsistency		NULL		NULL	Default	Nondefined			NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	CheckSecurityConsistency	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
73	166	0	NULL	CopyTo	ListItem;ExploreActions;ManageViewsListItem	CopyToAction		_________________________________________________________*______	Default	Nondefined			copy	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
74	167	0	NULL	Delete	WorkspaceActions;ListItem;ExploreActions;ListActions;ManageViewsListItem;SimpleListItem;SimpleApprovableListItem;ReadOnlyListItem;DocumentDetails	DeleteAction		____________________________________________________*___________	Default	Nondefined			delete	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
75	168	0	NULL	DocumentPreviewFinalizer	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	DocumentPreviewFinalizer	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
76	169	0	NULL	FinalizeBlobUpload	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ApplicationModel.UploadAction	FinalizeBlobUpload	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
77	170	0	NULL	FinalizeContent	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ApplicationModel.UploadAction	FinalizeContent	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
78	171	0	NULL	ForceUndoCheckOut	ListItem;ExploreActions	ForceUndoCheckOutAction	NULL	_______________________________________________________*_***___*	NULL	Nondefined	NULL	NULL	undocheckout	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
79	172	0	NULL	GetAllContentTypes		NULL		NULL	Default	Nondefined			NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	GetListOfAllContentTypes	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
80	173	0	NULL	GetAllowedChildTypesFromCTD		NULL		NULL	Default	Nondefined			NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	GetAllowedChildTypesFromCTD	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
81	174	0	NULL	GetAllowedUsers	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetAllowedUsers	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
82	175	0	NULL	GetBinaryToken	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ApplicationModel.UploadAction	GetBinaryToken	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
83	176	0	NULL	GetChildrenPermissionInfo	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetChildrenPermissionInfo	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
84	177	0	NULL	GetExistingPreviewImages	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	GetExistingPreviewImagesForOData	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
85	178	0	NULL	GetNameFromDisplayName	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.ContentNamingProvider	GetNameFromDisplayName	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
86	179	0	NULL	GetPermissionInfo	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetPermissionInfo	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
87	180	0	NULL	GetPermissionOverview	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Portal.PermissionQuery	GetPermissionOverview	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
88	181	0	NULL	GetPermissions	NULL	GetPermissionsAction		NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
89	182	0	NULL	GetPreviewImages	NULL	NULL	NULL	______________________________________________________________*_	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	GetPreviewImagesForOData	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
90	183	0	NULL	GetQueries	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Portal.QueryBuilder	GetQueries	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
91	184	0	NULL	GetQueryBuilderMetadata	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Portal.QueryBuilder	GetMetadata	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
92	185	0	NULL	GetRecentIndexingActivities	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	GetRecentIndexingActivities	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
93	186	0	NULL	GetRecentSecurityActivities	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	GetRecentSecurityActivities	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
94	187	0	NULL	TakeOwnership		NULL		_____________________________________________*__________________	Default	Nondefined			NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.RepositoryTools	TakeOwnership	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
95	188	0	NULL	UndoCheckOut	ListItem;ExploreActions	UndoCheckOutAction	NULL	_________________________________________________________***___*	NULL	Nondefined	NULL	NULL	undocheckout	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
96	189	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
97	190	0	NULL	AddMembers	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Group	AddMembers	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
98	191	0	NULL	GetParentGroups	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetParentGroups	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
99	192	0	NULL	RemoveMembers	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Group	RemoveMembers	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
100	193	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
101	194	0	NULL	Thumbnail				___________________________________________________________*___*	Default	Nondefined			application	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	Binary	Binary	NULL	antialias	highqualitybicubic	highquality	crop	Center	Center	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
102	195	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
103	196	0	NULL	Browse	ListItem;ExploreToolbar	OpenLinkAction		___________________________________________________________*___*	Default	Nondefined			link	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
104	197	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
105	198	0	NULL	GetSchema	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Services.Metadata.ClientMetadataProvider	GetSchema	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
106	199	0	NULL	GetVersionInfo	NULL	NULL	NULL	NULL	Default	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Repository	GetVersionInfo	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
107	200	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
108	201	0	NULL	SetInitialPreviewProperties	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Preview.DocumentPreviewProvider	SetInitialPreviewProperties	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
109	202	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
110	203	0	NULL	Decrypt	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.Cryptography.CryptoServiceProvider	Decrypt	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
111	204	0	NULL	Encrypt	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.Cryptography.CryptoServiceProvider	Encrypt	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
112	205	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
113	206	0	NULL	GetParentGroups	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.ContentRepository.Security.PermissionQueryForRest	GetParentGroups	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
114	207	0	NULL	Profile	UserActions	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	userprofile	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	SenseNet.Services.IdentityTools	BrowseProfile	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
115	1	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	BuiltIn	NULL	Admin	$2a$10$bSsyM8JgVSHTvIWfy/qQPOIZj9jMvAVPP6S0BxGTLzwhShfLRWQVW	Admin	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
116	208	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
117	209	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL		NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
118	210	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
119	211	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL		NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
120	212	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
121	213	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL		NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
122	214	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
123	215	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL		NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
124	216	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL		NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
125	10	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	BuiltIn	NULL	Somebody	$2a$10$PgXRiF4q4LFrzvWwooRsfufypvJ6haMRIqhpOxSGqz5vT93YZjQMS	Somebody	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
126	12	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	BuiltIn	NULL	Startup User	$2a$10$t3ieq26ODXo46vdN/LAfdug34jMiU9sqmjlcmpXBHL9F57k014q3m	Startup	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
127	217	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	BuiltIn	NULL	VirtualADUser	NULL	VirtualADUser	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
128	6	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	BuiltIn	NULL	Visitor	NULL	Visitor	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
129	218	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
130	219	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
131	220	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
132	221	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
133	222	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
134	223	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
135	224	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
136	225	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
137	226	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
138	227	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
139	228	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
140	229	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
141	230	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
142	231	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
143	232	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
144	13	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
145	233	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
146	234	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
147	235	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
148	236	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
149	237	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
150	238	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
151	239	0	NULL	complextypes	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
152	240	0	NULL	contenttypes	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
153	241	0	NULL	enums	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
154	242	0	NULL	fieldsettings	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
155	243	0	NULL	meta	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
156	244	0	NULL	resources	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
157	245	0	NULL	schemas	NULL	NULL	NULL	NULL	NULL	Nondefined	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
158	246	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
159	247	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
160	248	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
161	249	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
162	250	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
163	251	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
164	252	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
165	253	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
166	254	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
167	255	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
168	256	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
169	257	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
170	258	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
171	259	0	NULL	Restore	ListItem;ExploreToolbar	RestoreAction	NULL	_________________________________________________________*______	NULL	Nondefined	NULL	NULL	restore	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
";
        #endregion

        #region FLATPROPERTIES INT

        // SELECT Id, VersionId, Page, int_1, int_2, int_3, int_4, int_5, int_6, int_7, int_8, int_9, int_10, int_11, int_12, int_13, int_14, int_15, int_16, int_17, int_18, int_19, int_20, int_21, int_22, int_23, int_24, int_25, int_26, int_27, int_28, int_29, int_30, int_31, int_32, int_33, int_34, int_35, int_36, int_37, int_38, int_39, int_40 FROM FlatProperties

        private static string _prototypeFlatPropertiesInt = @"Id	VersionId	Page	int_1	int_2	int_3	int_4	int_5	int_6	int_7	int_8	int_9	int_10	int_11	int_12	int_13	int_14	int_15	int_16	int_17	int_18	int_19	int_20	int_21	int_22	int_23	int_24	int_25	int_26	int_27	int_28	int_29	int_30	int_31	int_32	int_33	int_34	int_35	int_36	int_37	int_38	int_39	int_40
1	94	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
2	95	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
3	96	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
4	97	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
5	98	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
6	99	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
7	100	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
8	101	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
9	102	0	0	1	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
10	103	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
11	104	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
12	105	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
13	106	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
14	107	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
15	108	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
16	109	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
17	110	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
18	111	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
19	112	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
20	113	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
21	114	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
22	115	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
23	116	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
24	117	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
25	118	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
26	119	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
27	120	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
28	121	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
29	122	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
30	123	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
31	124	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
32	125	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
33	126	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
34	127	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
35	128	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
36	129	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
37	130	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
38	131	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
39	132	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
40	133	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
41	134	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
42	135	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
43	136	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
44	137	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
45	138	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
46	139	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
47	140	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
48	141	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
49	142	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
50	143	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
51	144	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
52	145	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
53	146	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
54	147	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
55	148	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
56	149	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
57	150	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
58	151	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
59	152	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
60	153	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
61	154	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
62	155	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
63	156	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
64	157	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
65	158	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
66	159	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
67	160	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
68	161	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
69	162	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
70	163	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
71	164	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
72	165	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
73	166	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
74	167	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
75	168	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
76	169	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
77	170	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
78	171	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
79	172	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
80	173	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
81	174	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
82	175	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
83	176	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
84	177	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
85	178	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
86	179	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
87	180	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
88	181	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
89	182	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
90	183	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
91	184	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
92	185	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
93	186	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
94	187	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
95	188	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
96	189	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
97	190	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
98	191	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
99	192	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
100	193	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
101	194	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	120	120	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
102	195	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
103	196	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
104	197	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
105	198	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
106	199	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
107	200	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
108	201	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
109	202	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
110	203	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
111	204	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
112	205	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
113	206	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
114	207	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
115	1	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL
116	208	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	32	32	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
117	209	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
118	210	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
119	211	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
120	212	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
121	213	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
122	214	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
123	215	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
124	216	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
125	10	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL
126	12	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL
127	217	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	0	NULL	NULL	NULL	NULL	NULL
128	6	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	1	NULL	NULL	NULL	NULL	NULL
129	218	0	0	1	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
130	219	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
131	220	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
132	221	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
133	222	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
134	223	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
135	224	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
136	225	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
137	226	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
138	227	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
139	228	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
140	229	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
141	230	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
142	231	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
143	232	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
144	13	0	NULL	1	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
145	233	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
146	234	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
147	235	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
148	236	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
149	237	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
150	238	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
151	239	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
152	240	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
153	241	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
154	242	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
155	243	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
156	244	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
157	245	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
158	246	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
159	247	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
160	248	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
161	249	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
162	250	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
163	251	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
164	252	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
165	253	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
166	254	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
167	255	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	-4	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
168	256	0	0	0	0	0	0	1	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	1	0	0	NULL	NULL	NULL	NULL	NULL	0	0	100	NULL	NULL	NULL	NULL	NULL	NULL
169	257	0	0	1	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
170	258	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
171	259	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
";
        #endregion

        #region FLATPROPERTIES DATETIME

        // SELECT Id, VersionId, Page, datetime_1, datetime_2, datetime_3, datetime_4, datetime_5, datetime_6, datetime_7, datetime_8, datetime_9, datetime_10, datetime_11, datetime_12, datetime_13, datetime_14, datetime_15, datetime_16, datetime_17, datetime_18, datetime_19, datetime_20, datetime_21, datetime_22, datetime_23, datetime_24, datetime_25 FROM FlatProperties

        private static string _prototypeFlatPropertiesDatetime = @"Id	VersionId	Page	datetime_1	datetime_2	datetime_3	datetime_4	datetime_5	datetime_6	datetime_7	datetime_8	datetime_9	datetime_10	datetime_11	datetime_12	datetime_13	datetime_14	datetime_15	datetime_16	datetime_17	datetime_18	datetime_19	datetime_20	datetime_21	datetime_22	datetime_23	datetime_24	datetime_25
1	94	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
2	95	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
3	96	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
4	97	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
5	98	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
6	99	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
7	100	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
8	101	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
9	102	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
10	103	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
11	104	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
12	105	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
13	106	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
14	107	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
15	108	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
16	109	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
17	110	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
18	111	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
19	112	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
20	113	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
21	114	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
22	115	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
23	116	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
24	117	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
25	118	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
26	119	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
27	120	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
28	121	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
29	122	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
30	123	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
31	124	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
32	125	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
33	126	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
34	127	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
35	128	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
36	129	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
37	130	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
38	131	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
39	132	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
40	133	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
41	134	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
42	135	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
43	136	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
44	137	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
45	138	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
46	139	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
47	140	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
48	141	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
49	142	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
50	143	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
51	144	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
52	145	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
53	146	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
54	147	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
55	148	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
56	149	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
57	150	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
58	151	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
59	152	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
60	153	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
61	154	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
62	155	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
63	156	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
64	157	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
65	158	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
66	159	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
67	160	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
68	161	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
69	162	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
70	163	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
71	164	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
72	165	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
73	166	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
74	167	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
75	168	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
76	169	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
77	170	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
78	171	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
79	172	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
80	173	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
81	174	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
82	175	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
83	176	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
84	177	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
85	178	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
86	179	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
87	180	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
88	181	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
89	182	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
90	183	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
91	184	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
92	185	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
93	186	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
94	187	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
95	188	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
96	189	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
97	190	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
98	191	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
99	192	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
100	193	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
101	194	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
102	195	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
103	196	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
104	197	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
105	198	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
106	199	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
107	200	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
108	201	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
109	202	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
110	203	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
111	204	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
112	205	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
113	206	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
114	207	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
115	1	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	2018-11-13 09:06:21.640	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
116	208	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
117	209	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
118	210	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
119	211	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
120	212	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
121	213	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
122	214	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
123	215	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
124	216	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
125	10	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	2018-11-13 09:06:22.200	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
126	12	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	2018-11-13 09:06:22.417	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
127	217	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
128	6	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
129	218	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
130	219	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
131	220	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
132	221	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
133	222	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
134	223	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
135	224	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
136	225	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
137	226	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
138	227	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
139	228	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
140	229	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
141	230	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
142	231	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
143	232	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
144	13	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
145	233	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
146	234	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
147	235	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
148	236	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
149	237	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
150	238	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
151	239	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
152	240	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
153	241	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
154	242	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
155	243	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
156	244	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
157	245	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
158	246	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
159	247	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
160	248	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
161	249	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
162	250	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
163	251	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
164	252	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
165	253	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
166	254	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
167	255	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
168	256	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
169	257	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
170	258	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
171	259	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
";
        #endregion

        #region FLATPROPERTIES DECIMAL

        // SELECT Id, VersionId, Page, money_1, money_2, money_3, money_4, money_5, money_6, money_7, money_8, money_9, money_10, money_11, money_12, money_13, money_14, money_15 FROM FlatProperties

        private static string _prototypeFlatPropertiesDecimal = @"Id	VersionId	Page	money_1	money_2	money_3	money_4	money_5	money_6	money_7	money_8	money_9	money_10	money_11	money_12	money_13	money_14	money_15
1	94	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
2	95	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
3	96	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
4	97	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
5	98	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
6	99	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
7	100	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
8	101	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
9	102	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
10	103	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
11	104	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
12	105	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
13	106	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
14	107	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
15	108	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
16	109	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
17	110	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
18	111	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
19	112	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
20	113	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
21	114	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
22	115	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
23	116	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
24	117	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
25	118	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
26	119	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
27	120	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
28	121	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
29	122	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
30	123	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
31	124	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
32	125	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
33	126	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
34	127	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
35	128	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
36	129	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
37	130	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
38	131	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
39	132	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
40	133	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
41	134	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
42	135	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
43	136	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
44	137	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
45	138	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
46	139	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
47	140	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
48	141	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
49	142	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
50	143	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
51	144	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
52	145	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
53	146	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
54	147	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
55	148	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
56	149	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
57	150	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
58	151	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
59	152	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
60	153	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
61	154	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
62	155	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
63	156	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
64	157	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
65	158	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
66	159	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
67	160	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
68	161	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
69	162	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
70	163	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
71	164	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
72	165	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
73	166	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
74	167	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
75	168	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
76	169	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
77	170	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
78	171	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
79	172	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
80	173	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
81	174	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
82	175	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
83	176	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
84	177	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
85	178	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
86	179	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
87	180	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
88	181	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
89	182	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
90	183	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
91	184	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
92	185	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
93	186	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
94	187	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
95	188	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
96	189	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
97	190	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
98	191	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
99	192	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
100	193	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
101	194	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
102	195	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
103	196	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
104	197	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
105	198	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
106	199	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
107	200	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
108	201	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
109	202	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
110	203	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
111	204	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
112	205	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
113	206	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
114	207	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
115	1	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
116	208	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
117	209	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
118	210	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
119	211	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
120	212	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
121	213	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
122	214	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
123	215	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
124	216	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
125	10	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
126	12	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
127	217	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
128	6	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
129	218	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
130	219	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
131	220	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
132	221	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
133	222	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
134	223	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
135	224	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
136	225	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
137	226	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
138	227	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
139	228	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
140	229	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
141	230	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
142	231	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
143	232	0	0,00	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
144	13	0	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
145	233	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
146	234	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
147	235	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
148	236	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
149	237	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
150	238	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
151	239	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
152	240	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
153	241	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
154	242	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
155	243	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
156	244	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
157	245	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
158	246	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
159	247	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
160	248	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
161	249	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
162	250	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
163	251	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
164	252	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
165	253	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
166	254	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
167	255	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
168	256	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
169	257	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
170	258	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
171	259	0	0,00	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL	NULL
";
        #endregion

        #region REFERENCEPROPERTIES

        // SELECT ReferencePropertyId, VersionId, PropertyTypeId, ReferredNodeId FROM ReferenceProperties

        private static string _prototypeReferenceProperties = @"ReferencePropertyId	VersionId	PropertyTypeId	ReferredNodeId
2	11	72	7
3	7	72	1
4	7	72	1197
5	209	72	11
6	209	72	1199
7	209	72	1197
8	211	72	7
9	213	72	7
10	215	72	1196
11	215	72	1201
12	215	72	11
13	215	72	1199
14	215	72	1197
";
        #endregion

        #region SCHEMA

        private static readonly string _prototypeSchema = @"<?xml version='1.0' encoding='utf-8' ?>
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
		<PropertyType itemID='16' name='SharingData' dataType='Text' mapping='3' />
		<PropertyType itemID='17' name='ExtensionData' dataType='Text' mapping='4' />
		<PropertyType itemID='18' name='IsTaggable' dataType='Int' mapping='7' />
		<PropertyType itemID='19' name='Tags' dataType='Text' mapping='5' />
		<PropertyType itemID='20' name='IsRateable' dataType='Int' mapping='8' />
		<PropertyType itemID='21' name='RateStr' dataType='String' mapping='0' />
		<PropertyType itemID='22' name='RateAvg' dataType='Currency' mapping='0' />
		<PropertyType itemID='23' name='RateCount' dataType='Int' mapping='9' />
		<PropertyType itemID='24' name='CheckInComments' dataType='Text' mapping='6' />
		<PropertyType itemID='25' name='RejectReason' dataType='Text' mapping='7' />
		<PropertyType itemID='26' name='AppName' dataType='String' mapping='1' />
		<PropertyType itemID='27' name='Disabled' dataType='Int' mapping='10' />
		<PropertyType itemID='28' name='IsModal' dataType='Int' mapping='11' />
		<PropertyType itemID='29' name='Clear' dataType='Int' mapping='12' />
		<PropertyType itemID='30' name='Scenario' dataType='String' mapping='2' />
		<PropertyType itemID='31' name='ActionTypeName' dataType='String' mapping='3' />
		<PropertyType itemID='32' name='StyleHint' dataType='String' mapping='4' />
		<PropertyType itemID='33' name='RequiredPermissions' dataType='String' mapping='5' />
		<PropertyType itemID='34' name='DeepPermissionCheck' dataType='Int' mapping='13' />
		<PropertyType itemID='35' name='IncludeBackUrl' dataType='String' mapping='6' />
		<PropertyType itemID='36' name='CacheControl' dataType='String' mapping='7' />
		<PropertyType itemID='37' name='MaxAge' dataType='String' mapping='8' />
		<PropertyType itemID='38' name='CustomUrlParameters' dataType='String' mapping='9' />
		<PropertyType itemID='39' name='StoredIcon' dataType='String' mapping='10' />
		<PropertyType itemID='40' name='ContentListBindings' dataType='Text' mapping='8' />
		<PropertyType itemID='41' name='ContentListDefinition' dataType='Text' mapping='9' />
		<PropertyType itemID='42' name='DefaultView' dataType='String' mapping='11' />
		<PropertyType itemID='43' name='AvailableViews' dataType='Reference' mapping='2' />
		<PropertyType itemID='44' name='AvailableContentTypeFields' dataType='Reference' mapping='3' />
		<PropertyType itemID='45' name='ListEmail' dataType='String' mapping='12' />
		<PropertyType itemID='46' name='ExchangeSubscriptionId' dataType='String' mapping='13' />
		<PropertyType itemID='47' name='OverwriteFiles' dataType='Int' mapping='14' />
		<PropertyType itemID='48' name='GroupAttachments' dataType='String' mapping='14' />
		<PropertyType itemID='49' name='SaveOriginalEmail' dataType='Int' mapping='15' />
		<PropertyType itemID='50' name='IncomingEmailWorkflow' dataType='Reference' mapping='4' />
		<PropertyType itemID='51' name='OnlyFromLocalGroups' dataType='Int' mapping='16' />
		<PropertyType itemID='52' name='InboxFolder' dataType='String' mapping='15' />
		<PropertyType itemID='53' name='OwnerWhenVisitor' dataType='Reference' mapping='5' />
		<PropertyType itemID='54' name='AspectDefinition' dataType='Text' mapping='10' />
		<PropertyType itemID='55' name='FieldSettingContents' dataType='Reference' mapping='6' />
		<PropertyType itemID='56' name='Link' dataType='Reference' mapping='7' />
		<PropertyType itemID='57' name='WorkflowsRunning' dataType='Int' mapping='17' />
		<PropertyType itemID='58' name='UserAgentPattern' dataType='String' mapping='16' />
		<PropertyType itemID='59' name='SyncGuid' dataType='String' mapping='17' />
		<PropertyType itemID='60' name='LastSync' dataType='DateTime' mapping='2' />
		<PropertyType itemID='61' name='Watermark' dataType='String' mapping='18' />
		<PropertyType itemID='62' name='PageCount' dataType='Int' mapping='18' />
		<PropertyType itemID='63' name='MimeType' dataType='String' mapping='19' />
		<PropertyType itemID='64' name='Shapes' dataType='Text' mapping='11' />
		<PropertyType itemID='65' name='PageAttributes' dataType='Text' mapping='12' />
		<PropertyType itemID='66' name='From' dataType='String' mapping='20' />
		<PropertyType itemID='67' name='Body' dataType='Text' mapping='13' />
		<PropertyType itemID='68' name='Sent' dataType='DateTime' mapping='3' />
		<PropertyType itemID='69' name='ClassName' dataType='String' mapping='21' />
		<PropertyType itemID='70' name='MethodName' dataType='String' mapping='22' />
		<PropertyType itemID='71' name='Parameters' dataType='Text' mapping='14' />
		<PropertyType itemID='72' name='Members' dataType='Reference' mapping='8' />
		<PropertyType itemID='73' name='StatusCode' dataType='String' mapping='23' />
		<PropertyType itemID='74' name='RedirectUrl' dataType='String' mapping='24' />
		<PropertyType itemID='75' name='Width' dataType='Int' mapping='19' />
		<PropertyType itemID='76' name='Height' dataType='Int' mapping='20' />
		<PropertyType itemID='77' name='Keywords' dataType='Text' mapping='15' />
		<PropertyType itemID='78' name='DateTaken' dataType='DateTime' mapping='4' />
		<PropertyType itemID='79' name='CoverImage' dataType='Reference' mapping='9' />
		<PropertyType itemID='80' name='ImageType' dataType='String' mapping='25' />
		<PropertyType itemID='81' name='ImageFieldName' dataType='String' mapping='26' />
		<PropertyType itemID='82' name='Stretch' dataType='Int' mapping='21' />
		<PropertyType itemID='83' name='OutputFormat' dataType='String' mapping='27' />
		<PropertyType itemID='84' name='SmoothingMode' dataType='String' mapping='28' />
		<PropertyType itemID='85' name='InterpolationMode' dataType='String' mapping='29' />
		<PropertyType itemID='86' name='PixelOffsetMode' dataType='String' mapping='30' />
		<PropertyType itemID='87' name='ResizeTypeMode' dataType='String' mapping='31' />
		<PropertyType itemID='88' name='CropVAlign' dataType='String' mapping='32' />
		<PropertyType itemID='89' name='CropHAlign' dataType='String' mapping='33' />
		<PropertyType itemID='90' name='GlobalOnly' dataType='Int' mapping='22' />
		<PropertyType itemID='91' name='Date' dataType='DateTime' mapping='5' />
		<PropertyType itemID='92' name='MemoType' dataType='String' mapping='34' />
		<PropertyType itemID='93' name='SeeAlso' dataType='Reference' mapping='10' />
		<PropertyType itemID='94' name='Query' dataType='Text' mapping='16' />
		<PropertyType itemID='95' name='Downloads' dataType='Currency' mapping='1' />
		<PropertyType itemID='96' name='IsActive' dataType='Int' mapping='23' />
		<PropertyType itemID='97' name='IsWallContainer' dataType='Int' mapping='24' />
		<PropertyType itemID='98' name='WorkspaceSkin' dataType='Reference' mapping='11' />
		<PropertyType itemID='99' name='Manager' dataType='Reference' mapping='12' />
		<PropertyType itemID='100' name='Deadline' dataType='DateTime' mapping='6' />
		<PropertyType itemID='101' name='IsCritical' dataType='Int' mapping='25' />
		<PropertyType itemID='102' name='PendingUserLang' dataType='String' mapping='35' />
		<PropertyType itemID='103' name='Language' dataType='String' mapping='36' />
		<PropertyType itemID='104' name='EnableClientBasedCulture' dataType='Int' mapping='26' />
		<PropertyType itemID='105' name='EnableUserBasedCulture' dataType='Int' mapping='27' />
		<PropertyType itemID='106' name='UrlList' dataType='Text' mapping='17' />
		<PropertyType itemID='107' name='StartPage' dataType='Reference' mapping='13' />
		<PropertyType itemID='108' name='LoginPage' dataType='Reference' mapping='14' />
		<PropertyType itemID='109' name='SiteSkin' dataType='Reference' mapping='15' />
		<PropertyType itemID='110' name='DenyCrossSiteAccess' dataType='Int' mapping='28' />
		<PropertyType itemID='111' name='EnableAutofilters' dataType='String' mapping='37' />
		<PropertyType itemID='112' name='EnableLifespanFilter' dataType='String' mapping='38' />
		<PropertyType itemID='113' name='StartDate' dataType='DateTime' mapping='7' />
		<PropertyType itemID='114' name='DueDate' dataType='DateTime' mapping='8' />
		<PropertyType itemID='115' name='AssignedTo' dataType='Reference' mapping='16' />
		<PropertyType itemID='116' name='Priority' dataType='String' mapping='39' />
		<PropertyType itemID='117' name='Status' dataType='String' mapping='40' />
		<PropertyType itemID='118' name='TaskCompletion' dataType='Int' mapping='29' />
		<PropertyType itemID='119' name='KeepUntil' dataType='DateTime' mapping='9' />
		<PropertyType itemID='120' name='OriginalPath' dataType='String' mapping='41' />
		<PropertyType itemID='121' name='WorkspaceId' dataType='Int' mapping='30' />
		<PropertyType itemID='122' name='WorkspaceRelativePath' dataType='String' mapping='42' />
		<PropertyType itemID='123' name='MinRetentionTime' dataType='Int' mapping='31' />
		<PropertyType itemID='124' name='SizeQuota' dataType='Int' mapping='32' />
		<PropertyType itemID='125' name='BagCapacity' dataType='Int' mapping='33' />
		<PropertyType itemID='126' name='Enabled' dataType='Int' mapping='34' />
		<PropertyType itemID='127' name='Domain' dataType='String' mapping='43' />
		<PropertyType itemID='128' name='Email' dataType='String' mapping='44' />
		<PropertyType itemID='129' name='FullName' dataType='String' mapping='45' />
		<PropertyType itemID='130' name='OldPasswords' dataType='Text' mapping='18' />
		<PropertyType itemID='131' name='PasswordHash' dataType='String' mapping='46' />
		<PropertyType itemID='132' name='LoginName' dataType='String' mapping='47' />
		<PropertyType itemID='133' name='Profile' dataType='Reference' mapping='17' />
		<PropertyType itemID='134' name='FollowedWorkspaces' dataType='Reference' mapping='18' />
		<PropertyType itemID='135' name='LastLoggedOut' dataType='DateTime' mapping='10' />
		<PropertyType itemID='136' name='JobTitle' dataType='String' mapping='48' />
		<PropertyType itemID='137' name='ImageRef' dataType='Reference' mapping='19' />
		<PropertyType itemID='138' name='ImageData' dataType='Binary' mapping='1' />
		<PropertyType itemID='139' name='Captcha' dataType='String' mapping='49' />
		<PropertyType itemID='140' name='Department' dataType='String' mapping='50' />
		<PropertyType itemID='141' name='Languages' dataType='String' mapping='51' />
		<PropertyType itemID='142' name='Phone' dataType='String' mapping='52' />
		<PropertyType itemID='143' name='Gender' dataType='String' mapping='53' />
		<PropertyType itemID='144' name='MaritalStatus' dataType='String' mapping='54' />
		<PropertyType itemID='145' name='BirthDate' dataType='DateTime' mapping='11' />
		<PropertyType itemID='146' name='Education' dataType='Text' mapping='19' />
		<PropertyType itemID='147' name='TwitterAccount' dataType='String' mapping='55' />
		<PropertyType itemID='148' name='FacebookURL' dataType='String' mapping='56' />
		<PropertyType itemID='149' name='LinkedInURL' dataType='String' mapping='57' />
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
			<PropertyType name='SharingData' />
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
				<PropertyType name='SharingData' />
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
				<PropertyType name='LastLoggedOut' />
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
				<PropertyType name='SharingData' />
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
				<PropertyType name='SharingData' />
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
					<PropertyType name='SharingData' />
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




        #region INITIAL DATA

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
12	5	3	12	12	8	0	Startup	""""	/Root/IMS/BuiltIn/Portal/Startup
1000	2	5	13	13	3	1	System	""""	/Root/System
1001	1000	5	14	14	1	1	Schema	Schema	/Root/System/Schema
1002	1001	5	15	15	1	1	ContentTypes	ContentTypes	/Root/System/Schema/ContentTypes
1003	1000	5	16	16	2	1	Settings	Settings	/Root/System/Settings
";

        //private static string _initialBinaryProperties = @"BinaryPropertyId	VersionId	PropertyTypeId	FileId";
        //private static string _initialFiles = @"FileId	ContentType	FileNameWithoutExtension	Extension	Size";
        //private static string _initialTextProperties = @"TextPropertyNVarcharId	VersionId	PropertyTypeId	Value";
        //private static string _initialFlatPropertiesNvarchar = @"Id	VersionId	Page	nvarchar_1	nvarchar_2	nvarchar_3	nvarchar_4	nvarchar_5	nvarchar_6	nvarchar_7	nvarchar_8	nvarchar_9	nvarchar_10	nvarchar_11	nvarchar_12	nvarchar_13	nvarchar_14	nvarchar_15	nvarchar_16	nvarchar_17	nvarchar_18	nvarchar_19	nvarchar_20	nvarchar_21	nvarchar_22	nvarchar_23	nvarchar_24	nvarchar_25	nvarchar_26	nvarchar_27	nvarchar_28	nvarchar_29	nvarchar_30	nvarchar_31	nvarchar_32	nvarchar_33	nvarchar_34	nvarchar_35	nvarchar_36	nvarchar_37	nvarchar_38	nvarchar_39	nvarchar_40	nvarchar_41	nvarchar_42	nvarchar_43	nvarchar_44	nvarchar_45	nvarchar_46	nvarchar_47	nvarchar_48	nvarchar_49	nvarchar_50	nvarchar_51	nvarchar_52	nvarchar_53	nvarchar_54	nvarchar_55	nvarchar_56	nvarchar_57	nvarchar_58	nvarchar_59	nvarchar_60	nvarchar_61	nvarchar_62	nvarchar_63	nvarchar_64	nvarchar_65	nvarchar_66	nvarchar_67	nvarchar_68	nvarchar_69	nvarchar_70	nvarchar_71	nvarchar_72	nvarchar_73	nvarchar_74	nvarchar_75	nvarchar_76	nvarchar_77	nvarchar_78	nvarchar_79	nvarchar_80";
        //private static string _initialFlatPropertiesInt = @"Id	VersionId	Page	int_1	int_2	int_3	int_4	int_5	int_6	int_7	int_8	int_9	int_10	int_11	int_12	int_13	int_14	int_15	int_16	int_17	int_18	int_19	int_20	int_21	int_22	int_23	int_24	int_25	int_26	int_27	int_28	int_29	int_30	int_31	int_32	int_33	int_34	int_35	int_36	int_37	int_38	int_39	int_40";
        //private static string _initialFlatPropertiesDatetime = @"Id	VersionId	Page	datetime_1	datetime_2	datetime_3	datetime_4	datetime_5	datetime_6	datetime_7	datetime_8	datetime_9	datetime_10	datetime_11	datetime_12	datetime_13	datetime_14	datetime_15	datetime_16	datetime_17	datetime_18	datetime_19	datetime_20	datetime_21	datetime_22	datetime_23	datetime_24	datetime_25";
        //private static string _initialFlatPropertiesDecimal = @"Id	VersionId	Page	money_1	money_2	money_3	money_4	money_5	money_6	money_7	money_8	money_9	money_10	money_11	money_12	money_13	money_14	money_15";
        //private static string _initialReferenceProperties = @"ReferencePropertyId	VersionId	PropertyTypeId	ReferredNodeId";

        private static readonly string _initialSchema = @"<?xml version='1.0' encoding='utf-8' ?>
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
		<PropertyType itemID='148' name='LastLoggedOut' dataType='DateTime' mapping='11' />
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
				<PropertyType name='LastLoggedOut' />
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

