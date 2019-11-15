using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using SenseNet.Search.Querying;
using SenseNet.Tests.Accessors;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataOperationMethodTests : ODataTestBase
    {
        /* ====================================================================== OPERATION INFO TESTS */

        [TestMethod]
        public void OD_MBO_GetInfo_0Prm()
        {
            ODataTest(() =>
            {
                var method = new TestMethodInfo("fv1", null, null);

                var info = AddMethod(method);

                Assert.IsNull(info);
            });
        }
        [TestMethod]
        public void OD_MBO_GetInfo_1Prm_Invalid()
        {
            ODataTest(() =>
            {
                var method = new TestMethodInfo("fv1", "string a", null);

                var info = AddMethod(method);

                Assert.IsNull(info);
            });
        }
        [TestMethod]
        public void OD_MBO_GetInfo_1Prm_Optional()
        {
            ODataTest(() =>
            {
                var method = new TestMethodInfo("fv1", null, "Content content");

                var info = AddMethod(method);

                Assert.IsNull(info);
            });
        }
        [TestMethod]
        public void OD_MBO_GetInfo_1Prm()
        {
            ODataTest(() =>
            {
                var method = new TestMethodInfo("fv1", "Content content", null);

                var info = AddMethod(method);

                Assert.AreEqual(0, info.RequiredParameterNames.Length);
                Assert.AreEqual(0, info.RequiredParameterTypes.Length);
                Assert.AreEqual(0, info.OptionalParameterNames.Length);
                Assert.AreEqual(0, info.OptionalParameterTypes.Length);
            });
        }

        [TestMethod]
        public void OD_MBO_GetInfo_5Prm2()
        {
            ODataTest(() =>
            {
                var method = new TestMethodInfo("fv1",
                    "Content content, string a, int b",
                    "string c, object d");

                // ACTION
                var info = AddMethod(method);

                // ASSERT
                Assert.AreEqual(method, info.Method);
                Assert.AreEqual(2, info.OptionalParameterNames.Length);
                Assert.AreEqual(
                    "a,b",
                    string.Join(",", info.RequiredParameterNames));
                Assert.AreEqual(
                    "String,Int32",
                    string.Join(",", info.RequiredParameterTypes.Select(t => t.Name).ToArray()));
                Assert.AreEqual(
                    "c,d",
                    string.Join(",", info.OptionalParameterNames));
                Assert.AreEqual(
                    "String,Object",
                    string.Join(",", info.OptionalParameterTypes.Select(t => t.Name).ToArray()));
            });
        }

        [TestMethod]
        public void OD_MBO_GetInfo_AllowedArrays()
        {
            ODataTest(() =>
            {
                var method = new TestMethodInfo("fv1",
                    "Content content, object[] a, string[] b, int[] c, long[] d, bool[] e, float[] f, double[] g, decimal[] h",
                    null);

                // ACTION
                var info = AddMethod(method);

                // ASSERT
                Assert.AreEqual(8, info.RequiredParameterNames.Length);
                Assert.AreEqual(8, info.RequiredParameterTypes.Length);
                Assert.AreEqual(0, info.OptionalParameterNames.Length);
                Assert.AreEqual(0, info.OptionalParameterTypes.Length);
                Assert.AreEqual(typeof(object[]), info.RequiredParameterTypes[0]);
                Assert.AreEqual(typeof(string[]), info.RequiredParameterTypes[1]);
                Assert.AreEqual(typeof(int[]), info.RequiredParameterTypes[2]);
                Assert.AreEqual(typeof(long[]), info.RequiredParameterTypes[3]);
                Assert.AreEqual(typeof(bool[]), info.RequiredParameterTypes[4]);
                Assert.AreEqual(typeof(float[]), info.RequiredParameterTypes[5]);
                Assert.AreEqual(typeof(double[]), info.RequiredParameterTypes[6]);
                Assert.AreEqual(typeof(decimal[]), info.RequiredParameterTypes[7]);
            });
        }

        [TestMethod]
        public void OD_MBO_GetInfo_DisallowedArrays()
        {
            ODataTest(() =>
            {
                Assert.IsNull(AddMethod(new TestMethodInfo(
                    "fv1", "Content content, DateTime[] a", null)));
                Assert.IsNull(AddMethod(new TestMethodInfo(
                    "fv1", "Content content, Elephant[] a", null)));
                // etc.
            });
        }

        /* ====================================================================== SEARCH METHOD TESTS */

        [TestMethod]
        public void OD_MBO_Discover()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    OperationCenter.Discover();

                    var discovered = OperationCenter.Operations;

                    //UNDONE: Not finished test.
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Strict_1()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", "int x"));
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "int x"));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "string x"));
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", "int x"));

                    // ACTION
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1",
                            @"{""a"":""asdf"",""b"":""qwer"",""y"":12,""x"":42}");

                    // ASSERT
                    Assert.AreEqual(m1, context.Operation);
                    Assert.AreEqual(2, context.Parameters.Count);
                    Assert.AreEqual("asdf", context.Parameters["a"]);
                    Assert.AreEqual(42, context.Parameters["x"]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Strict_2()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", "int x"));
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "int x"));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "string x"));
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", "int x"));

                    // ACTION
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1",
                            @"{""a"":""asdf"",""b"":""qwer"",""y"":12,""x"":""42"" }");

                    // ASSERT
                    Assert.AreEqual(m2, context.Operation);
                    Assert.AreEqual(2, context.Parameters.Count);
                    Assert.AreEqual("asdf", context.Parameters["a"]);
                    Assert.AreEqual("42", context.Parameters["x"]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Bool()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m = AddMethod(new TestMethodInfo("fv1", "Content content, bool a", null));

                    // ACTION-1 strict
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":true}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(true, context.Parameters["a"]);

                    // ACTION not strict
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""true""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(true, context.Parameters["a"]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Decimal()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m = AddMethod(new TestMethodInfo("fv1", "Content content, decimal a", null));

                    // ACTION-1 strict
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":0.123456789}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789m, context.Parameters["a"]);

                    // ACTION not strict, localized
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("hu-hu");
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""0,123456789""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789m, context.Parameters["a"]);

                    // ACTION not strict, globalized
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""0.123456789""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789m, context.Parameters["a"]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Float()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m = AddMethod(new TestMethodInfo("fv1", "Content content, float a", null));

                    // ACTION-1 strict
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":0.123456789}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789f, context.Parameters["a"]);

                    // ACTION not strict, localized
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("hu-hu");
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""0,123456789""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789f, context.Parameters["a"]);

                    // ACTION not strict, globalized
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""0.123456789""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789f, context.Parameters["a"]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Double()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m = AddMethod(new TestMethodInfo("fv1", "Content content, double a", null));

                    // ACTION-1 strict
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":0.123456789}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789d, context.Parameters["a"]);

                    // ACTION not strict, localized
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("hu-hu");
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""0,123456789""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789d, context.Parameters["a"]);

                    // ACTION not strict, globalized
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""0.123456789""}");
                    // ASSERT
                    Assert.AreEqual(m, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(0.123456789d, context.Parameters["a"]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Spaceship()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, Spaceship a", null));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, Elephant a", null));

                    // ACTION-1
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1",
                            @"{""a"":{""Name"":""Space Bender 8"", ""Class"":""Big F Vehicle"", ""Length"":444}}");

                    // ASSERT
                    Assert.AreEqual(m1, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(typeof(Spaceship), context.Parameters["a"].GetType());
                    var spaceship = (Spaceship)context.Parameters["a"];
                    Assert.AreEqual("Space Bender 8", spaceship.Name);
                    Assert.AreEqual("Big F Vehicle", spaceship.Class);
                    Assert.AreEqual(444, spaceship.Length);
                }

            });
        }

        [TestMethod]
        public void OD_MBO_GetMethodByRequest_Elephant()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, Spaceship a", null));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, Elephant a", null));

                    // ACTION-1
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1",
                            @"{""a"":{""Snout"":42, ""Height"":44}}");

                    // ASSERT
                    Assert.AreEqual(m2, context.Operation);
                    Assert.AreEqual(1, context.Parameters.Count);
                    Assert.AreEqual(typeof(Elephant), context.Parameters["a"].GetType());
                    var elephant = (Elephant)context.Parameters["a"];
                    Assert.AreEqual(42, elephant.Snout);
                    Assert.AreEqual(44, elephant.Height);
                }
            });
        }


        [TestMethod]
        public void OD_MBO_GetMethodByRequest_NotStrict_1()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", "int x"));
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "int x"));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "bool x"));
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", "int x"));

                    // ACTION-1
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1",
                            @"{""a"":""asdf"",""b"":""qwer"",""y"":12,""x"":""42""}");

                    // ASSERT-1
                    Assert.AreEqual(m1, context.Operation);
                    Assert.AreEqual(2, context.Parameters.Count);
                    Assert.AreEqual("asdf", context.Parameters["a"]);
                    Assert.AreEqual(42, context.Parameters["x"]);

                    // ACTION-2
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1",
                            @"{ ""a"":""asdf"",""b"":""qwer"",""y"":12,""x"":""true""}");

                    // ASSERT-2
                    Assert.AreEqual(m2, context.Operation);
                    Assert.AreEqual(2, context.Parameters.Count);
                    Assert.AreEqual("asdf", context.Parameters["a"]);
                    Assert.AreEqual(true, context.Parameters["x"]);
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(OperationNotFoundException))]
        public void OD_MBO_GetMethodByRequest_NotFound_ByName()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(new TestMethodInfo("fv0", "Content content, string a", "int x"));

                    // ACTION
                    var context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""asdf""}");
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(OperationNotFoundException))]
        public void OD_MBO_GetMethodByRequest_NotFound_ByRequiredParamName()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(new TestMethodInfo("fv0", "Content content, string a, string b", null));

                    // ACTION
                    var context = OperationCenter.GetMethodByRequest(GetContent(), "fv0", @"{""a"":""asdf""}");
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(OperationNotFoundException))]
        public void OD_MBO_GetMethodByRequest_NotFound_ByRequiredParamType()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(new TestMethodInfo("fv0", "Content content, string a, string b", null));

                    // ACTION
                    var context = OperationCenter.GetMethodByRequest(GetContent(), "fv0", @"{""a"":""asdf"",""b"":42}");
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(AmbiguousMatchException))]
        public void OD_MBO_GetMethodByRequest_AmbiguousMatch()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", "int x"));
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "int x"));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "string x"));
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", "int x"));

                    // ACTION
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{""a"":""asdf""}");
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(OperationNotFoundException))]
        public void OD_MBO_GetMethodByRequest_UnmatchedOptional()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", "int x"));
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "int x"));
                    var m2 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", "bool x"));
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", "int x"));

                    // ACTION
                    var context =
                        OperationCenter.GetMethodByRequest(GetContent(), "fv1", @"{ ""a"":""asdf"",""x"":""asdf""}");
                }
            });
        }

        /* ====================================================================== REQUEST PARSING TESTS */

        [TestMethod]
        [ExpectedException(typeof(JsonReaderException))]
        public void OD_MBO_Request_InvalidJson()
        {
            var request = ODataMiddleware.Read("asdf");
        }
        [TestMethod]
        public void OD_MBO_Request_Invalid()
        {
            var request = ODataMiddleware.Read("");
            Assert.IsNull(request);

            request = ODataMiddleware.Read("['asdf']");
            Assert.IsNull(request);
        }
        [TestMethod]
        public void OD_MBO_Request_Object()
        {
            var request = ODataMiddleware.Read("{'a':'asdf'}");
            Assert.AreEqual(JTokenType.String, request["a"].Type);
            Assert.AreEqual("{\"a\":\"asdf\"}", request.ToString()
                .Replace("\r", "").Replace("\n", "").Replace(" ", ""));
        }
        [TestMethod]
        public void OD_MBO_Request_Models()
        {
            var request = ODataMiddleware.Read("models=[{'a':'asdf'}]");
            Assert.AreEqual(JTokenType.String, request["a"].Type);
            Assert.AreEqual("{\"a\":\"asdf\"}", request.ToString()
                .Replace("\r", "").Replace("\n", "").Replace(" ", ""));
        }
        [TestMethod]
        public void OD_MBO_Request_Properties()
        {
            var request = ODataMiddleware.Read("models=[{'a':42, 'b':false, 'c':'asdf', 'd':[], 'e':{a:12}}]");
            Assert.AreEqual(JTokenType.Integer, request["a"].Type);
            Assert.AreEqual(JTokenType.Boolean, request["b"].Type);
            Assert.AreEqual(JTokenType.String, request["c"].Type);
            Assert.AreEqual(JTokenType.Array, request["d"].Type);
            Assert.AreEqual(JTokenType.Object, request["e"].Type);
        }
        [TestMethod]
        public void OD_MBO_Request_Float()
        {
            var request = ODataMiddleware.Read("models=[{'a':4.2}]");
            Assert.AreEqual(JTokenType.Float, request["a"].Type);
            Assert.AreEqual(4.2f, request["a"].Value<float>());
            Assert.AreEqual(4.2d, request["a"].Value<double>());
            Assert.AreEqual(4.2m, request["a"].Value<decimal>());
        }
        //[TestMethod]
        //public void OD_MBO_Request_StringArray()
        //{
        //    var request = ODataMiddleware.Read("models=[{'a':['xxx','yyy','zzz']}]");

        //    var array = (JArray) request["a"];
        //    Assert.AreEqual(JTokenType.Array, array.Type);
        //    var stringArray = array.Select(x => x.Value<string>()).ToArray();
        //    var actual = string.Join(",", stringArray);
        //    Assert.AreEqual("xxx,yyy,zzz", actual);
        //}
        [TestMethod]
        public void OD_MBO_Request_ObjectArray()
        {
            var request = ODataMiddleware.Read("models=[{'a':[1,'xxx',false,42]}]");

            var array = (JArray)request["a"];
            Assert.AreEqual(JTokenType.Array, array.Type);
            var objectArray = array.Cast<object>().ToArray();
            var actual = string.Join(",", objectArray.Select(x=>x.ToString()));
            Assert.AreEqual("1,xxx,False,42", actual);
        }

        /* ====================================================================== CALLING TESTS */

        [TestMethod]
        public void OD_MBO_Call_RequiredPrimitives()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op1"));
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op1",
                            @"{""a"":""asdf"", ""b"":42, ""c"":true, ""d"":0.12, ""e"":0.13, ""f"":0.14}");

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var objects = (object[]) result;
                    Assert.AreEqual("asdf", objects[0]);
                    Assert.AreEqual(42, objects[1]);
                    Assert.AreEqual(true, objects[2]);
                    Assert.AreEqual(0.12f, objects[3]);
                    Assert.AreEqual(0.13m, objects[4]);
                    Assert.AreEqual(0.14d, objects[5]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Call_OptionalPrimitives()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op2"));

                    // ACTION
                    OperationCallingContext context;
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""dummy"":0}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    var objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""a"":""testvalue""}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    objects = (object[])result;
                    Assert.AreEqual("testvalue", objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""b"":42}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(42, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""c"":true}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(true, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""d"":12.345}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(12.345f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""e"":12.345}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(12.345m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""f"":12.345}");
                        result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(12.345d, objects[5]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Call_MinimalParameters()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op3"));

                    // ACTION
                    using (new OperationInspectorSwindler(new AllowEverything()))
                    {
                        var context = OperationCenter.GetMethodByRequest(GetContent(), "Op3", @"{""dummy"":1}");
                        var result = OperationCenter.Invoke(context);
                        // ASSERT
                        Assert.AreEqual("Called", result);
                    }
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Call_NullAndDefault()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op1"));
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op1",
                            @"{""a"":null, ""b"":null, ""c"":null, ""d"":null, ""e"":null, ""f"":null}");

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var objects = (object[])result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Call_UndefinedAndDefault()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op1"));
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op1",
                            @"{""a"":undefined, ""b"":undefined, ""c"":undefined, ""d"":undefined, ""e"":undefined, ""f"":undefined}");

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var objects = (object[]) result;
                    Assert.AreEqual(null, objects[0]);
                    Assert.AreEqual(0, objects[1]);
                    Assert.AreEqual(false, objects[2]);
                    Assert.AreEqual(0.0f, objects[3]);
                    Assert.AreEqual(0.0m, objects[4]);
                    Assert.AreEqual(0.0d, objects[5]);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Call_Inspection()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op1"));

                    var inspector = new AllowEverything();
                    var content = GetContent(null, "User");

                    // ACTION
                    using (new OperationInspectorSwindler(inspector))
                    {
                        var context = OperationCenter.GetMethodByRequest(content, "Op1",
                            @"{""a"":""asdf"", ""b"":42, ""c"":true, ""d"":0.12, ""e"":0.13, ""f"":0.14}");
                        var result = OperationCenter.Invoke(context);
                    }

                    // ASSERT
                    var lines = inspector.Log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    Assert.AreEqual(3, lines.Length);
                    Assert.AreEqual("CheckByRoles: 1, Administrators,Editors", lines[0]);
                    Assert.AreEqual("CheckByPermissions: 0, 1, See,Run", lines[1]);
                    //Assert.AreEqual("CheckBeforeInvoke: 1, Op1", lines[2]);
                }
            });
        }

        //[TestMethod]
        //public void OD_MBO_Call_Enumerables()
        //{
        //    ODataTest(() =>
        //    {
        //        using (new CleanOperationCenterBlock())
        //        {
        //            AddMethod(typeof(TestOperations).GetMethod("Op4"));
        //            OperationCallingContext context;
        //            using (new OperationInspectorSwindler(new AllowEverything()))
        //                context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op1",
        //                    @"{""a"":[""xxx"",""yyy"",""zzz""],
        //                                 ""b"":[1,2,3,42],
        //                                 ""c"":[true,false,true],
        //                                 ""d"":[0.12, 0.13, 0,14],
        //                                 ""e"":[0.22, 0.23, 0,24],
        //                                 ""f"":[0.32, 0.33, 0,34],
        //                                 ""g"":[{""Snout"": 345, ""Height"": 543},{""Snout"": 456, ""Height"": 654}]}");

        //            // ACTION
        //            object result;
        //            using (new OperationInspectorSwindler(new AllowEverything()))
        //                result = OperationCenter.Invoke(context);

        //            // ASSERT
        //            var objects = (object[])result;
        //            Assert.AreEqual("asdf", objects[0]);
        //            Assert.AreEqual(42, objects[1]);
        //            Assert.AreEqual(true, objects[2]);
        //            Assert.AreEqual(0.12f, objects[3]);
        //            Assert.AreEqual(0.13m, objects[4]);
        //            Assert.AreEqual(0.14d, objects[5]);
        //        }
        //    });
        //}
        [TestMethod]
        public void OD_MBO_Call_ObjectArray()
        {

            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(typeof(TestOperations).GetMethod("Op4"));
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op4",
                            @"{'a':[42, 'xxx', true, 4.25, [1,2,3], {'Snout': 456, 'Height': 654}]}");

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var objects = (object[])result;
                    Assert.AreEqual(42L, objects[0]);
                    Assert.AreEqual("xxx", objects[1]);
                    Assert.AreEqual(true, objects[2]);
                    Assert.AreEqual(4.25d, objects[3]);
                    Assert.AreEqual("[1,2,3]", objects[4]);
                    Assert.AreEqual("{'Snout': 456, 'Height': 654}", objects[5]);
                }
            });
        }

        /* ====================================================================== TOOLS */

        private readonly Attribute[] _defaultAttributes = new Attribute[] { new ODataFunction() };

        private class CleanOperationCenterBlock : IDisposable
        {
            public CleanOperationCenterBlock()
            {
                var _ = new ODataMiddleware(null); // Ensure running the first-touch discover
                OperationCenter.Operations.Clear();
            }
            public void Dispose()
            {
                OperationCenter.Operations.Clear();
                OperationCenter.Discover();
            }
        }

        private OperationInfo AddMethod(MethodInfo method)
        {
            return OperationCenter.AddMethod(method);
        }
        private OperationInfo AddMethod(TestMethodInfo method)
        {
            return OperationCenter.AddMethod(method, _defaultAttributes);
        }


        private class TestContentHandler
        {
            public static readonly string CTD = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{0}' handler='{1}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <Fields>
    <Field name='Value' type='ShortText'/>
  </Fields>
</ContentType>
";
            public string Value { get; set; }
        }
        private Content GetContent(string value = null, string contentTypeName = null)
        {
            return Content.Create(
                new TestContentHandler {Value = value},
                string.Format(TestContentHandler.CTD, contentTypeName ?? "TestContent", typeof(TestContentHandler).FullName));
        }

        internal class OperationInspectorSwindler : IDisposable
        {
            private readonly OperationInspector _original;
            public OperationInspectorSwindler(OperationInspector instance)
            {
                _original = OperationInspector.Instance;
                OperationInspector.Instance = instance;
            }

            public void Dispose()
            {
                OperationInspector.Instance = _original;
            }
        }
        internal class AllowEverything : OperationInspector
        {
            private StringBuilder _sb = new StringBuilder();
            public string Log { get { return _sb.ToString(); } }

            public override bool CheckPolicies(IUser user, string[] policies, OperationCallingContext context)
            {
                _sb.AppendLine($"CheckPolicies: {GetRealUserId(user)}, {string.Join(",", policies)}");
                return true;
            }
            public override bool CheckByPermissions(Content content, IUser user, string[] permissions)
            {
                _sb.AppendLine($"CheckByPermissions: {content.Id}, {GetRealUserId(user)}, {string.Join(",", permissions)}");
                return true;
            }
            public override bool CheckByRoles(IUser user, string[] roles)
            {
                _sb.AppendLine($"CheckByRoles: {GetRealUserId(user)}, {string.Join(",", roles)}");
                return true;
            }

            private int GetRealUserId(IUser user)
            {
                if (user is SystemUser sysUser)
                    return sysUser.OriginalUser.Id;
                return user.Id;
            }
        }

        internal class TestParameterInfo : ParameterInfo
        {
            public TestParameterInfo(int position, Type parameterType, string name, bool isOptional)
            {
                PositionImpl = position;
                NameImpl = name;
                ClassImpl = parameterType;
                if (isOptional)
                    AttrsImpl |= ParameterAttributes.Optional;
            }
        }

        public class TestMethodInfo : MethodBase
        {
            private ParameterInfo[] _parameters;
            public TestMethodInfo(string name, string requiredParameters, string optionalParameters)
            {
                Name = name;
                _parameters = ParseParameters(requiredParameters, optionalParameters);
            }
            private ParameterInfo[] ParseParameters(string requiredParameters, string optionalParameters)
            {
                var p = 0;
                var parameters =
                    requiredParameters?.Split(',').Select(x => ParseParameter(p++, x, true)).ToArray() ?? new ParameterInfo[0];
                if (optionalParameters != null)
                {
                    var optionals = optionalParameters.Split(',').Select(x => ParseParameter(p++, x, false)).ToArray();
                    parameters = parameters.Union(optionals).ToArray();
                }
                return parameters;
            }
            private ParameterInfo ParseParameter(int position, string src, bool required)
            {
                var terms = src.Trim().Split(' ');
                var type = ParseType(terms[0].Trim());
                return new TestParameterInfo(position, type, terms[1].Trim(), !required);
            }
            private Type ParseType(string src)
            {
                switch (src)
                {
                    case "Content": return typeof(Content);
                    case "string": return typeof(string);
                    case "int": return typeof(int);
                    case "long": return typeof(long);
                    case "double": return typeof(double);
                    case "decimal": return typeof(decimal);
                    case "float": return typeof(float);
                    case "bool": return typeof(bool);
                    case "object": return typeof(object);

                    case "string[]": return typeof(string[]);
                    case "int[]": return typeof(int[]);
                    case "long[]": return typeof(long[]);
                    case "double[]": return typeof(double[]);
                    case "decimal[]": return typeof(decimal[]);
                    case "float[]": return typeof(float[]);
                    case "bool[]": return typeof(bool[]);
                    case "object[]": return typeof(object[]);

                    // disallowed types
                    case "DateTime": return typeof(DateTime);
                    case "DateTime[]": return typeof(DateTime[]);
                    case "Elephant": return typeof(Elephant);
                    case "Elephant[]": return typeof(Elephant[]);
                    case "Spaceship": return typeof(Spaceship);
                    case "Spaceship[]": return typeof(Spaceship[]);

                    case "IEnumerable<int>": return typeof(IEnumerable<int>);
                    case "List<int>": return typeof(List<int>);
                    case "Stack<int>": return typeof(Stack<int>);
                    case "Dictionary<int-int>": return typeof(Dictionary<int, int>);

                    default:
                        throw new ApplicationException("Unknown type: " + src);
                }
            }

            /* ======================================================================================= */

            public override ParameterInfo[] GetParameters() => _parameters;


            public override MethodAttributes Attributes => throw new NotImplementedException();

            public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override MemberTypes MemberType => throw new NotImplementedException();

            public override string Name { get; }

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override MethodImplAttributes GetMethodImplementationFlags()
            {
                throw new NotImplementedException();
            }

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }
        }

    }
}
