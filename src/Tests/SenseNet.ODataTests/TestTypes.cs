using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.OData;
using Task = System.Threading.Tasks.Task;
// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

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
        #region Methods for general tests

        [ODataFunction]
        [ContentTypes(N.CT.User, N.CT.Group, "OrgUnit")]
        [AllowedRoles(N.R.Administrators, "Editors")]
        [RequiredPolicies("Policy1")]
        [RequiredPermissions("See, Run")]
        [Scenario("Scenario1, Scenario2")]
        [Scenario("Scenario2", "Scenario3, Scenario4")]
        public static object[] Op1(Content content,
            string a, int b, bool c, float d, decimal e, double f)
        {
            return new object[] { a, b, c, d, e, f };
        }

        [ODataAction]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType, N.CT.File)] // Causes no content type check ("File" is redundant).
        [AllowedRoles(N.R.Administrators, "Editors", "Editors,Visitor")]
        [RequiredPolicies("Policy1,Policy2", "Policy2,Policy3")]
        [RequiredPermissions("P1, P2", "P3")]
        public static object[] Op2(Content content,
            string a = null, int b = 0, bool c = false, float d = 0f, decimal e = 0m, double f = 0d)
        {
            return new object[] { a, b, c, d, e, f };
        }

        [ODataFunction]
        // There is no ContentTypes. This means: expected contentType is "GenericContent".
        [RequiredPolicies("Policy2")]
        public static string Op3(Content content)
        {
            return "Called";
        }

        [ODataFunction]
        public static JObject Op4(Content content, JObject a)
        {
            return a;
        }

        [ODataFunction]
        public static object[] Op5(Content content, object[] a)
        {
            return a;
        }

        [ODataFunction(Description = "Lorem ipsum ...")] // Default icon: "Application"
        public static void Op6(Content content)
        {

        }
        [ODataFunction(Icon = "icon42")]
        public static void Op7(Content content)
        {

        }
        [ODataFunction(Description = "Lorem ipsum ...", Icon = "Application")]
        public static void Op8(Content content)
        {

        }
        [ODataFunction("Op9_Renamed", Description = "Lorem ipsum ...", Icon = "icon94")]
        public static void Op9(Content content)
        {

        }
        [ODataFunction]
        [RequiredPolicies("ContentNameMustBeRoot")]
        public static void Op10(Content content)
        {

        }

        #endregion

        #region Methods for enumerable parameter tests

        [ODataFunction] public static string[] Array_String(Content content, string[] a) => a;
        [ODataFunction] public static int[] Array_Int(Content content, int[] a) => a;
        [ODataFunction] public static long[] Array_Long(Content content, long[] a) => a;
        [ODataFunction] public static bool[] Array_Bool(Content content, bool[] a) => a;
        [ODataFunction] public static float[] Array_Float(Content content, float[] a) => a;
        [ODataFunction] public static double[] Array_Double(Content content, double[] a) => a;
        [ODataFunction] public static decimal[] Array_Decimal(Content content, decimal[] a) => a;

        [ODataFunction] public static string[] Optional_Array_String(Content content, string[] a = null) => a;
        [ODataFunction] public static int[] Optional_Array_Int(Content content, int[] a = null) => a;
        [ODataFunction] public static long[] Optional_Array_Long(Content content, long[] a = null) => a;
        [ODataFunction] public static bool[] Optional_Array_Bool(Content content, bool[] a = null) => a;
        [ODataFunction] public static float[] Optional_Array_Float(Content content, float[] a = null) => a;
        [ODataFunction] public static double[] Optional_Array_Double(Content content, double[] a = null) => a;
        [ODataFunction] public static decimal[] Optional_Array_Decimal(Content content, decimal[] a = null) => a;

        [ODataFunction] public static List<string> List_String(Content content, List<string> a) => a;
        [ODataFunction] public static List<int> List_Int(Content content, List<int> a) => a;
        [ODataFunction] public static List<long> List_Long(Content content, List<long> a) => a;
        [ODataFunction] public static List<bool> List_Bool(Content content, List<bool> a) => a;
        [ODataFunction] public static List<float> List_Float(Content content, List<float> a) => a;
        [ODataFunction] public static List<double> List_Double(Content content, List<double> a) => a;
        [ODataFunction] public static List<decimal> List_Decimal(Content content, List<decimal> a) => a;

        [ODataFunction] public static List<string> Optional_List_String(Content content, List<string> a = null) => a;
        [ODataFunction] public static List<int> Optional_List_Int(Content content, List<int> a = null) => a;
        [ODataFunction] public static List<long> Optional_List_Long(Content content, List<long> a = null) => a;
        [ODataFunction] public static List<bool> Optional_List_Bool(Content content, List<bool> a = null) => a;
        [ODataFunction] public static List<float> Optional_List_Float(Content content, List<float> a = null) => a;
        [ODataFunction] public static List<double> Optional_List_Double(Content content, List<double> a = null) => a;
        [ODataFunction] public static List<decimal> Optional_List_Decimal(Content content, List<decimal> a = null) => a;

        [ODataFunction] public static IEnumerable<string> Enumerable_String(Content content, IEnumerable<string> a) => a;
        [ODataFunction] public static IEnumerable<int> Enumerable_Int(Content content, IEnumerable<int> a) => a;
        [ODataFunction] public static IEnumerable<long> Enumerable_Long(Content content, IEnumerable<long> a) => a;
        [ODataFunction] public static IEnumerable<bool> Enumerable_Bool(Content content, IEnumerable<bool> a) => a;
        [ODataFunction] public static IEnumerable<float> Enumerable_Float(Content content, IEnumerable<float> a) => a;
        [ODataFunction] public static IEnumerable<double> Enumerable_Double(Content content, IEnumerable<double> a) => a;
        [ODataFunction] public static IEnumerable<decimal> Enumerable_Decimal(Content content, IEnumerable<decimal> a) => a;

        [ODataFunction] public static IEnumerable<string> Optional_Enumerable_String(Content content, IEnumerable<string> a = null) => a;
        [ODataFunction] public static IEnumerable<int> Optional_Enumerable_Int(Content content, IEnumerable<int> a = null) => a;
        [ODataFunction] public static IEnumerable<long> Optional_Enumerable_Long(Content content, IEnumerable<long> a = null) => a;
        [ODataFunction] public static IEnumerable<bool> Optional_Enumerable_Bool(Content content, IEnumerable<bool> a = null) => a;
        [ODataFunction] public static IEnumerable<float> Optional_Enumerable_Float(Content content, IEnumerable<float> a = null) => a;
        [ODataFunction] public static IEnumerable<double> Optional_Enumerable_Double(Content content, IEnumerable<double> a = null) => a;
        [ODataFunction] public static IEnumerable<decimal> Optional_Enumerable_Decimal(Content content, IEnumerable<decimal> a = null) => a;

        [ODataFunction] public static IEnumerable<string> ODataArray_String(Content content, ODataArray<string> a) => a;
        [ODataFunction] public static IEnumerable<int> ODataArray_Int(Content content, ODataArray<int> a) => a;
        [ODataFunction] public static IEnumerable<long> ODataArray_Long(Content content, ODataArray<long> a) => a;
        [ODataFunction] public static IEnumerable<bool> ODataArray_Bool(Content content, ODataArray<bool> a) => a;
        [ODataFunction] public static IEnumerable<float> ODataArray_Float(Content content, ODataArray<float> a) => a;
        [ODataFunction] public static IEnumerable<double> ODataArray_Double(Content content, ODataArray<double> a) => a;
        [ODataFunction] public static IEnumerable<decimal> ODataArray_Decimal(Content content, ODataArray<decimal> a) => a;

        [ODataFunction] public static IEnumerable<string> Optional_ODataArray_String(Content content, ODataArray<string> a = null) => a;
        [ODataFunction] public static IEnumerable<int> Optional_ODataArray_Int(Content content, ODataArray<int> a = null) => a;
        [ODataFunction] public static IEnumerable<long> Optional_ODataArray_Long(Content content, ODataArray<long> a = null) => a;
        [ODataFunction] public static IEnumerable<bool> Optional_ODataArray_Bool(Content content, ODataArray<bool> a = null) => a;
        [ODataFunction] public static IEnumerable<float> Optional_ODataArray_Float(Content content, ODataArray<float> a = null) => a;
        [ODataFunction] public static IEnumerable<double> Optional_ODataArray_Double(Content content, ODataArray<double> a = null) => a;
        [ODataFunction] public static IEnumerable<decimal> Optional_ODataArray_Decimal(Content content, ODataArray<decimal> a = null) => a;

        [ODataFunction] public static IEnumerable<ODataArrayTests.TestItem> ODataArray_TestItemArray(Content content, ODataArrayTests.TestItemArray a) => a;
        [ODataFunction] public static IEnumerable<ODataArrayTests.TestItem> Optional_ODataArray_TestItemArray(Content content, ODataArrayTests.TestItemArray a) => a;
        #endregion

        #region Methods for real calls

        [ODataFunction]
        public static string Authorization_None(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        [ODataFunction]
        [AllowedRoles(N.R.Administrators)]
        public static string AuthorizedByRole_Administrators(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        [ODataFunction]
        [AllowedRoles(N.R.Visitor)]
        public static string AuthorizedByRole_Visitor(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }
        [ODataFunction]
        [AllowedRoles(N.R.All)]
        public static string AuthorizedByRole_All(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }
        [ODataFunction]
        [AllowedRoles("All")]
        public static string AuthorizedByRole_All2(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        [ODataFunction]
        [RequiredPermissions(N.P.Open)]
        public static string AuthorizedByPermission(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        [ODataFunction]
        [RequiredPolicies("VisitorAllowed,AdminDenied")]
        public static string AuthorizedByPolicy(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        [ODataFunction]
        [RequiredPolicies("UnknownPolicy")]
        public static string AuthorizedByPolicy_Error(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        public const string GoodMethodName = "GoodMethodName";
        [ODataFunction(GoodMethodName)]
        public static string WrongMethodName(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }

        [ODataFunction]
        public static string SensitiveMethodName(Content content, string a)
        {
            return MethodBase.GetCurrentMethod().Name + "-" + a;
        }


        [ODataFunction]
        public static Task<string> AsyncMethod(Content content, string a = null)
        {
            return Task.FromResult(MethodBase.GetCurrentMethod().Name + "-" + (a ?? "[NULL]"));
        }

        #endregion

        [ODataFunction]
        [AllowedRoles(N.R.All)]
        public static string FunctionForQueryStringTest(Content content, string a, int b)
        {
            return $"{MethodBase.GetCurrentMethod().Name}-{a}-{b}";
        }

        [ODataAction]
        [AllowedRoles(N.R.All)]
        public static string ActionForQueryStringTest(Content content, string a, int b)
        {
            return $"{MethodBase.GetCurrentMethod().Name}-{a}-{b}";
        }

    }
}
