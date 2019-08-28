// ReSharper disable CheckNamespace

using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class SenseNetServicesInitialData : IRepositoryDataFile
    {
        public string PropertyTypes => @"
  Id| DataType  | Mapping| Name
----- ----------- -------- ---------------
   1| Binary    |       0| Binary
   2| Int       |       0| VersioningMode
";

        public string NodeTypes => @"
  Id| Name                          | ParentName                    | ClassName                                                   | Properties
----- ------------------------------- ------------------------------- ------------------------------------------------------------- ------------------------------------------
   9| ContentType                   | <null>                        | SenseNet.ContentRepository.Schema.ContentType               | [Binary]
  10| GenericContent                | <null>                        | SenseNet.ContentRepository.GenericContent                   | [VersioningMode]
   1| Folder                        | GenericContent                | SenseNet.ContentRepository.Folder                           | [VersioningMode]
   2| Group                         | GenericContent                | SenseNet.ContentRepository.Group                            | []
   3| User                          | GenericContent                | SenseNet.ContentRepository.User                             | []
   4| PortalRoot                    | Folder                        | SenseNet.ContentRepository.PortalRoot                       | []
   5| SystemFolder                  | Folder                        | SenseNet.ContentRepository.SystemFolder                     | []
   6| Domains                       | Folder                        | SenseNet.ContentRepository.Folder                           | []
   7| Domain                        | Folder                        | SenseNet.ContentRepository.Domain                           | []
   8| OrganizationalUnit            | Folder                        | SenseNet.ContentRepository.OrganizationalUnit               | []
";

        public string Nodes => @"
NodeId| TypeId| Parent|  Index| MinorV| MajorV| IsSystem| Creator| Modifier| Owner | Name                                    | DisplayName                                       | Path
------- ------- -------  ------ ------- ------- --------- -------- --------- ------- ----------------------------------------- --------------------------------------------------- -------------------------------------
     1|      3|      5|      0|      1|      1|    False|       1|        1|      1| Admin                                   | """"                                              | /Root/IMS/BuiltIn/Portal/Admin
     2|      4|      0|      1|      2|      2|    False|       1|        1|      1| Root                                    | """"                                              | /Root
     3|      6|      2|      3|      3|      3|    False|       1|        1|      1| IMS                                     | Users and Groups                                  | /Root/IMS
     4|      7|      3|      0|      4|      4|    False|       1|        1|      1| BuiltIn                                 | """"                                              | /Root/IMS/BuiltIn
     5|      8|      4|      0|      5|      5|    False|       1|        1|      1| Portal                                  | """"                                              | /Root/IMS/BuiltIn/Portal
     6|      3|      5|      4|      6|      6|    False|       1|        1|      1| Visitor                                 | """"                                              | /Root/IMS/BuiltIn/Portal/Visitor
     7|      2|      5|      2|      7|      7|    False|       1|        1|      1| Administrators                          | """"                                              | /Root/IMS/BuiltIn/Portal/Administrators
     8|      2|      5|      3|      8|      8|    False|       1|        1|      1| Everyone                                | """"                                              | /Root/IMS/BuiltIn/Portal/Everyone
     9|      2|      5|      5|      9|      9|    False|       1|        1|      1| Owners                                  | """"                                              | /Root/IMS/BuiltIn/Portal/Owners
    10|      3|      5|      7|     10|     10|    False|       1|        1|      1| Somebody                                | """"                                              | /Root/IMS/BuiltIn/Portal/Somebody
    11|      2|      5|      7|     11|     11|    False|       1|        1|      1| Operators                               | """"                                              | /Root/IMS/BuiltIn/Portal/Operators
    12|      3|      5|      8|     12|     12|    False|       1|        1|      1| Startup                                 | """"                                              | /Root/IMS/BuiltIn/Portal/Startup
  1000|      5|      2|      3|     13|     13|     True|       1|        1|      1| System                                  | """"                                              | /Root/System
  1001|      5|   1000|      1|     14|     14|     True|       1|        1|      1| Schema                                  | Schema                                            | /Root/System/Schema
  1002|      5|   1001|      1|     15|     15|     True|       1|        1|      1| ContentTypes                            | ContentTypes                                      | /Root/System/Schema/ContentTypes
  1003|      5|   1000|      2|     16|     16|     True|       1|        1|      1| Settings                                | Settings                                          | /Root/System/Settings
";

        public string Versions => @"
VersionId| NodeId| Creator| Modifier|  Version
---------- ------- -------- --------- ---------
        1|      1|       1|        1|  V1.0.A
        2|      2|       1|        1|  V1.0.A
        3|      3|       1|        1|  V1.0.A
        4|      4|       1|        1|  V1.0.A
        5|      5|       1|        1|  V1.0.A
        6|      6|       1|        1|  V1.0.A
        7|      7|       1|        1|  V1.0.A
        8|      8|       1|        1|  V1.0.A
        9|      9|       1|        1|  V1.0.A
       10|     10|       1|        1|  V1.0.A
       11|     11|       1|        1|  V1.0.A
       12|     12|       1|        1|  V1.0.A
       13|   1000|       1|        1|  V1.0.A
       14|   1001|       1|        1|  V1.0.A
       15|   1002|       1|        1|  V1.0.A
       16|   1003|       1|        1|  V1.0.A
";

        public string DynamicData => string.Empty;
        public IDictionary<string, string> ContentTypeDefinitions { get; } = new Dictionary<string, string>();
        public IDictionary<string, string> Blobs { get; } = new Dictionary<string, string>();
        public IList<string> Permissions { get; } = new List<string>();
    }
}
