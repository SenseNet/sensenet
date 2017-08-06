using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal partial class InMemoryDataProvider
    {
        #region NODES

        // SELECT NodeId, COALESCE(ParentNodeId, 0) ParentNodeId, NodeTypeId, LastMajorVersionId, LastMinorVersionId, [Index], IsSystem, Name, COALESCE(DisplayName, '""""'), [Path] FROM Nodes

        private static string _initialNodes = @"NodeId	ParentNodeId	NodeTypeId	LastMajorVersionId	LastMinorVersionId	Index	IsSystem	Name	(No column name)	Path
1	5	3	1	1	1	0	Admin	""""	/Root/IMS/BuiltIn/Portal/Admin
2	0	4	2	2	1	0	Root	""""	/Root
3	2	6	3	3	3	0	IMS	""""	/Root/IMS
4	3	7	4	4	0	0	BuiltIn	""""	/Root/IMS/BuiltIn
5	4	8	5	5	0	0	Portal	""""	/Root/IMS/BuiltIn/Portal
6	5	3	6	6	4	0	Visitor	""""	/Root/IMS/BuiltIn/Portal/Visitor
7	5	2	7	7	4	0	Administrators	""""	/Root/IMS/BuiltIn/Portal/Administrators
8	5	2	8	8	3	0	Everyone	""""	/Root/IMS/BuiltIn/Portal/Everyone
9	5	2	9	9	5	0	Owners	""""	/Root/IMS/BuiltIn/Portal/Owners
10	5	3	10	10	6	0	Somebody	""""	/Root/IMS/BuiltIn/Portal/Somebody
11	5	2	11	11	7	0	Operators	""""	/Root/IMS/BuiltIn/Portal/Operators
1000	2	5	12	12	3	1	System	""""	/Root/System
1001	1000	5	13	13	1	1	Schema	""""	/Root/System/Schema
1002	1001	5	14	14	1	1	ContentTypes	""""	/Root/System/Schema/ContentTypes
1003	1000	5	15	15	2	1	Settings	""""	/Root/System/Settings
1004	1002	9	16	16	0	1	ContentType	$Ctd-ContentType,DisplayName	/Root/System/Schema/ContentTypes/ContentType
1005	1002	9	17	17	0	1	GenericContent	$Ctd-GenericContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent
1006	1005	9	18	18	0	1	Folder	$Ctd-Folder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder
1007	1006	9	19	19	0	1	ADFolder	$Ctd-ADFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ADFolder
1008	1005	9	20	20	0	1	File	$Ctd-File,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File
1009	1008	9	21	21	0	1	Settings	$Ctd-Settings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings
1010	1009	9	22	22	0	1	ADSettings	$Ctd-ADSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/ADSettings
1011	1008	9	23	23	0	1	SystemFile	$Ctd-SystemFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile
1012	1011	9	24	24	0	1	ApplicationCacheFile	$Ctd-ApplicationCacheFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/ApplicationCacheFile
1013	1005	9	25	25	0	1	Application	$Ctd-Application,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application
1014	1013	9	26	26	0	1	ApplicationOverride	$Ctd-ApplicationOverride,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ApplicationOverride
1015	1005	9	27	27	0	1	Workflow	$Ctd-Workflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow
1016	1015	9	28	28	0	1	ApprovalWorkflow	$Ctd-ApprovalWorkflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow/ApprovalWorkflow
1017	1005	9	29	29	0	1	ListItem	$Ctd-ListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem
1018	1017	9	30	30	0	1	Task	$Ctd-Task,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Task
1019	1018	9	31	31	0	1	ApprovalWorkflowTask	$Ctd-ApprovalWorkflowTask,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Task/ApprovalWorkflowTask
1020	1017	9	32	32	0	1	WebContent	$Ctd-WebContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/WebContent
1021	1020	9	33	33	0	1	Article	$Ctd-Article,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/WebContent/Article
1022	1006	9	34	34	0	1	ArticleSection	$Ctd-ArticleSection,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ArticleSection
1023	1006	9	35	35	0	1	ContentList	$Ctd-ContentList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList
1024	1023	9	36	36	0	1	Aspect	$Ctd-Aspect,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Aspect
1025	1013	9	37	37	0	1	BackupIndexHandler	$Ctd-BackupIndexHandler,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/BackupIndexHandler
1026	1005	9	38	38	0	1	FieldSettingContent	$Ctd-FieldSettingContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent
1027	1026	9	39	39	0	1	BinaryFieldSetting	$Ctd-BinaryFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/BinaryFieldSetting
1028	1006	9	40	40	0	1	Workspace	$Ctd-Workspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace
1029	1028	9	41	41	0	1	Blog	$Ctd-Blog,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Blog
1030	1017	9	42	42	0	1	BlogPost	$Ctd-BlogPost,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/BlogPost
1031	1017	9	43	43	0	1	CalendarEvent	$Ctd-CalendarEvent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/CalendarEvent
1032	1013	9	44	44	0	1	CaptchaImageApplication	$Ctd-CaptchaImageApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/CaptchaImageApplication
1033	1017	9	45	45	0	1	Car	$Ctd-Car,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Car
1034	1026	9	46	46	0	1	TextFieldSetting	$Ctd-TextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting
1035	1034	9	47	47	0	1	ShortTextFieldSetting	$Ctd-ShortTextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting
1036	1035	9	48	48	0	1	ChoiceFieldSetting	$Ctd-ChoiceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting
1037	1017	9	49	49	0	1	Comment	$Ctd-Comment,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Comment
1038	1017	9	50	50	0	1	ConfirmationItem	$Ctd-ConfirmationItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/ConfirmationItem
1039	1005	9	51	51	0	1	ContentLink	$Ctd-ContentLink,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ContentLink
1040	1006	9	52	52	0	1	SmartFolder	$Ctd-SmartFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder
1041	1040	9	53	53	0	1	ContentRotator	$Ctd-ContentRotator,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SmartFolder/ContentRotator
1042	1008	9	54	54	0	1	ContentView	$Ctd-ContentView,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/ContentView
1043	1006	9	55	55	0	1	ContentViews	$Ctd-ContentViews,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentViews
1044	1008	9	56	56	0	1	Contract	$Ctd-Contract,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Contract
1045	1026	9	57	57	0	1	NumberFieldSetting	$Ctd-NumberFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting
1046	1045	9	58	58	0	1	CurrencyFieldSetting	$Ctd-CurrencyFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NumberFieldSetting/CurrencyFieldSetting
1047	1023	9	59	59	0	1	ItemList	$Ctd-ItemList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList
1048	1047	9	60	60	0	1	CustomList	$Ctd-CustomList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/CustomList
1049	1017	9	61	61	0	1	CustomListItem	$Ctd-CustomListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/CustomListItem
1050	1026	9	62	62	0	1	DateTimeFieldSetting	$Ctd-DateTimeFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/DateTimeFieldSetting
1051	1006	9	63	63	0	1	Device	$Ctd-Device,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Device
1052	1006	9	64	64	0	1	DiscussionForum	$Ctd-DiscussionForum,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/DiscussionForum
1053	1023	9	65	65	0	1	Library	$Ctd-Library,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library
1054	1053	9	66	66	0	1	DocumentLibrary	$Ctd-DocumentLibrary,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/DocumentLibrary
1055	1015	9	67	67	0	1	DocumentPreviewWorkflow	$Ctd-DocumentPreviewWorkflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow/DocumentPreviewWorkflow
1056	1028	9	68	68	0	1	DocumentWorkspace	$Ctd-DocumentWorkspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/DocumentWorkspace
1057	1006	9	69	69	0	1	DocumentWorkspaceFolder	$Ctd-DocumentWorkspaceFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/DocumentWorkspaceFolder
1058	1006	9	70	70	0	1	Domain	$Ctd-Domain,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Domain
1059	1006	9	71	71	0	1	Domains	$Ctd-Domains,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Domains
1060	1008	9	72	72	0	1	DynamicJsonContent	Dynamic JSON content	/Root/System/Schema/ContentTypes/GenericContent/File/DynamicJsonContent
1061	1006	9	73	73	0	1	Email	$Ctd-Email,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Email
1062	1047	9	74	74	0	1	EventList	$Ctd-EventList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/EventList
1063	1047	9	75	75	0	1	Form	$Ctd-Form,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/Form
1064	1063	9	76	76	0	1	EventRegistrationForm	$Ctd-EventRegistrationForm,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/Form/EventRegistrationForm
1065	1017	9	77	77	0	1	FormItem	$Ctd-FormItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/FormItem
1066	1065	9	78	78	0	1	EventRegistrationFormItem	$Ctd-EventRegistrationFormItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/FormItem/EventRegistrationFormItem
1067	1008	9	79	79	0	1	ExecutableFile	$Ctd-ExecutableFile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/ExecutableFile
1068	1006	9	80	80	0	1	ExpenseClaim	$Ctd-ExpenseClaim,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ExpenseClaim
1069	1017	9	81	81	0	1	ExpenseClaimItem	$Ctd-ExpenseClaimItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/ExpenseClaimItem
1070	1015	9	82	82	0	1	ExpenseClaimWorkflow	$Ctd-ExpenseClaimWorkflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow/ExpenseClaimWorkflow
1071	1019	9	83	83	0	1	ExpenseClaimWorkflowTask	$Ctd-ExpenseClaimWorkflowTask,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Task/ApprovalWorkflowTask/ExpenseClaimWorkflowTask
1072	1013	9	84	84	0	1	ExportToCsvApplication	$Ctd-ExportToCsvApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ExportToCsvApplication
1073	1008	9	85	85	0	1	FieldControlTemplate	$Ctd-FieldControlTemplate,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/FieldControlTemplate
1074	1006	9	86	86	0	1	FieldControlTemplates	$Ctd-FieldControlTemplates,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/FieldControlTemplates
1075	1015	9	87	87	0	1	ForgottenPasswordWorkflow	$Ctd-ForgottenPasswordWorkflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow/ForgottenPasswordWorkflow
1076	1017	9	88	88	0	1	ForumEntry	$Ctd-ForumEntry,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/ForumEntry
1077	1047	9	89	89	0	1	ForumTopic	$Ctd-ForumTopic,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/ForumTopic
1078	1013	9	90	90	0	1	GenericODataApplication	$Ctd-GenericODataApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/GenericODataApplication
1079	1013	9	91	91	0	1	GoogleSitemap	$Ctd-GoogleSitemap,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/GoogleSitemap
1080	1005	9	92	92	0	1	Group	$Ctd-Group,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Group
1081	1020	9	93	93	0	1	HTMLContent	$Ctd-HTMLContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/WebContent/HTMLContent
1082	1008	9	94	94	0	1	HtmlTemplate	$Ctd-HtmlTemplate,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/HtmlTemplate
1083	1013	9	95	95	0	1	HttpEndpointDemoContent	$Ctd-HttpEndpointDemoContent,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpEndpointDemoContent
1084	1013	9	96	96	0	1	HttpHandlerApplication	$Ctd-HttpHandlerApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpHandlerApplication
1085	1013	9	97	97	0	1	HttpStatusApplication	$Ctd-HttpStatusApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/HttpStatusApplication
1086	1026	9	98	98	0	1	HyperLinkFieldSetting	$Ctd-HyperLinkFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/HyperLinkFieldSetting
1087	1008	9	99	99	0	1	Image	$Ctd-Image,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Image
1088	1053	9	100	100	0	1	ImageLibrary	$Ctd-ImageLibrary,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/Library/ImageLibrary
1089	1013	9	101	101	0	1	ImgResizeApplication	$Ctd-ImgResizeApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/ImgResizeApplication
1090	1009	9	102	102	0	1	IndexingSettings	$Ctd-IndexingSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/IndexingSettings
1091	1026	9	103	103	0	1	IntegerFieldSetting	$Ctd-IntegerFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/IntegerFieldSetting
1092	1002	9	104	104	0	1	JournalNode	$Ctd-JournalNode,DisplayName	/Root/System/Schema/ContentTypes/JournalNode
1093	1006	9	105	105	0	1	KPIDatasource	$Ctd-KPIDatasource,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/KPIDatasource
1094	1006	9	106	106	0	1	KPIDatasources	$Ctd-KPIDatasources,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/KPIDatasources
1095	1017	9	107	107	0	1	Like	$Ctd-Like,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Like
1096	1017	9	108	108	0	1	Link	$Ctd-Link,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Link
1097	1047	9	109	109	0	1	LinkList	$Ctd-LinkList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/LinkList
1098	1008	9	110	110	0	1	UserControl	$Ctd-UserControl,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/UserControl
1099	1098	9	111	111	0	1	ViewBase	$Ctd-ViewBase,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/UserControl/ViewBase
1100	1099	9	112	112	0	1	ListView	$Ctd-ListView,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/UserControl/ViewBase/ListView
1101	1009	9	113	113	0	1	LoggingSettings	$Ctd-LoggingSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/LoggingSettings
1102	1034	9	114	114	0	1	LongTextFieldSetting	$Ctd-LongTextFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/LongTextFieldSetting
1103	1015	9	115	115	0	1	MailProcessorWorkflow	$Ctd-MailProcessorWorkflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow/MailProcessorWorkflow
1104	1011	9	116	116	0	1	MasterPage	$Ctd-MasterPage,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/MasterPage
1105	1017	9	117	117	0	1	Memo	$Ctd-Memo,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Memo
1106	1047	9	118	118	0	1	MemoList	$Ctd-MemoList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/MemoList
1107	1005	9	119	119	0	1	NotificationConfig	$Ctd-NotificationConfig,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/NotificationConfig
1108	1026	9	120	120	0	1	NullFieldSetting	$Ctd-NullFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/NullFieldSetting
1109	1008	9	121	121	0	1	OrderForm	$Ctd-OrderForm,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/OrderForm
1110	1006	9	122	122	0	1	OrganizationalUnit	$Ctd-OrganizationalUnit,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/OrganizationalUnit
1111	1006	9	123	123	0	1	OtherWorkspaceFolder	$Ctd-OtherWorkspaceFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/OtherWorkspaceFolder
1112	1026	9	124	124	0	1	PageBreakFieldSetting	$Ctd-PageBreakFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/PageBreakFieldSetting
1113	1013	9	125	125	0	1	Webform	$Ctd-Webform,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/Webform
1114	1113	9	126	126	0	1	Page	$Ctd-Page,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/Webform/Page
1115	1011	9	127	127	0	1	PageTemplate	$Ctd-PageTemplate,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/PageTemplate
1116	1035	9	128	128	0	1	PasswordFieldSetting	$Ctd-PasswordFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/PasswordFieldSetting
1117	1036	9	129	129	0	1	PermissionChoiceFieldSetting	$Ctd-PermissionChoiceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/PermissionChoiceFieldSetting
1118	1006	9	130	130	0	1	PortalRoot	$Ctd-PortalRoot,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/PortalRoot
1119	1009	9	131	131	0	1	PortalSettings	$Ctd-PortalSettings,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Settings/PortalSettings
1120	1006	9	132	132	0	1	PortletCategory	$Ctd-PortletCategory,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/PortletCategory
1121	1017	9	133	133	0	1	Portlet	$Ctd-Portlet,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Portlet
1122	1006	9	134	134	0	1	SystemFolder	$Ctd-SystemFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder
1123	1122	9	135	135	0	1	Portlets	$Ctd-Portlets,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Portlets
1124	1017	9	136	136	0	1	Post	$Ctd-Post,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/Post
1125	1006	9	137	137	0	1	Posts	$Ctd-Posts,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Posts
1126	1087	9	138	138	0	1	PreviewImage	$Ctd-PreviewImage,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Image/PreviewImage
1127	1006	9	139	139	0	1	ProfileDomain	$Ctd-ProfileDomain,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ProfileDomain
1128	1006	9	140	140	0	1	Profiles	$Ctd-Profiles,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Profiles
1129	1028	9	141	141	0	1	ProjectWorkspace	$Ctd-ProjectWorkspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/ProjectWorkspace
1130	1006	9	142	142	0	1	ProjectWorkspaceFolder	$Ctd-ProjectWorkspaceFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ProjectWorkspaceFolder
1131	1005	9	143	143	0	1	PublicRegistrationConfig	$Ctd-PublicRegistrationConfig,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/PublicRegistrationConfig
1132	1005	9	144	144	0	1	Query	$Ctd-Query,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Query
1133	1026	9	145	145	0	1	ReferenceFieldSetting	$Ctd-ReferenceFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/ReferenceFieldSetting
1134	1005	9	146	146	0	1	User	$Ctd-User,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/User
1135	1134	9	147	147	0	1	RegisteredUser	$Ctd-RegisteredUser,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/User/RegisteredUser
1136	1015	9	148	148	0	1	RegistrationWorkflow	$Ctd-RegistrationWorkflow,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Workflow/RegistrationWorkflow
1137	1011	9	149	149	0	1	Resource	$Ctd-Resource,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/SystemFile/Resource
1138	1122	9	150	150	0	1	Resources	$Ctd-Resources,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Resources
1139	1013	9	151	151	0	1	RssApplication	$Ctd-RssApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/RssApplication
1140	1006	9	152	152	0	1	RuntimeContentContainer	$Ctd-RuntimeContentContainer,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/RuntimeContentContainer
1141	1028	9	153	153	0	1	SalesWorkspace	$Ctd-SalesWorkspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/SalesWorkspace
1142	1006	9	154	154	0	1	SalesWorkspaceFolder	$Ctd-SalesWorkspaceFolder,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SalesWorkspaceFolder
1143	1028	9	155	155	0	1	Site	$Ctd-Site,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Site
1144	1006	9	156	156	0	1	Sites	$Ctd-Sites,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Sites
1145	1122	9	157	157	0	1	Skin	$Ctd-Skin,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Skin
1146	1122	9	158	158	0	1	Skins	$Ctd-Skins,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/Skins
1147	1017	9	159	159	0	1	SliderItem	$Ctd-SliderItem,SliderItem-DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/SliderItem
1148	1005	9	160	160	0	1	Subscription	$Ctd-Subscription,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Subscription
1149	1047	9	161	161	0	1	Survey	$Ctd-Survey,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/Survey
1150	1017	9	162	162	0	1	SurveyItem	$Ctd-SurveyItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/SurveyItem
1151	1047	9	163	163	0	1	SurveyList	$Ctd-SurveyList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/SurveyList
1152	1017	9	164	164	0	1	SurveyListItem	$Ctd-SurveyListItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/SurveyListItem
1153	1005	9	165	165	0	1	Tag	$Ctd-Tag,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Tag
1154	1047	9	166	166	0	1	TaskList	$Ctd-TaskList,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/TaskList
1155	1028	9	167	167	0	1	TeamWorkspace	$Ctd-TeamWorkspace,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TeamWorkspace
1156	1006	9	168	168	0	1	TrashBag	$Ctd-TrashBag,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/TrashBag
1157	1028	9	169	169	0	1	TrashBin	$Ctd-TrashBin,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/TrashBin
1158	1028	9	170	170	0	1	UserProfile	$Ctd-UserProfile,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/UserProfile
1159	1005	9	171	171	0	1	UserSearch	$Ctd-UserSearch,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/UserSearch
1160	1008	9	172	172	0	1	Video	$Ctd-Video,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/Video
1161	1149	9	173	173	0	1	Voting	$Ctd-Voting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList/ItemList/Survey/Voting
1162	1017	9	174	174	0	1	VotingItem	$Ctd-VotingItem,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/VotingItem
1163	1020	9	175	175	0	1	WebContentDemo	$Ctd-WebContentDemo,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/ListItem/WebContent/WebContentDemo
1164	1013	9	176	176	0	1	WebServiceApplication	$Ctd-WebServiceApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/WebServiceApplication
1165	1005	9	177	177	0	1	WikiArticle	$Ctd-WikiArticle,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/WikiArticle
1166	1028	9	178	178	0	1	Wiki	$Ctd-Wiki,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace/Wiki
1167	1008	9	179	179	0	1	WorkflowDefinition	$Ctd-WorkflowDefinition,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/File/WorkflowDefinition
1168	1026	9	180	180	0	1	XmlFieldSetting	$Ctd-XmlFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/XmlFieldSetting
1169	1013	9	181	181	0	1	XsltApplication	$Ctd-XsltApplication,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/Application/XsltApplication
1170	1036	9	182	182	0	1	YesNoFieldSetting	$Ctd-YesNoFieldSetting,DisplayName	/Root/System/Schema/ContentTypes/GenericContent/FieldSettingContent/TextFieldSetting/ShortTextFieldSetting/ChoiceFieldSetting/YesNoFieldSetting
1171	1001	5	183	183	0	1	Aspects	""""	/Root/System/Schema/Aspects
1172	1171	117	184	184	0	1	Summarizable	$Aspect,Summarizable-DisplayName	/Root/System/Schema/Aspects/Summarizable
1173	1000	5	185	185	0	1	AppCache	""""	/Root/System/AppCache
1174	1173	134	186	186	0	1	Events	""""	/Root/System/AppCache/Events
1175	1122	9	187	187	0	1	TestSystemFolder	""""	/Root/System/Schema/ContentTypes/GenericContent/Folder/SystemFolder/TestSystemFolder
";
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
		<PropertyType itemID='25' name='SyncGuid' dataType='String' mapping='1' />
		<PropertyType itemID='26' name='LastSync' dataType='DateTime' mapping='2' />
		<PropertyType itemID='27' name='Watermark' dataType='String' mapping='2' />
		<PropertyType itemID='28' name='PageCount' dataType='Int' mapping='10' />
		<PropertyType itemID='29' name='MimeType' dataType='String' mapping='3' />
		<PropertyType itemID='30' name='Shapes' dataType='Text' mapping='7' />
		<PropertyType itemID='31' name='PageAttributes' dataType='Text' mapping='8' />
		<PropertyType itemID='32' name='GlobalOnly' dataType='Int' mapping='11' />
		<PropertyType itemID='33' name='AppName' dataType='String' mapping='4' />
		<PropertyType itemID='34' name='Disabled' dataType='Int' mapping='12' />
		<PropertyType itemID='35' name='IsModal' dataType='Int' mapping='13' />
		<PropertyType itemID='36' name='Clear' dataType='Int' mapping='14' />
		<PropertyType itemID='37' name='Scenario' dataType='String' mapping='5' />
		<PropertyType itemID='38' name='ActionTypeName' dataType='String' mapping='6' />
		<PropertyType itemID='39' name='StyleHint' dataType='String' mapping='7' />
		<PropertyType itemID='40' name='RequiredPermissions' dataType='String' mapping='8' />
		<PropertyType itemID='41' name='DeepPermissionCheck' dataType='Int' mapping='15' />
		<PropertyType itemID='42' name='IncludeBackUrl' dataType='String' mapping='9' />
		<PropertyType itemID='43' name='CacheControl' dataType='String' mapping='10' />
		<PropertyType itemID='44' name='MaxAge' dataType='String' mapping='11' />
		<PropertyType itemID='45' name='CustomUrlParameters' dataType='String' mapping='12' />
		<PropertyType itemID='46' name='StoredIcon' dataType='String' mapping='13' />
		<PropertyType itemID='47' name='WorkflowStatus' dataType='String' mapping='14' />
		<PropertyType itemID='48' name='WorkflowDefinitionVersion' dataType='String' mapping='15' />
		<PropertyType itemID='49' name='WorkflowInstanceGuid' dataType='String' mapping='16' />
		<PropertyType itemID='50' name='RelatedContent' dataType='Reference' mapping='2' />
		<PropertyType itemID='51' name='RelatedContentTimestamp' dataType='Currency' mapping='1' />
		<PropertyType itemID='52' name='SystemMessages' dataType='Text' mapping='9' />
		<PropertyType itemID='53' name='AllowManualStart' dataType='Int' mapping='16' />
		<PropertyType itemID='54' name='AutostartOnPublished' dataType='Int' mapping='17' />
		<PropertyType itemID='55' name='AutostartOnCreated' dataType='Int' mapping='18' />
		<PropertyType itemID='56' name='AutostartOnChanged' dataType='Int' mapping='19' />
		<PropertyType itemID='57' name='ContentWorkflow' dataType='Int' mapping='20' />
		<PropertyType itemID='58' name='AbortOnRelatedContentChange' dataType='Int' mapping='21' />
		<PropertyType itemID='59' name='OwnerSiteUrl' dataType='String' mapping='17' />
		<PropertyType itemID='60' name='FirstLevelTimeFrame' dataType='String' mapping='18' />
		<PropertyType itemID='61' name='SecondLevelTimeFrame' dataType='String' mapping='19' />
		<PropertyType itemID='62' name='FirstLevelApprover' dataType='Reference' mapping='3' />
		<PropertyType itemID='63' name='SecondLevelApprover' dataType='Reference' mapping='4' />
		<PropertyType itemID='64' name='WaitForAll' dataType='Int' mapping='22' />
		<PropertyType itemID='65' name='StartDate' dataType='DateTime' mapping='3' />
		<PropertyType itemID='66' name='DueDate' dataType='DateTime' mapping='4' />
		<PropertyType itemID='67' name='AssignedTo' dataType='Reference' mapping='5' />
		<PropertyType itemID='68' name='Priority' dataType='String' mapping='20' />
		<PropertyType itemID='69' name='Status' dataType='String' mapping='21' />
		<PropertyType itemID='70' name='TaskCompletion' dataType='Int' mapping='23' />
		<PropertyType itemID='71' name='Comment' dataType='String' mapping='22' />
		<PropertyType itemID='72' name='Result' dataType='String' mapping='23' />
		<PropertyType itemID='73' name='ContentToApprove' dataType='Reference' mapping='6' />
		<PropertyType itemID='74' name='ReviewDate' dataType='DateTime' mapping='5' />
		<PropertyType itemID='75' name='ArchiveDate' dataType='DateTime' mapping='6' />
		<PropertyType itemID='76' name='Subtitle' dataType='String' mapping='24' />
		<PropertyType itemID='77' name='Lead' dataType='Text' mapping='10' />
		<PropertyType itemID='78' name='Body' dataType='Text' mapping='11' />
		<PropertyType itemID='79' name='Pinned' dataType='Int' mapping='24' />
		<PropertyType itemID='80' name='Keywords' dataType='Text' mapping='12' />
		<PropertyType itemID='81' name='Author' dataType='String' mapping='25' />
		<PropertyType itemID='82' name='ImageRef' dataType='Reference' mapping='7' />
		<PropertyType itemID='83' name='ImageData' dataType='Binary' mapping='1' />
		<PropertyType itemID='84' name='ContentListBindings' dataType='Text' mapping='13' />
		<PropertyType itemID='85' name='ContentListDefinition' dataType='Text' mapping='14' />
		<PropertyType itemID='86' name='DefaultView' dataType='String' mapping='26' />
		<PropertyType itemID='87' name='AvailableViews' dataType='Reference' mapping='8' />
		<PropertyType itemID='88' name='AvailableContentTypeFields' dataType='Reference' mapping='9' />
		<PropertyType itemID='89' name='ListEmail' dataType='String' mapping='27' />
		<PropertyType itemID='90' name='ExchangeSubscriptionId' dataType='String' mapping='28' />
		<PropertyType itemID='91' name='OverwriteFiles' dataType='Int' mapping='25' />
		<PropertyType itemID='92' name='GroupAttachments' dataType='String' mapping='29' />
		<PropertyType itemID='93' name='SaveOriginalEmail' dataType='Int' mapping='26' />
		<PropertyType itemID='94' name='IncomingEmailWorkflow' dataType='Reference' mapping='10' />
		<PropertyType itemID='95' name='OnlyFromLocalGroups' dataType='Int' mapping='27' />
		<PropertyType itemID='96' name='InboxFolder' dataType='String' mapping='30' />
		<PropertyType itemID='97' name='OwnerWhenVisitor' dataType='Reference' mapping='11' />
		<PropertyType itemID='98' name='AspectDefinition' dataType='Text' mapping='15' />
		<PropertyType itemID='99' name='FieldSettingContents' dataType='Reference' mapping='12' />
		<PropertyType itemID='100' name='IsActive' dataType='Int' mapping='28' />
		<PropertyType itemID='101' name='IsWallContainer' dataType='Int' mapping='29' />
		<PropertyType itemID='102' name='WorkspaceSkin' dataType='Reference' mapping='13' />
		<PropertyType itemID='103' name='Manager' dataType='Reference' mapping='14' />
		<PropertyType itemID='104' name='Deadline' dataType='DateTime' mapping='7' />
		<PropertyType itemID='105' name='IsCritical' dataType='Int' mapping='30' />
		<PropertyType itemID='106' name='ShowAvatar' dataType='Int' mapping='31' />
		<PropertyType itemID='107' name='PublishedOn' dataType='DateTime' mapping='8' />
		<PropertyType itemID='108' name='LeadingText' dataType='Text' mapping='16' />
		<PropertyType itemID='109' name='BodyText' dataType='Text' mapping='17' />
		<PropertyType itemID='110' name='IsPublished' dataType='Int' mapping='32' />
		<PropertyType itemID='111' name='RegistrationForm' dataType='Reference' mapping='15' />
		<PropertyType itemID='112' name='Location' dataType='String' mapping='31' />
		<PropertyType itemID='113' name='EndDate' dataType='DateTime' mapping='9' />
		<PropertyType itemID='114' name='AllDay' dataType='Int' mapping='33' />
		<PropertyType itemID='115' name='EventUrl' dataType='String' mapping='32' />
		<PropertyType itemID='116' name='RequiresRegistration' dataType='Int' mapping='34' />
		<PropertyType itemID='117' name='OwnerEmail' dataType='String' mapping='33' />
		<PropertyType itemID='118' name='NotificationMode' dataType='String' mapping='34' />
		<PropertyType itemID='119' name='EmailTemplate' dataType='Text' mapping='18' />
		<PropertyType itemID='120' name='EmailTemplateSubmitter' dataType='Text' mapping='19' />
		<PropertyType itemID='121' name='EmailFrom' dataType='String' mapping='35' />
		<PropertyType itemID='122' name='EmailFromSubmitter' dataType='String' mapping='36' />
		<PropertyType itemID='123' name='EmailField' dataType='String' mapping='37' />
		<PropertyType itemID='124' name='MaxParticipants' dataType='Int' mapping='35' />
		<PropertyType itemID='125' name='EventType' dataType='String' mapping='38' />
		<PropertyType itemID='126' name='Make' dataType='String' mapping='39' />
		<PropertyType itemID='127' name='Model' dataType='String' mapping='40' />
		<PropertyType itemID='128' name='Style' dataType='String' mapping='41' />
		<PropertyType itemID='129' name='StartingDate' dataType='DateTime' mapping='10' />
		<PropertyType itemID='130' name='Color' dataType='String' mapping='42' />
		<PropertyType itemID='131' name='EngineSize' dataType='String' mapping='43' />
		<PropertyType itemID='132' name='Power' dataType='String' mapping='44' />
		<PropertyType itemID='133' name='Price' dataType='Currency' mapping='2' />
		<PropertyType itemID='134' name='Confirmed' dataType='Int' mapping='36' />
		<PropertyType itemID='135' name='Link' dataType='Reference' mapping='16' />
		<PropertyType itemID='136' name='Query' dataType='Text' mapping='20' />
		<PropertyType itemID='137' name='EnableAutofilters' dataType='String' mapping='45' />
		<PropertyType itemID='138' name='EnableLifespanFilter' dataType='String' mapping='46' />
		<PropertyType itemID='139' name='SelectionMode' dataType='String' mapping='47' />
		<PropertyType itemID='140' name='OrderingMode' dataType='String' mapping='48' />
		<PropertyType itemID='141' name='ContractId' dataType='String' mapping='49' />
		<PropertyType itemID='142' name='Project' dataType='Reference' mapping='17' />
		<PropertyType itemID='143' name='Language' dataType='String' mapping='50' />
		<PropertyType itemID='144' name='Responsee' dataType='Reference' mapping='18' />
		<PropertyType itemID='145' name='Lawyer' dataType='String' mapping='51' />
		<PropertyType itemID='146' name='RelatedDocs' dataType='Reference' mapping='19' />
		<PropertyType itemID='147' name='WorkflowsRunning' dataType='Int' mapping='37' />
		<PropertyType itemID='148' name='UserAgentPattern' dataType='String' mapping='52' />
		<PropertyType itemID='149' name='StartIndex' dataType='Int' mapping='38' />
		<PropertyType itemID='150' name='ContentVersion' dataType='String' mapping='53' />
		<PropertyType itemID='151' name='From' dataType='String' mapping='54' />
		<PropertyType itemID='152' name='Sent' dataType='DateTime' mapping='11' />
		<PropertyType itemID='153' name='RegistrationFolder' dataType='Reference' mapping='20' />
		<PropertyType itemID='154' name='EmailList' dataType='Text' mapping='21' />
		<PropertyType itemID='155' name='TitleSubmitter' dataType='String' mapping='55' />
		<PropertyType itemID='156' name='AfterSubmitText' dataType='Text' mapping='22' />
		<PropertyType itemID='157' name='Email' dataType='String' mapping='56' />
		<PropertyType itemID='158' name='GuestNumber' dataType='Int' mapping='39' />
		<PropertyType itemID='159' name='Amount' dataType='Currency' mapping='3' />
		<PropertyType itemID='160' name='Date' dataType='DateTime' mapping='12' />
		<PropertyType itemID='161' name='CEO' dataType='Reference' mapping='21' />
		<PropertyType itemID='162' name='BudgetLimit' dataType='Int' mapping='40' />
		<PropertyType itemID='163' name='FinanceEmail' dataType='String' mapping='57' />
		<PropertyType itemID='164' name='Reason' dataType='Text' mapping='23' />
		<PropertyType itemID='165' name='ExpenseClaim' dataType='Reference' mapping='22' />
		<PropertyType itemID='166' name='Sum' dataType='Int' mapping='41' />
		<PropertyType itemID='167' name='EmailForPassword' dataType='String' mapping='58' />
		<PropertyType itemID='168' name='ReplyTo' dataType='Reference' mapping='23' />
		<PropertyType itemID='169' name='PostedBy' dataType='Reference' mapping='24' />
		<PropertyType itemID='170' name='SerialNo' dataType='Int' mapping='42' />
		<PropertyType itemID='171' name='ClassName' dataType='String' mapping='59' />
		<PropertyType itemID='172' name='MethodName' dataType='String' mapping='60' />
		<PropertyType itemID='173' name='Parameters' dataType='Text' mapping='24' />
		<PropertyType itemID='174' name='ListHidden' dataType='Int' mapping='43' />
		<PropertyType itemID='175' name='SiteUrl' dataType='String' mapping='61' />
		<PropertyType itemID='176' name='Members' dataType='Reference' mapping='25' />
		<PropertyType itemID='177' name='HTMLFragment' dataType='Text' mapping='25' />
		<PropertyType itemID='178' name='A' dataType='Int' mapping='44' />
		<PropertyType itemID='179' name='B' dataType='Int' mapping='45' />
		<PropertyType itemID='180' name='StatusCode' dataType='String' mapping='62' />
		<PropertyType itemID='181' name='RedirectUrl' dataType='String' mapping='63' />
		<PropertyType itemID='182' name='Width' dataType='Int' mapping='46' />
		<PropertyType itemID='183' name='Height' dataType='Int' mapping='47' />
		<PropertyType itemID='184' name='DateTaken' dataType='DateTime' mapping='13' />
		<PropertyType itemID='185' name='CoverImage' dataType='Reference' mapping='26' />
		<PropertyType itemID='186' name='ImageType' dataType='String' mapping='64' />
		<PropertyType itemID='187' name='ImageFieldName' dataType='String' mapping='65' />
		<PropertyType itemID='188' name='Stretch' dataType='Int' mapping='48' />
		<PropertyType itemID='189' name='OutputFormat' dataType='String' mapping='66' />
		<PropertyType itemID='190' name='SmoothingMode' dataType='String' mapping='67' />
		<PropertyType itemID='191' name='InterpolationMode' dataType='String' mapping='68' />
		<PropertyType itemID='192' name='PixelOffsetMode' dataType='String' mapping='69' />
		<PropertyType itemID='193' name='ResizeTypeMode' dataType='String' mapping='70' />
		<PropertyType itemID='194' name='CropVAlign' dataType='String' mapping='71' />
		<PropertyType itemID='195' name='CropHAlign' dataType='String' mapping='72' />
		<PropertyType itemID='196' name='KPIData' dataType='Text' mapping='26' />
		<PropertyType itemID='197' name='Url' dataType='String' mapping='73' />
		<PropertyType itemID='198' name='Template' dataType='Reference' mapping='27' />
		<PropertyType itemID='199' name='FilterXml' dataType='Text' mapping='27' />
		<PropertyType itemID='200' name='QueryTop' dataType='Int' mapping='49' />
		<PropertyType itemID='201' name='QuerySkip' dataType='Int' mapping='50' />
		<PropertyType itemID='202' name='Icon' dataType='String' mapping='74' />
		<PropertyType itemID='203' name='Columns' dataType='Text' mapping='28' />
		<PropertyType itemID='204' name='SortBy' dataType='String' mapping='75' />
		<PropertyType itemID='205' name='GroupBy' dataType='String' mapping='76' />
		<PropertyType itemID='206' name='Flat' dataType='Int' mapping='51' />
		<PropertyType itemID='207' name='MainScenario' dataType='String' mapping='77' />
		<PropertyType itemID='208' name='MemoType' dataType='String' mapping='78' />
		<PropertyType itemID='209' name='SeeAlso' dataType='Reference' mapping='28' />
		<PropertyType itemID='210' name='Subject' dataType='String' mapping='79' />
		<PropertyType itemID='211' name='SenderAddress' dataType='String' mapping='80' />
		<PropertyType itemID='212' name='CompanyName' dataType='String' mapping='81' />
		<PropertyType itemID='213' name='OrderFormId' dataType='String' mapping='82' />
		<PropertyType itemID='214' name='CompanySeat' dataType='Text' mapping='29' />
		<PropertyType itemID='215' name='RepresentedBy' dataType='String' mapping='83' />
		<PropertyType itemID='216' name='ContactEmailAddress' dataType='String' mapping='84' />
		<PropertyType itemID='217' name='ContactPhoneNr' dataType='String' mapping='85' />
		<PropertyType itemID='218' name='MetaTitle' dataType='String' mapping='86' />
		<PropertyType itemID='219' name='MetaDescription' dataType='Text' mapping='30' />
		<PropertyType itemID='220' name='MetaAuthors' dataType='String' mapping='87' />
		<PropertyType itemID='221' name='CustomMeta' dataType='Text' mapping='31' />
		<PropertyType itemID='222' name='PageTemplateNode' dataType='Reference' mapping='29' />
		<PropertyType itemID='223' name='PersonalizationSettings' dataType='Binary' mapping='2' />
		<PropertyType itemID='224' name='TemporaryPortletInfo' dataType='Text' mapping='32' />
		<PropertyType itemID='225' name='TextExtract' dataType='Text' mapping='33' />
		<PropertyType itemID='226' name='SmartUrl' dataType='String' mapping='88' />
		<PropertyType itemID='227' name='PageSkin' dataType='Reference' mapping='30' />
		<PropertyType itemID='228' name='HasTemporaryPortletInfo' dataType='Int' mapping='52' />
		<PropertyType itemID='229' name='IsExternal' dataType='Int' mapping='53' />
		<PropertyType itemID='230' name='OuterUrl' dataType='String' mapping='89' />
		<PropertyType itemID='231' name='PageId' dataType='String' mapping='90' />
		<PropertyType itemID='232' name='NodeName' dataType='String' mapping='91' />
		<PropertyType itemID='233' name='MasterPageNode' dataType='Reference' mapping='31' />
		<PropertyType itemID='234' name='TypeName' dataType='String' mapping='92' />
		<PropertyType itemID='235' name='JournalId' dataType='Int' mapping='54' />
		<PropertyType itemID='236' name='PostType' dataType='Int' mapping='55' />
		<PropertyType itemID='237' name='SharedContent' dataType='Reference' mapping='32' />
		<PropertyType itemID='238' name='PostDetails' dataType='Text' mapping='34' />
		<PropertyType itemID='239' name='Completion' dataType='Currency' mapping='4' />
		<PropertyType itemID='240' name='SecurityGroups' dataType='Reference' mapping='33' />
		<PropertyType itemID='241' name='DefaultDomainPath' dataType='Reference' mapping='34' />
		<PropertyType itemID='242' name='UserTypeName' dataType='String' mapping='93' />
		<PropertyType itemID='243' name='DuplicateErrorMessage' dataType='Text' mapping='35' />
		<PropertyType itemID='244' name='IsBodyHtml' dataType='Int' mapping='56' />
		<PropertyType itemID='245' name='ActivationEnabled' dataType='Int' mapping='57' />
		<PropertyType itemID='246' name='ActivationEmailTemplate' dataType='Text' mapping='36' />
		<PropertyType itemID='247' name='ActivationSuccessTemplate' dataType='Text' mapping='37' />
		<PropertyType itemID='248' name='AlreadyActivatedMessage' dataType='Text' mapping='38' />
		<PropertyType itemID='249' name='MailSubjectTemplate' dataType='Text' mapping='39' />
		<PropertyType itemID='250' name='MailFrom' dataType='String' mapping='94' />
		<PropertyType itemID='251' name='AdminEmailAddress' dataType='String' mapping='95' />
		<PropertyType itemID='252' name='RegistrationSuccessTemplate' dataType='Text' mapping='40' />
		<PropertyType itemID='253' name='ResetPasswordTemplate' dataType='Text' mapping='41' />
		<PropertyType itemID='254' name='ResetPasswordSubjectTemplate' dataType='String' mapping='96' />
		<PropertyType itemID='255' name='ResetPasswordSuccessfulTemplate' dataType='Text' mapping='42' />
		<PropertyType itemID='256' name='ChangePasswordUserInterfacePath' dataType='String' mapping='97' />
		<PropertyType itemID='257' name='ChangePasswordSuccessfulMessage' dataType='String' mapping='98' />
		<PropertyType itemID='258' name='ForgottenPasswordUserInterfacePath' dataType='String' mapping='99' />
		<PropertyType itemID='259' name='NewRegistrationContentView' dataType='String' mapping='100' />
		<PropertyType itemID='260' name='EditProfileContentView' dataType='String' mapping='101' />
		<PropertyType itemID='261' name='AutoGeneratePassword' dataType='Int' mapping='58' />
		<PropertyType itemID='262' name='DisableCreatedUser' dataType='Int' mapping='59' />
		<PropertyType itemID='263' name='IsUniqueEmail' dataType='Int' mapping='60' />
		<PropertyType itemID='264' name='AutomaticLogon' dataType='Int' mapping='61' />
		<PropertyType itemID='265' name='ChangePasswordPagePath' dataType='Reference' mapping='35' />
		<PropertyType itemID='266' name='ChangePasswordRestrictedText' dataType='Text' mapping='43' />
		<PropertyType itemID='267' name='AlreadyRegisteredUserMessage' dataType='Text' mapping='44' />
		<PropertyType itemID='268' name='UpdateProfileSuccessTemplate' dataType='Text' mapping='45' />
		<PropertyType itemID='269' name='EmailNotValid' dataType='Text' mapping='46' />
		<PropertyType itemID='270' name='NoEmailGiven' dataType='Text' mapping='47' />
		<PropertyType itemID='271' name='ActivateByAdmin' dataType='Int' mapping='62' />
		<PropertyType itemID='272' name='ActivateEmailSubject' dataType='String' mapping='102' />
		<PropertyType itemID='273' name='ActivateEmailTemplate' dataType='Text' mapping='48' />
		<PropertyType itemID='274' name='ActivateAdmins' dataType='Reference' mapping='36' />
		<PropertyType itemID='275' name='Enabled' dataType='Int' mapping='63' />
		<PropertyType itemID='276' name='Domain' dataType='String' mapping='103' />
		<PropertyType itemID='277' name='FullName' dataType='String' mapping='104' />
		<PropertyType itemID='278' name='OldPasswords' dataType='Text' mapping='49' />
		<PropertyType itemID='279' name='PasswordHash' dataType='String' mapping='105' />
		<PropertyType itemID='280' name='LoginName' dataType='String' mapping='106' />
		<PropertyType itemID='281' name='FollowedWorkspaces' dataType='Reference' mapping='37' />
		<PropertyType itemID='282' name='JobTitle' dataType='String' mapping='107' />
		<PropertyType itemID='283' name='Captcha' dataType='String' mapping='108' />
		<PropertyType itemID='284' name='Department' dataType='String' mapping='109' />
		<PropertyType itemID='285' name='Languages' dataType='String' mapping='110' />
		<PropertyType itemID='286' name='Phone' dataType='String' mapping='111' />
		<PropertyType itemID='287' name='Gender' dataType='String' mapping='112' />
		<PropertyType itemID='288' name='MaritalStatus' dataType='String' mapping='113' />
		<PropertyType itemID='289' name='BirthDate' dataType='DateTime' mapping='14' />
		<PropertyType itemID='290' name='Education' dataType='Text' mapping='50' />
		<PropertyType itemID='291' name='TwitterAccount' dataType='String' mapping='114' />
		<PropertyType itemID='292' name='FacebookURL' dataType='String' mapping='115' />
		<PropertyType itemID='293' name='LinkedInURL' dataType='String' mapping='116' />
		<PropertyType itemID='294' name='ResetKey' dataType='String' mapping='117' />
		<PropertyType itemID='295' name='ActivationId' dataType='String' mapping='118' />
		<PropertyType itemID='296' name='Activated' dataType='Int' mapping='64' />
		<PropertyType itemID='297' name='SecurityQuestion' dataType='String' mapping='119' />
		<PropertyType itemID='298' name='SecurityAnswer' dataType='String' mapping='120' />
		<PropertyType itemID='299' name='UserName' dataType='String' mapping='121' />
		<PropertyType itemID='300' name='RegistrationType' dataType='String' mapping='122' />
		<PropertyType itemID='301' name='Downloads' dataType='Currency' mapping='5' />
		<PropertyType itemID='302' name='Customer' dataType='Text' mapping='51' />
		<PropertyType itemID='303' name='ExpectedRevenue' dataType='Currency' mapping='6' />
		<PropertyType itemID='304' name='ChanceOfWinning' dataType='Currency' mapping='7' />
		<PropertyType itemID='305' name='Contacts' dataType='Text' mapping='52' />
		<PropertyType itemID='306' name='Notes' dataType='Text' mapping='53' />
		<PropertyType itemID='307' name='ContractSigned' dataType='Int' mapping='65' />
		<PropertyType itemID='308' name='ContractSignedDate' dataType='DateTime' mapping='15' />
		<PropertyType itemID='309' name='PendingUserLang' dataType='String' mapping='123' />
		<PropertyType itemID='310' name='EnableClientBasedCulture' dataType='Int' mapping='66' />
		<PropertyType itemID='311' name='EnableUserBasedCulture' dataType='Int' mapping='67' />
		<PropertyType itemID='312' name='UrlList' dataType='Text' mapping='54' />
		<PropertyType itemID='313' name='StartPage' dataType='Reference' mapping='38' />
		<PropertyType itemID='314' name='LoginPage' dataType='Reference' mapping='39' />
		<PropertyType itemID='315' name='SiteSkin' dataType='Reference' mapping='40' />
		<PropertyType itemID='316' name='DenyCrossSiteAccess' dataType='Int' mapping='68' />
		<PropertyType itemID='317' name='NewSkin' dataType='Int' mapping='69' />
		<PropertyType itemID='318' name='Background' dataType='Reference' mapping='41' />
		<PropertyType itemID='319' name='YouTubeBackground' dataType='String' mapping='124' />
		<PropertyType itemID='320' name='VerticalAlignment' dataType='String' mapping='125' />
		<PropertyType itemID='321' name='HorizontalAlignment' dataType='String' mapping='126' />
		<PropertyType itemID='322' name='OuterCallToActionButton' dataType='String' mapping='127' />
		<PropertyType itemID='323' name='InnerCallToActionButton' dataType='Text' mapping='55' />
		<PropertyType itemID='324' name='ContentPath' dataType='String' mapping='128' />
		<PropertyType itemID='325' name='UserPath' dataType='String' mapping='129' />
		<PropertyType itemID='326' name='UserEmail' dataType='String' mapping='130' />
		<PropertyType itemID='327' name='UserId' dataType='Currency' mapping='8' />
		<PropertyType itemID='328' name='Frequency' dataType='String' mapping='131' />
		<PropertyType itemID='329' name='LandingPage' dataType='Reference' mapping='42' />
		<PropertyType itemID='330' name='PageContentView' dataType='Reference' mapping='43' />
		<PropertyType itemID='331' name='InvalidSurveyPage' dataType='Reference' mapping='44' />
		<PropertyType itemID='332' name='MailTemplatePage' dataType='Reference' mapping='45' />
		<PropertyType itemID='333' name='EnableMoreFilling' dataType='Int' mapping='70' />
		<PropertyType itemID='334' name='EnableNotificationMail' dataType='Int' mapping='71' />
		<PropertyType itemID='335' name='Evaluators' dataType='Reference' mapping='46' />
		<PropertyType itemID='336' name='EvaluatedBy' dataType='Reference' mapping='47' />
		<PropertyType itemID='337' name='EvaluatedAt' dataType='DateTime' mapping='16' />
		<PropertyType itemID='338' name='Evaluation' dataType='Text' mapping='56' />
		<PropertyType itemID='339' name='MailSubject' dataType='String' mapping='132' />
		<PropertyType itemID='340' name='AdminEmailTemplate' dataType='Text' mapping='57' />
		<PropertyType itemID='341' name='SubmitterEmailTemplate' dataType='Text' mapping='58' />
		<PropertyType itemID='342' name='OnlySingleResponse' dataType='Int' mapping='72' />
		<PropertyType itemID='343' name='RawJson' dataType='Text' mapping='59' />
		<PropertyType itemID='344' name='IntroText' dataType='Text' mapping='60' />
		<PropertyType itemID='345' name='OutroText' dataType='Text' mapping='61' />
		<PropertyType itemID='346' name='OutroRedirectLink' dataType='Reference' mapping='48' />
		<PropertyType itemID='347' name='Description2' dataType='String' mapping='133' />
		<PropertyType itemID='348' name='BlackListPath' dataType='Text' mapping='62' />
		<PropertyType itemID='349' name='KeepUntil' dataType='DateTime' mapping='17' />
		<PropertyType itemID='350' name='OriginalPath' dataType='String' mapping='134' />
		<PropertyType itemID='351' name='WorkspaceId' dataType='Int' mapping='73' />
		<PropertyType itemID='352' name='WorkspaceRelativePath' dataType='String' mapping='135' />
		<PropertyType itemID='353' name='MinRetentionTime' dataType='Int' mapping='74' />
		<PropertyType itemID='354' name='SizeQuota' dataType='Int' mapping='75' />
		<PropertyType itemID='355' name='BagCapacity' dataType='Int' mapping='76' />
		<PropertyType itemID='356' name='Search' dataType='String' mapping='136' />
		<PropertyType itemID='357' name='IsResultVisibleBefore' dataType='Int' mapping='77' />
		<PropertyType itemID='358' name='ResultPageContentView' dataType='Reference' mapping='49' />
		<PropertyType itemID='359' name='VotingPageContentView' dataType='Reference' mapping='50' />
		<PropertyType itemID='360' name='CannotSeeResultContentView' dataType='Reference' mapping='51' />
		<PropertyType itemID='361' name='LandingPageContentView' dataType='Reference' mapping='52' />
		<PropertyType itemID='362' name='RelatedImage' dataType='Reference' mapping='53' />
		<PropertyType itemID='363' name='Header' dataType='Text' mapping='63' />
		<PropertyType itemID='364' name='Details' dataType='String' mapping='137' />
		<PropertyType itemID='365' name='ContentLanguage' dataType='String' mapping='138' />
		<PropertyType itemID='366' name='WikiArticleText' dataType='Text' mapping='64' />
		<PropertyType itemID='367' name='ReferencedWikiTitles' dataType='Text' mapping='65' />
		<PropertyType itemID='368' name='DeleteInstanceAfterFinished' dataType='String' mapping='139' />
		<PropertyType itemID='369' name='AssignableToContentList' dataType='Int' mapping='78' />
		<PropertyType itemID='370' name='Cacheable' dataType='Int' mapping='79' />
		<PropertyType itemID='371' name='CacheableForLoggedInUser' dataType='Int' mapping='80' />
		<PropertyType itemID='372' name='CacheByHost' dataType='Int' mapping='81' />
		<PropertyType itemID='373' name='CacheByPath' dataType='Int' mapping='82' />
		<PropertyType itemID='374' name='CacheByParams' dataType='Int' mapping='83' />
		<PropertyType itemID='375' name='CacheByLanguage' dataType='Int' mapping='84' />
		<PropertyType itemID='376' name='SlidingExpirationMinutes' dataType='Int' mapping='85' />
		<PropertyType itemID='377' name='AbsoluteExpiration' dataType='Int' mapping='86' />
		<PropertyType itemID='378' name='CustomCacheKey' dataType='String' mapping='140' />
		<PropertyType itemID='379' name='OmitXmlDeclaration' dataType='Int' mapping='87' />
		<PropertyType itemID='380' name='ResponseEncoding' dataType='String' mapping='141' />
		<PropertyType itemID='381' name='WithChildren' dataType='Int' mapping='88' />
	</UsedPropertyTypes>
	<NodeTypeHierarchy>
		<NodeType itemID='11' name='JournalNode' className='SenseNet.Portal.Workspaces.JournalNode' />
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
				<PropertyType name='ImageRef' />
				<PropertyType name='ImageData' />
				<PropertyType name='Manager' />
				<PropertyType name='Language' />
				<PropertyType name='Email' />
				<PropertyType name='Enabled' />
				<PropertyType name='Domain' />
				<PropertyType name='FullName' />
				<PropertyType name='OldPasswords' />
				<PropertyType name='PasswordHash' />
				<PropertyType name='LoginName' />
				<PropertyType name='FollowedWorkspaces' />
				<PropertyType name='JobTitle' />
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
				<NodeType itemID='112' name='RegisteredUser' className='SenseNet.ContentRepository.User'>
					<PropertyType name='ResetKey' />
					<PropertyType name='ActivationId' />
					<PropertyType name='Activated' />
					<PropertyType name='SecurityQuestion' />
					<PropertyType name='SecurityAnswer' />
				</NodeType>
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
				<NodeType itemID='48' name='TrashBag' className='SenseNet.ContentRepository.TrashBag'>
					<PropertyType name='Link' />
					<PropertyType name='KeepUntil' />
					<PropertyType name='OriginalPath' />
					<PropertyType name='WorkspaceId' />
					<PropertyType name='WorkspaceRelativePath' />
				</NodeType>
				<NodeType itemID='47' name='Sites' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='46' name='SalesWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='45' name='RuntimeContentContainer' className='SenseNet.ContentRepository.RuntimeContentContainer' />
				<NodeType itemID='44' name='ProjectWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='43' name='Profiles' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='42' name='ProfileDomain' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='41' name='Posts' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='40' name='PortletCategory' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='39' name='OtherWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='38' name='KPIDatasources' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='37' name='KPIDatasource' className='SenseNet.ContentRepository.KPIDatasource'>
					<PropertyType name='KPIData' />
				</NodeType>
				<NodeType itemID='36' name='FieldControlTemplates' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='35' name='ExpenseClaim' className='SenseNet.ContentRepository.ExpenseClaim' />
				<NodeType itemID='34' name='Email' className='SenseNet.ContentRepository.Folder'>
					<PropertyType name='Body' />
					<PropertyType name='From' />
					<PropertyType name='Sent' />
				</NodeType>
				<NodeType itemID='33' name='DocumentWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='32' name='DiscussionForum' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='31' name='Device' className='SenseNet.ApplicationModel.Device'>
					<PropertyType name='UserAgentPattern' />
				</NodeType>
				<NodeType itemID='30' name='ContentViews' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='29' name='SmartFolder' className='SenseNet.ContentRepository.SmartFolder'>
					<PropertyType name='Query' />
					<PropertyType name='EnableAutofilters' />
					<PropertyType name='EnableLifespanFilter' />
					<NodeType itemID='129' name='ContentRotator' className='SenseNet.ContentRepository.ContentRotator'>
						<PropertyType name='SelectionMode' />
						<PropertyType name='OrderingMode' />
					</NodeType>
				</NodeType>
				<NodeType itemID='28' name='Workspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
					<PropertyType name='IsActive' />
					<PropertyType name='IsWallContainer' />
					<PropertyType name='WorkspaceSkin' />
					<PropertyType name='Manager' />
					<PropertyType name='Deadline' />
					<PropertyType name='IsCritical' />
					<NodeType itemID='128' name='Wiki' className='SenseNet.ContentRepository.Workspaces.Workspace' />
					<NodeType itemID='127' name='UserProfile' className='SenseNet.ContentRepository.UserProfile' />
					<NodeType itemID='126' name='TrashBin' className='SenseNet.ContentRepository.TrashBin'>
						<PropertyType name='MinRetentionTime' />
						<PropertyType name='SizeQuota' />
						<PropertyType name='BagCapacity' />
					</NodeType>
					<NodeType itemID='125' name='TeamWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace' />
					<NodeType itemID='124' name='Site' className='SenseNet.Portal.Site'>
						<PropertyType name='Language' />
						<PropertyType name='PendingUserLang' />
						<PropertyType name='EnableClientBasedCulture' />
						<PropertyType name='EnableUserBasedCulture' />
						<PropertyType name='UrlList' />
						<PropertyType name='StartPage' />
						<PropertyType name='LoginPage' />
						<PropertyType name='SiteSkin' />
						<PropertyType name='DenyCrossSiteAccess' />
					</NodeType>
					<NodeType itemID='123' name='SalesWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
						<PropertyType name='StartDate' />
						<PropertyType name='Completion' />
						<PropertyType name='Customer' />
						<PropertyType name='ExpectedRevenue' />
						<PropertyType name='ChanceOfWinning' />
						<PropertyType name='Contacts' />
						<PropertyType name='Notes' />
						<PropertyType name='ContractSigned' />
						<PropertyType name='ContractSignedDate' />
					</NodeType>
					<NodeType itemID='122' name='ProjectWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
						<PropertyType name='Completion' />
					</NodeType>
					<NodeType itemID='121' name='DocumentWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace' />
					<NodeType itemID='120' name='Blog' className='SenseNet.ContentRepository.Workspaces.Workspace'>
						<PropertyType name='ShowAvatar' />
					</NodeType>
				</NodeType>
				<NodeType itemID='27' name='ContentList' className='SenseNet.ContentRepository.ContentList'>
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
					<NodeType itemID='119' name='Library' className='SenseNet.ContentRepository.ContentList'>
						<NodeType itemID='159' name='ImageLibrary' className='SenseNet.ContentRepository.ContentList'>
							<PropertyType name='CoverImage' />
						</NodeType>
						<NodeType itemID='158' name='DocumentLibrary' className='SenseNet.ContentRepository.ContentList' />
					</NodeType>
					<NodeType itemID='118' name='ItemList' className='SenseNet.ContentRepository.ContentList'>
						<NodeType itemID='157' name='TaskList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='156' name='SurveyList' className='SenseNet.Portal.Portlets.ContentHandlers.SurveyList'>
							<PropertyType name='EmailFrom' />
							<PropertyType name='EmailField' />
							<PropertyType name='EmailList' />
							<PropertyType name='LandingPage' />
							<PropertyType name='EnableNotificationMail' />
							<PropertyType name='MailSubject' />
							<PropertyType name='AdminEmailTemplate' />
							<PropertyType name='SubmitterEmailTemplate' />
							<PropertyType name='OnlySingleResponse' />
							<PropertyType name='RawJson' />
							<PropertyType name='IntroText' />
							<PropertyType name='OutroText' />
							<PropertyType name='OutroRedirectLink' />
						</NodeType>
						<NodeType itemID='155' name='Survey' className='SenseNet.ContentRepository.Survey'>
							<PropertyType name='SenderAddress' />
							<PropertyType name='LandingPage' />
							<PropertyType name='PageContentView' />
							<PropertyType name='InvalidSurveyPage' />
							<PropertyType name='MailTemplatePage' />
							<PropertyType name='EnableMoreFilling' />
							<PropertyType name='EnableNotificationMail' />
							<PropertyType name='Evaluators' />
							<NodeType itemID='165' name='Voting' className='SenseNet.ContentRepository.Voting'>
								<PropertyType name='IsResultVisibleBefore' />
								<PropertyType name='ResultPageContentView' />
								<PropertyType name='VotingPageContentView' />
								<PropertyType name='CannotSeeResultContentView' />
								<PropertyType name='LandingPageContentView' />
							</NodeType>
						</NodeType>
						<NodeType itemID='154' name='MemoList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='153' name='LinkList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='152' name='ForumTopic' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='151' name='Form' className='SenseNet.Portal.Portlets.ContentHandlers.Form'>
							<PropertyType name='EmailTemplate' />
							<PropertyType name='EmailTemplateSubmitter' />
							<PropertyType name='EmailFrom' />
							<PropertyType name='EmailFromSubmitter' />
							<PropertyType name='EmailField' />
							<PropertyType name='EmailList' />
							<PropertyType name='TitleSubmitter' />
							<PropertyType name='AfterSubmitText' />
							<NodeType itemID='164' name='EventRegistrationForm' className='SenseNet.Portal.Portlets.ContentHandlers.Form' />
						</NodeType>
						<NodeType itemID='150' name='EventList' className='SenseNet.ContentRepository.ContentList'>
							<PropertyType name='RegistrationFolder' />
						</NodeType>
						<NodeType itemID='149' name='CustomList' className='SenseNet.ContentRepository.ContentList' />
					</NodeType>
					<NodeType itemID='117' name='Aspect' className='SenseNet.ContentRepository.Aspect'>
						<PropertyType name='AspectDefinition' />
						<PropertyType name='FieldSettingContents' />
					</NodeType>
				</NodeType>
				<NodeType itemID='26' name='ArticleSection' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='25' name='ADFolder' className='SenseNet.ContentRepository.Security.ADSync.ADFolder'>
					<PropertyType name='SyncGuid' />
					<PropertyType name='LastSync' />
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
					<NodeType itemID='168' name='TestNode' className='SenseNet.SearchImpl.Tests.Implementations.TestNode' />
					<NodeType itemID='116' name='Skins' className='SenseNet.ContentRepository.SystemFolder' />
					<NodeType itemID='115' name='Skin' className='SenseNet.ContentRepository.SystemFolder'>
						<PropertyType name='NewSkin' />
					</NodeType>
					<NodeType itemID='114' name='Resources' className='SenseNet.ContentRepository.SystemFolder' />
					<NodeType itemID='113' name='Portlets' className='SenseNet.ContentRepository.SystemFolder' />
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
			<NodeType itemID='24' name='WikiArticle' className='SenseNet.Portal.WikiArticle'>
				<PropertyType name='WikiArticleText' />
				<PropertyType name='ReferencedWikiTitles' />
			</NodeType>
			<NodeType itemID='23' name='UserSearch' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='Search' />
			</NodeType>
			<NodeType itemID='22' name='Tag' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='Description2' />
				<PropertyType name='BlackListPath' />
			</NodeType>
			<NodeType itemID='21' name='Subscription' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='IsActive' />
				<PropertyType name='Language' />
				<PropertyType name='UserName' />
				<PropertyType name='ContentPath' />
				<PropertyType name='UserPath' />
				<PropertyType name='UserEmail' />
				<PropertyType name='UserId' />
				<PropertyType name='Frequency' />
			</NodeType>
			<NodeType itemID='20' name='Query' className='SenseNet.ContentRepository.QueryContent'>
				<PropertyType name='Query' />
			</NodeType>
			<NodeType itemID='19' name='PublicRegistrationConfig' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='SecurityGroups' />
				<PropertyType name='DefaultDomainPath' />
				<PropertyType name='UserTypeName' />
				<PropertyType name='DuplicateErrorMessage' />
				<PropertyType name='IsBodyHtml' />
				<PropertyType name='ActivationEnabled' />
				<PropertyType name='ActivationEmailTemplate' />
				<PropertyType name='ActivationSuccessTemplate' />
				<PropertyType name='AlreadyActivatedMessage' />
				<PropertyType name='MailSubjectTemplate' />
				<PropertyType name='MailFrom' />
				<PropertyType name='AdminEmailAddress' />
				<PropertyType name='RegistrationSuccessTemplate' />
				<PropertyType name='ResetPasswordTemplate' />
				<PropertyType name='ResetPasswordSubjectTemplate' />
				<PropertyType name='ResetPasswordSuccessfulTemplate' />
				<PropertyType name='ChangePasswordUserInterfacePath' />
				<PropertyType name='ChangePasswordSuccessfulMessage' />
				<PropertyType name='ForgottenPasswordUserInterfacePath' />
				<PropertyType name='NewRegistrationContentView' />
				<PropertyType name='EditProfileContentView' />
				<PropertyType name='AutoGeneratePassword' />
				<PropertyType name='DisableCreatedUser' />
				<PropertyType name='IsUniqueEmail' />
				<PropertyType name='AutomaticLogon' />
				<PropertyType name='ChangePasswordPagePath' />
				<PropertyType name='ChangePasswordRestrictedText' />
				<PropertyType name='AlreadyRegisteredUserMessage' />
				<PropertyType name='UpdateProfileSuccessTemplate' />
				<PropertyType name='EmailNotValid' />
				<PropertyType name='NoEmailGiven' />
				<PropertyType name='ActivateByAdmin' />
				<PropertyType name='ActivateEmailSubject' />
				<PropertyType name='ActivateEmailTemplate' />
				<PropertyType name='ActivateAdmins' />
			</NodeType>
			<NodeType itemID='18' name='NotificationConfig' className='SenseNet.Messaging.NotificationConfig'>
				<PropertyType name='Body' />
				<PropertyType name='Subject' />
				<PropertyType name='SenderAddress' />
			</NodeType>
			<NodeType itemID='17' name='ContentLink' className='SenseNet.ContentRepository.ContentLink'>
				<PropertyType name='Link' />
			</NodeType>
			<NodeType itemID='16' name='FieldSettingContent' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
				<NodeType itemID='111' name='XmlFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='110' name='ReferenceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='109' name='PageBreakFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='108' name='NullFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='107' name='IntegerFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='106' name='HyperLinkFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='105' name='DateTimeFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='104' name='NumberFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
					<NodeType itemID='148' name='CurrencyFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				</NodeType>
				<NodeType itemID='103' name='TextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
					<NodeType itemID='147' name='LongTextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
					<NodeType itemID='146' name='ShortTextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
						<NodeType itemID='163' name='PasswordFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
						<NodeType itemID='162' name='ChoiceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
							<NodeType itemID='167' name='YesNoFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
							<NodeType itemID='166' name='PermissionChoiceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
						</NodeType>
					</NodeType>
				</NodeType>
				<NodeType itemID='102' name='BinaryFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
			</NodeType>
			<NodeType itemID='15' name='ListItem' className='SenseNet.ContentRepository.GenericContent'>
				<NodeType itemID='101' name='VotingItem' className='SenseNet.ContentRepository.VotingItem' />
				<NodeType itemID='100' name='SurveyListItem' className='SenseNet.ContentRepository.GenericContent' />
				<NodeType itemID='99' name='SurveyItem' className='SenseNet.ContentRepository.SurveyItem'>
					<PropertyType name='EvaluatedBy' />
					<PropertyType name='EvaluatedAt' />
					<PropertyType name='Evaluation' />
				</NodeType>
				<NodeType itemID='98' name='SliderItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Background' />
					<PropertyType name='YouTubeBackground' />
					<PropertyType name='VerticalAlignment' />
					<PropertyType name='HorizontalAlignment' />
					<PropertyType name='OuterCallToActionButton' />
					<PropertyType name='InnerCallToActionButton' />
				</NodeType>
				<NodeType itemID='97' name='Post' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='JournalId' />
					<PropertyType name='PostType' />
					<PropertyType name='SharedContent' />
					<PropertyType name='PostDetails' />
				</NodeType>
				<NodeType itemID='96' name='Portlet' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='ImageRef' />
					<PropertyType name='ImageData' />
					<PropertyType name='TypeName' />
				</NodeType>
				<NodeType itemID='95' name='Memo' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Date' />
					<PropertyType name='MemoType' />
					<PropertyType name='SeeAlso' />
				</NodeType>
				<NodeType itemID='94' name='Link' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Url' />
				</NodeType>
				<NodeType itemID='93' name='Like' className='SenseNet.ContentRepository.GenericContent' />
				<NodeType itemID='92' name='ForumEntry' className='SenseNet.Portal.DiscussionForum.ForumEntry'>
					<PropertyType name='ReplyTo' />
					<PropertyType name='PostedBy' />
					<PropertyType name='SerialNo' />
				</NodeType>
				<NodeType itemID='91' name='ExpenseClaimItem' className='SenseNet.ContentRepository.ExpenseClaimItem'>
					<PropertyType name='ImageRef' />
					<PropertyType name='ImageData' />
					<PropertyType name='Amount' />
					<PropertyType name='Date' />
				</NodeType>
				<NodeType itemID='90' name='FormItem' className='SenseNet.Portal.Portlets.ContentHandlers.FormItem'>
					<NodeType itemID='145' name='EventRegistrationFormItem' className='SenseNet.Portal.Portlets.ContentHandlers.EventRegistrationFormItem'>
						<PropertyType name='Email' />
						<PropertyType name='GuestNumber' />
					</NodeType>
				</NodeType>
				<NodeType itemID='89' name='CustomListItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='WorkflowsRunning' />
				</NodeType>
				<NodeType itemID='88' name='ConfirmationItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Confirmed' />
				</NodeType>
				<NodeType itemID='87' name='Comment' className='SenseNet.ContentRepository.GenericContent' />
				<NodeType itemID='86' name='Car' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Make' />
					<PropertyType name='Model' />
					<PropertyType name='Style' />
					<PropertyType name='StartingDate' />
					<PropertyType name='Color' />
					<PropertyType name='EngineSize' />
					<PropertyType name='Power' />
					<PropertyType name='Price' />
				</NodeType>
				<NodeType itemID='85' name='CalendarEvent' className='SenseNet.ContentRepository.CalendarEvent'>
					<PropertyType name='StartDate' />
					<PropertyType name='Lead' />
					<PropertyType name='RegistrationForm' />
					<PropertyType name='Location' />
					<PropertyType name='EndDate' />
					<PropertyType name='AllDay' />
					<PropertyType name='EventUrl' />
					<PropertyType name='RequiresRegistration' />
					<PropertyType name='OwnerEmail' />
					<PropertyType name='NotificationMode' />
					<PropertyType name='EmailTemplate' />
					<PropertyType name='EmailTemplateSubmitter' />
					<PropertyType name='EmailFrom' />
					<PropertyType name='EmailFromSubmitter' />
					<PropertyType name='EmailField' />
					<PropertyType name='MaxParticipants' />
					<PropertyType name='EventType' />
				</NodeType>
				<NodeType itemID='84' name='BlogPost' className='SenseNet.Portal.BlogPost'>
					<PropertyType name='PublishedOn' />
					<PropertyType name='LeadingText' />
					<PropertyType name='BodyText' />
					<PropertyType name='IsPublished' />
				</NodeType>
				<NodeType itemID='83' name='WebContent' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='ReviewDate' />
					<PropertyType name='ArchiveDate' />
					<NodeType itemID='144' name='WebContentDemo' className='SenseNet.ContentRepository.GenericContent'>
						<PropertyType name='Subtitle' />
						<PropertyType name='Body' />
						<PropertyType name='Keywords' />
						<PropertyType name='Author' />
						<PropertyType name='RelatedImage' />
						<PropertyType name='Header' />
						<PropertyType name='Details' />
						<PropertyType name='ContentLanguage' />
					</NodeType>
					<NodeType itemID='143' name='HTMLContent' className='SenseNet.ContentRepository.GenericContent'>
						<PropertyType name='HTMLFragment' />
					</NodeType>
					<NodeType itemID='142' name='Article' className='SenseNet.ContentRepository.GenericContent'>
						<PropertyType name='Subtitle' />
						<PropertyType name='Lead' />
						<PropertyType name='Body' />
						<PropertyType name='Pinned' />
						<PropertyType name='Keywords' />
						<PropertyType name='Author' />
						<PropertyType name='ImageRef' />
						<PropertyType name='ImageData' />
					</NodeType>
				</NodeType>
				<NodeType itemID='82' name='Task' className='SenseNet.ContentRepository.Task'>
					<PropertyType name='StartDate' />
					<PropertyType name='DueDate' />
					<PropertyType name='AssignedTo' />
					<PropertyType name='Priority' />
					<PropertyType name='Status' />
					<PropertyType name='TaskCompletion' />
					<NodeType itemID='141' name='ApprovalWorkflowTask' className='SenseNet.ContentRepository.Task'>
						<PropertyType name='Comment' />
						<PropertyType name='Result' />
						<PropertyType name='ContentToApprove' />
						<NodeType itemID='161' name='ExpenseClaimWorkflowTask' className='SenseNet.ContentRepository.Task'>
							<PropertyType name='Reason' />
							<PropertyType name='ExpenseClaim' />
							<PropertyType name='Sum' />
						</NodeType>
					</NodeType>
				</NodeType>
			</NodeType>
			<NodeType itemID='14' name='Workflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
				<PropertyType name='WorkflowStatus' />
				<PropertyType name='WorkflowDefinitionVersion' />
				<PropertyType name='WorkflowInstanceGuid' />
				<PropertyType name='RelatedContent' />
				<PropertyType name='RelatedContentTimestamp' />
				<PropertyType name='SystemMessages' />
				<PropertyType name='AllowManualStart' />
				<PropertyType name='AutostartOnPublished' />
				<PropertyType name='AutostartOnCreated' />
				<PropertyType name='AutostartOnChanged' />
				<PropertyType name='ContentWorkflow' />
				<PropertyType name='AbortOnRelatedContentChange' />
				<PropertyType name='OwnerSiteUrl' />
				<NodeType itemID='81' name='RegistrationWorkflow' className='SenseNet.Workflow.RegistrationWorkflow'>
					<PropertyType name='Email' />
					<PropertyType name='FullName' />
					<PropertyType name='PasswordHash' />
					<PropertyType name='UserName' />
					<PropertyType name='RegistrationType' />
				</NodeType>
				<NodeType itemID='80' name='MailProcessorWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase' />
				<NodeType itemID='79' name='ForgottenPasswordWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
					<PropertyType name='EmailForPassword' />
				</NodeType>
				<NodeType itemID='78' name='ExpenseClaimWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
					<PropertyType name='CEO' />
					<PropertyType name='BudgetLimit' />
					<PropertyType name='FinanceEmail' />
				</NodeType>
				<NodeType itemID='77' name='DocumentPreviewWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
					<PropertyType name='StartIndex' />
					<PropertyType name='ContentVersion' />
				</NodeType>
				<NodeType itemID='76' name='ApprovalWorkflow' className='SenseNet.Workflow.ApprovalWorkflow'>
					<PropertyType name='FirstLevelTimeFrame' />
					<PropertyType name='SecondLevelTimeFrame' />
					<PropertyType name='FirstLevelApprover' />
					<PropertyType name='SecondLevelApprover' />
					<PropertyType name='WaitForAll' />
				</NodeType>
			</NodeType>
			<NodeType itemID='13' name='Application' className='SenseNet.ApplicationModel.Application'>
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
				<NodeType itemID='75' name='XsltApplication' className='SenseNet.Portal.Handlers.XsltApplication'>
					<PropertyType name='Binary' />
					<PropertyType name='MimeType' />
					<PropertyType name='Cacheable' />
					<PropertyType name='CacheableForLoggedInUser' />
					<PropertyType name='CacheByHost' />
					<PropertyType name='CacheByPath' />
					<PropertyType name='CacheByParams' />
					<PropertyType name='CacheByLanguage' />
					<PropertyType name='SlidingExpirationMinutes' />
					<PropertyType name='AbsoluteExpiration' />
					<PropertyType name='CustomCacheKey' />
					<PropertyType name='OmitXmlDeclaration' />
					<PropertyType name='ResponseEncoding' />
					<PropertyType name='WithChildren' />
				</NodeType>
				<NodeType itemID='74' name='WebServiceApplication' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Binary' />
				</NodeType>
				<NodeType itemID='73' name='RssApplication' className='SenseNet.Services.RssApplication' />
				<NodeType itemID='72' name='Webform' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Binary' />
					<NodeType itemID='140' name='Page' className='SenseNet.Portal.Page'>
						<PropertyType name='Comment' />
						<PropertyType name='Keywords' />
						<PropertyType name='MetaTitle' />
						<PropertyType name='MetaDescription' />
						<PropertyType name='MetaAuthors' />
						<PropertyType name='CustomMeta' />
						<PropertyType name='PageTemplateNode' />
						<PropertyType name='PersonalizationSettings' />
						<PropertyType name='TemporaryPortletInfo' />
						<PropertyType name='TextExtract' />
						<PropertyType name='SmartUrl' />
						<PropertyType name='PageSkin' />
						<PropertyType name='HasTemporaryPortletInfo' />
						<PropertyType name='IsExternal' />
						<PropertyType name='OuterUrl' />
						<PropertyType name='PageId' />
						<PropertyType name='NodeName' />
					</NodeType>
				</NodeType>
				<NodeType itemID='71' name='ImgResizeApplication' className='SenseNet.Portal.ApplicationModel.ImgResizeApplication'>
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
				<NodeType itemID='70' name='HttpStatusApplication' className='SenseNet.Portal.AppModel.HttpStatusApplication'>
					<PropertyType name='StatusCode' />
					<PropertyType name='RedirectUrl' />
				</NodeType>
				<NodeType itemID='69' name='HttpHandlerApplication' className='SenseNet.Portal.Handlers.HttpHandlerApplication' />
				<NodeType itemID='68' name='HttpEndpointDemoContent' className='SenseNet.ContentRepository.HttpEndpointDemoContent'>
					<PropertyType name='A' />
					<PropertyType name='B' />
				</NodeType>
				<NodeType itemID='67' name='GoogleSitemap' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Query' />
					<PropertyType name='ListHidden' />
					<PropertyType name='SiteUrl' />
				</NodeType>
				<NodeType itemID='66' name='GenericODataApplication' className='SenseNet.Portal.ApplicationModel.GenericODataApplication'>
					<PropertyType name='ClassName' />
					<PropertyType name='MethodName' />
					<PropertyType name='Parameters' />
				</NodeType>
				<NodeType itemID='65' name='ExportToCsvApplication' className='SenseNet.Services.ExportToCsvApplication' />
				<NodeType itemID='64' name='CaptchaImageApplication' className='SenseNet.Portal.UI.Controls.Captcha.CaptchaImageApplication' />
				<NodeType itemID='63' name='BackupIndexHandler' className='SenseNet.Portal.Handlers.BackupIndexHandler' />
				<NodeType itemID='62' name='ApplicationOverride' className='SenseNet.ApplicationModel.Application' />
			</NodeType>
			<NodeType itemID='12' name='File' className='SenseNet.ContentRepository.File'>
				<PropertyType name='Binary' />
				<PropertyType name='Watermark' />
				<PropertyType name='PageCount' />
				<PropertyType name='MimeType' />
				<PropertyType name='Shapes' />
				<PropertyType name='PageAttributes' />
				<NodeType itemID='61' name='WorkflowDefinition' className='SenseNet.Workflow.WorkflowDefinitionHandler'>
					<PropertyType name='ContentWorkflow' />
					<PropertyType name='AbortOnRelatedContentChange' />
					<PropertyType name='DeleteInstanceAfterFinished' />
					<PropertyType name='AssignableToContentList' />
				</NodeType>
				<NodeType itemID='60' name='Video' className='SenseNet.ContentRepository.File'>
					<PropertyType name='Keywords' />
				</NodeType>
				<NodeType itemID='59' name='OrderForm' className='SenseNet.ContentRepository.File'>
					<PropertyType name='CompanyName' />
					<PropertyType name='OrderFormId' />
					<PropertyType name='CompanySeat' />
					<PropertyType name='RepresentedBy' />
					<PropertyType name='ContactEmailAddress' />
					<PropertyType name='ContactPhoneNr' />
				</NodeType>
				<NodeType itemID='58' name='UserControl' className='SenseNet.ContentRepository.File'>
					<NodeType itemID='139' name='ViewBase' className='SenseNet.Portal.UI.ContentListViews.Handlers.ViewBase'>
						<PropertyType name='EnableAutofilters' />
						<PropertyType name='EnableLifespanFilter' />
						<PropertyType name='Template' />
						<PropertyType name='FilterXml' />
						<PropertyType name='QueryTop' />
						<PropertyType name='QuerySkip' />
						<PropertyType name='Icon' />
						<NodeType itemID='160' name='ListView' className='SenseNet.Portal.UI.ContentListViews.Handlers.ListView'>
							<PropertyType name='Columns' />
							<PropertyType name='SortBy' />
							<PropertyType name='GroupBy' />
							<PropertyType name='Flat' />
							<PropertyType name='MainScenario' />
						</NodeType>
					</NodeType>
				</NodeType>
				<NodeType itemID='57' name='Image' className='SenseNet.ContentRepository.Image'>
					<PropertyType name='Keywords' />
					<PropertyType name='Width' />
					<PropertyType name='Height' />
					<PropertyType name='DateTaken' />
					<NodeType itemID='138' name='PreviewImage' className='SenseNet.ContentRepository.Image' />
				</NodeType>
				<NodeType itemID='56' name='HtmlTemplate' className='SenseNet.Portal.UI.HtmlTemplate' />
				<NodeType itemID='55' name='FieldControlTemplate' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='54' name='ExecutableFile' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='53' name='DynamicJsonContent' className='SenseNet.Portal.Handlers.DynamicJsonContent' />
				<NodeType itemID='52' name='Contract' className='SenseNet.ContentRepository.File'>
					<PropertyType name='Keywords' />
					<PropertyType name='ContractId' />
					<PropertyType name='Project' />
					<PropertyType name='Language' />
					<PropertyType name='Responsee' />
					<PropertyType name='Lawyer' />
					<PropertyType name='RelatedDocs' />
				</NodeType>
				<NodeType itemID='51' name='ContentView' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='50' name='SystemFile' className='SenseNet.ContentRepository.File'>
					<NodeType itemID='137' name='Resource' className='SenseNet.ContentRepository.i18n.Resource'>
						<PropertyType name='Downloads' />
					</NodeType>
					<NodeType itemID='136' name='PageTemplate' className='SenseNet.Portal.PageTemplate'>
						<PropertyType name='MasterPageNode' />
					</NodeType>
					<NodeType itemID='135' name='MasterPage' className='SenseNet.Portal.MasterPage' />
					<NodeType itemID='134' name='ApplicationCacheFile' className='SenseNet.ContentRepository.ApplicationCacheFile' />
				</NodeType>
				<NodeType itemID='49' name='Settings' className='SenseNet.ContentRepository.Settings'>
					<PropertyType name='GlobalOnly' />
					<NodeType itemID='133' name='PortalSettings' className='SenseNet.Portal.PortalSettings' />
					<NodeType itemID='132' name='LoggingSettings' className='SenseNet.ContentRepository.LoggingSettings' />
					<NodeType itemID='131' name='IndexingSettings' className='SenseNet.Search.IndexingSettings' />
					<NodeType itemID='130' name='ADSettings' className='SenseNet.ContentRepository.Security.ADSync.ADSettings' />
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
