using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.OData;

namespace SenseNet.ODataTests
{
    internal class Elephant
    {
        public int Snout { get; set; }
        public int Height { get; set; }
    }
    internal class Spaceship
    {
        public string Name { get; set; }
        public string Class { get; set; }
        public int Length { get; set; }
    }

    public class TestOperations
    {
        [ODataFunction]
        [RequiredPermission("See, Run")]
        [SnAuthorize(Role = "Administrators,Editors")]
        //UNDONE: Extend and use AppModel's ScenarioAttribute
        //[Scenario("Scenario1, Scenario2")]
        //[Scenario(Scenario = "Scenario2, Scenario3")]
        [ContentType("User, Group")]
        [ContentType(ContentTypeName = "OrgUnit")]
        public static object[] Op1(Content content,
            string a, int b, bool c, float d, decimal e, double f)
        {
            return new object[] { a, b, c, d, e, f };
        }

        [ODataAction]
        [SnAuthorize(Policy = "Policy1")]
        [RequiredPermission("P1, P2")]
        [RequiredPermission(Permission = "P3")]
        public static object[] Op2(Content content,
            string a = null, int b = 0, bool c = false, float d = 0f, decimal e = 0m, double f = 0d)
        {
            return new object[] { a, b, c, d, e, f };
        }

        [ODataFunction]
        [SnAuthorize("Policy2")]
        public static string Op3(Content content)
        {
            return "Called";
        }
    }
}
