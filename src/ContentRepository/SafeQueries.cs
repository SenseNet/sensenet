using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    /// <summary>Holds safe queries in public static readonly string properties.</summary>
    public class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"</summary>
        public static string AllDevices { get { return "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"; } }
        /// <summary>Returns with the following query: "+TypeIs:Aspect +Name:@0 .AUTOFILTERS:OFF .COUNTONLY"</summary>
        public static string AspectExists { get { return "+TypeIs:Aspect +Name:@0 .AUTOFILTERS:OFF .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+InTree:@0"</summary>
        public static string InTree { get { return "+InTree:@0"; } }
        /// <summary>Returns with the following query: "InTree:@0 .SORT:Path"</summary>
        public static string InTreeOrderByPath { get { return "InTree:@0 .SORT:Path"; } }

        /// <summary>Returns with the following query: "+TypeIs:@0"</summary>
        public static string TypeIs { get { return "+TypeIs:@0"; } }
        /// <summary>Returns with the following query: "+InFolder:@0"</summary>
        public static string InFolder { get { return "+InFolder:@0"; } }
        /// <summary>Returns with the following query: "+InFolder:@0 +TypeIs:@1"</summary>
        public static string InFolderAndTypeIs { get { return "+InFolder:@0 +TypeIs:@1"; } }

        /// <summary>Returns with the following query: "+InFolder:@0 .COUNTONLY"</summary>
        public static string InFolderCountOnly { get { return "+InFolder:@0 .COUNTONLY"; } }
        /// <summary>Returns with the following query: "+InFolder:@0 +TypeIs:@1 .COUNTONLY"</summary>
        public static string InFolderAndTypeIsCountOnly { get { return "+InFolder:@0 +TypeIs:@1 .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:@1"</summary>
        public static string InTreeAndTypeIs { get { return "+InTree:@0 +TypeIs:@1"; } }
        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:@1 +Name:@2"</summary>
        public static string InTreeAndTypeIsAndName { get { return "+InTree:@0 +TypeIs:@1 +Name:@2"; } }

        /// <summary>Returns with the following query: "+InTree:@0 .COUNTONLY"</summary>
        public static string InTreeCountOnly { get { return "+InTree:@0 .COUNTONLY"; } }
        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:@1 .COUNTONLY"</summary>
        public static string InTreeAndTypeIsCountOnly { get { return "+InTree:@0 +TypeIs:@1 .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+TypeIs:@0 +Name:@1"</summary>
        public static string TypeIsAndName { get { return "+TypeIs:@0 +Name:@1"; } }

        /// <summary>Returns with the following query: "+TypeIs:Settings +Name:@0 -Id:@1 +InTree:@2 .AUTOFILTERS:OFF .COUNTONLY"</summary>
        public static string SettingsByNameAndSubtree { get { return "+TypeIs:Settings +Name:@0 -Id:@1 +InTree:@2 .AUTOFILTERS:OFF .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+TypeIs:Settings +GlobalOnly:true +Name:@0 +InTree:@1 .AUTOFILTERS:OFF .COUNTONLY"</summary>
        public static string SettingsGlobalOnly { get { return "+TypeIs:Settings +GlobalOnly:true +Name:@0 +InTree:@1 .AUTOFILTERS:OFF .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +Name:@1 +TypeIs:(User Group)"</summary>
        public static string UserOrGroupByName { get { return "+InTree:@0 +Name:@1 +TypeIs:(User Group)"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +((TypeIs:User AND (Name:@1 OR LoginName:@1) OR (TypeIs:Group AND Name:@1))"</summary>
        public static string UserOrGroupByLoginName { get { return "+InTree:@0 +((TypeIs:User AND (Name:(@1 @2) OR LoginName:(@1 @2))) OR (TypeIs:Group AND (Name:(@1 @2))))"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:User +LoginName:@1"</summary>
        public static string UsersByLoginName { get { return "+InTree:@0 +TypeIs:User +LoginName:@1"; } }

        /// <summary>Returns with the following query: "TypeIs:File AND InTree:@0 .SORT:CreationDate"</summary>
        public static string FilesInTree { get { return "TypeIs:File AND InTree:@0 .SORT:CreationDate"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:OrganizationalUnit .SORT:Path"</summary>
        public static string OrgUnitsInTree { get { return "+InTree:@0 +TypeIs:OrganizationalUnit .SORT:Path"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:(Group OrganizationalUnit) .SORT:Path"</summary>
        public static string SecurityGroupsInTree { get { return "+InTree:@0 +TypeIs:(Group OrganizationalUnit) .SORT:Path"; } }

        /// <summary>Returns with the following query: "+TypeIs:Group +Members:@0 .SORT:Path"</summary>
        public static string SecurityGroupsWhereThisIsMember { get { return "+TypeIs:(Group OrganizationalUnit) +Members:@0 .SORT:Path"; } }

        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:(User Group OrganizationalUnit) .SORT:Path"</summary>
        public static string SecurityIdentitiesInTree { get { return "+InTree:@0 +TypeIs:(User Group OrganizationalUnit) .SORT:Path"; } }

        /// <summary>Returns with the following query: "+TypeIs:Workflow +RelatedContent:@0 .AUTOFILTERS:OFF"</summary>
        public static string WorkflowsByRelatedContent { get { return "+TypeIs:Workflow +RelatedContent:@0 .AUTOFILTERS:OFF"; } }
    }
}
