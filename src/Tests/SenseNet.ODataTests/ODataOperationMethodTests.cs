using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
                    var countBefore = OperationCenter.Operations.Count;
                    Assert.AreEqual(0, countBefore);

                    // ACTION
                    OperationCenter.Discover();

                    // ASSERT
                    var countAfter = OperationCenter.Operations.Count;
                    Assert.IsTrue(countAfter > countBefore);
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
            });
        }

        [TestMethod]
        public void OD_MBO_Call_OptionalPrimitives()
        {
            ODataTest(() =>
            {
                // ACTION
                OperationCallingContext context;
                object result;
                using (new OperationInspectorSwindler(new AllowEverything()))
                {
                    context = OperationCenter.GetMethodByRequest(GetContent(), "Op2", @"{""dummy"":0}");
                    result = OperationCenter.Invoke(context);
                }

                // ASSERT
                var objects = (object[]) result;
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
                objects = (object[]) result;
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
                objects = (object[]) result;
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
                objects = (object[]) result;
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
                objects = (object[]) result;
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
                objects = (object[]) result;
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
                objects = (object[]) result;
                Assert.AreEqual(null, objects[0]);
                Assert.AreEqual(0, objects[1]);
                Assert.AreEqual(false, objects[2]);
                Assert.AreEqual(0.0f, objects[3]);
                Assert.AreEqual(0.0m, objects[4]);
                Assert.AreEqual(12.345d, objects[5]);
            });
        }

        [TestMethod]
        public void OD_MBO_Call_MinimalParameters()
        {
            ODataTest(() =>
            {
                // ACTION
                using (new OperationInspectorSwindler(new AllowEverything()))
                {
                    var context = OperationCenter.GetMethodByRequest(GetContent(), "Op3", @"{""dummy"":1}");
                    var result = OperationCenter.Invoke(context);
                    // ASSERT
                    Assert.AreEqual("Called", result);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Call_NullAndDefault()
        {
            ODataTest(() =>
            {
                OperationCallingContext context;
                using (new OperationInspectorSwindler(new AllowEverything()))
                    context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op1",
                        @"{""a"":null, ""b"":null, ""c"":null, ""d"":null, ""e"":null, ""f"":null}");

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
            });
        }

        [TestMethod]
        public void OD_MBO_Call_UndefinedAndDefault()
        {
            ODataTest(() =>
            {
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
            });
        }

        [TestMethod]
        public void OD_MBO_Call_Inspection()
        {
            ODataTest(() =>
            {
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
                var lines = inspector.Log.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                Assert.AreEqual(3, lines.Length);
                Assert.AreEqual("CheckByRoles: 1, Administrators,Editors", lines[0]);
                Assert.AreEqual("CheckByPermissions: 0, 1, See,Run", lines[1]);
                Assert.AreEqual("CheckPolicies: 1, Policy1", lines[2]);
            });
        }

        /* ================================================================ REAL INSPECTION */

        [TestMethod]
        public void OD_MBO_Call_RealInspection_WithoutAuthorization()
        {
            ODataTest(() =>
            {
                RealInspectionTest(nameof(TestOperations.Authorization_None), null, 200);
                RealInspectionTest(nameof(TestOperations.Authorization_None), User.Administrator, 401, ODataExceptionCode.Unauthorized);
                RealInspectionTest(nameof(TestOperations.Authorization_None), User.Visitor, 404);
            });
        }
        [TestMethod]
        public void OD_MBO_Call_RealInspection_AuthorizationByRole()
        {
            ODataTest(() =>
            {
                RealInspectionTest(nameof(TestOperations.AuthorizedByRole_Administrators), null, 200);
                RealInspectionTest(nameof(TestOperations.AuthorizedByRole_Administrators), User.Administrator, 200);
                RealInspectionTest(nameof(TestOperations.AuthorizedByRole_Administrators), User.Visitor, 404);
            });
        }
        [TestMethod]
        public void OD_MBO_Call_RealInspection_AuthorizationByRole_Visitor()
        {
            ODataTest(() =>
            {
                using (new AllowPermissionBlock(Identifiers.PortalRootId, Identifiers.VisitorUserId,
                    false, PermissionType.See))
                {
                    RealInspectionTest(nameof(TestOperations.AuthorizedByRole_Visitor), null, 200);
                    RealInspectionTest(nameof(TestOperations.AuthorizedByRole_Visitor), User.Administrator, 403, ODataExceptionCode.Forbidden);
                    RealInspectionTest(nameof(TestOperations.AuthorizedByRole_Visitor), User.Visitor, 200);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_Call_RealInspection_AuthorizationByRole_All()
        {
            ODataTest(() =>
            {
                using (new AllowPermissionBlock(Identifiers.PortalRootId, Identifiers.VisitorUserId,
                    false, PermissionType.See))
                {
                    RealInspectionTest(nameof(TestOperations.AuthorizedByRole_All), null, 200);
                    RealInspectionTest(nameof(TestOperations.AuthorizedByRole_All), User.Administrator, 200);
                    RealInspectionTest(nameof(TestOperations.AuthorizedByRole_All), User.Visitor, 200);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_Call_RealInspection_AuthorizationByPermission()
        {
            ODataTest(() =>
            {
                using (new AllowPermissionBlock(Identifiers.PortalRootId, Identifiers.VisitorUserId,
                    false, PermissionType.See))
                {
                    RealInspectionTest(nameof(TestOperations.AuthorizedByPermission), null, 200);
                    RealInspectionTest(nameof(TestOperations.AuthorizedByPermission), User.Administrator, 200);
                    RealInspectionTest(nameof(TestOperations.AuthorizedByPermission), User.Visitor, 403, ODataExceptionCode.Forbidden);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_Call_RealInspection_AuthorizationByPolicy()
        {
            ODataTest(() =>
            {
                RealInspectionTest(nameof(TestOperations.AuthorizedByPolicy), null, 200);
                RealInspectionTest(nameof(TestOperations.AuthorizedByPolicy), User.Administrator, 403, ODataExceptionCode.Forbidden);
                RealInspectionTest(nameof(TestOperations.AuthorizedByPolicy), User.Visitor, 200);
            });
        }

        private void RealInspectionTest(string methodName, IUser user, int expectedHttpCode,
            ODataExceptionCode? expectedODataExceptionCode = null)
        {
            var magicValue = Guid.NewGuid().ToString();
            ODataResponse response;

            // ACTION
            using (user == null ? (IDisposable)new SystemAccount() : new CurrentUserBlock(user))
                response = ODataPostAsync($"/OData.svc/Root('IMS')/{methodName}", "?param2=value2",
                    $"{{a:\"{magicValue}\"}}").ConfigureAwait(false).GetAwaiter().GetResult();

            // ASSERT
            Assert.AreEqual(expectedHttpCode, response.StatusCode);
            if (expectedHttpCode == 200)
            {
                Assert.AreEqual($"{methodName}-{magicValue}", response.Result);
            }
            else if (expectedHttpCode == 404)
            {
                Assert.AreEqual(0, response.Result.Length);
            }
            else
            {
                var error = GetError(response, false);
                Assert.IsNotNull(error);
                Assert.AreEqual(expectedODataExceptionCode.Value, error.Code);
            }
        }

        /* ================================================================ CALL */

        [TestMethod]
        public void OD_MBO_Call_JObject()
        {
            ODataTest(() =>
            {
                OperationCallingContext context;
                using (new OperationInspectorSwindler(new AllowEverything()))
                    context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op4",
                        @"{'a':{'Snout': 456, 'Height': 654}}");

                // ACTION
                object result;
                using (new OperationInspectorSwindler(new AllowEverything()))
                    result = OperationCenter.Invoke(context);

                // ASSERT
                Assert.AreEqual(typeof(JObject), result.GetType());
                Assert.AreEqual("{\"Snout\":456,\"Height\":654}", RemoveWhitespaces(result.ToString()));
            });
        }

        [TestMethod]
        public void OD_MBO_Call_ObjectArray()
        {
            ODataTest(() =>
            {
                OperationCallingContext context;
                using (new OperationInspectorSwindler(new AllowEverything()))
                    context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), "Op5",
                        @"{'a':[42, 'xxx', true, 4.25, [1,2,3], {'Snout': 456, 'Height': 654}]}");

                // ACTION
                object result;
                using (new OperationInspectorSwindler(new AllowEverything()))
                    result = OperationCenter.Invoke(context);

                // ASSERT
                var objects = (object[]) result;
                Assert.AreEqual(42L, objects[0]);
                Assert.AreEqual("xxx", objects[1]);
                Assert.AreEqual(true, objects[2]);
                Assert.AreEqual(4.25d, objects[3]);
                Assert.AreEqual(typeof(JArray), objects[4].GetType());
                Assert.AreEqual("[1,2,3]", RemoveWhitespaces(objects[4].ToString()));
                Assert.AreEqual(typeof(JObject), objects[5].GetType());
                Assert.AreEqual("{\"Snout\":456,\"Height\":654}", RemoveWhitespaces(objects[5].ToString()));
            });
        }

        [TestMethod]
        public void OD_MBO_Call_Array()
        {
            #region void Test<T>(string methodName, string request, T[] expectedResult)
            void Test<T>(string methodName, string request, T[] expectedResult)
            {
                ODataTest(() =>
                {
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), methodName, request);

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var values = (T[]) result;
                    Assert.AreEqual(expectedResult.Length, values.Length);
                    for (var i = 0; i < expectedResult.Length; i++)
                        Assert.AreEqual(expectedResult[i], values[i],
                            $"Assert.AreEqual failed. Expected: {expectedResult[i]}. Actual: {values[i]}");
                });
            }
            #endregion

            Test<string>(nameof(TestOperations.Array_String), @"{'a':['xxx', 'yyy', 'zzz']}", new[] { "xxx", "yyy", "zzz" });
            Test<int>(nameof(TestOperations.Array_Int), @"{'a':[1,2,42]}", new[] { 1, 2, 42 });
            Test<long>(nameof(TestOperations.Array_Long), @"{'a':[1,2,42]}", new[] { 1L, 2L, 42L });
            Test<bool>(nameof(TestOperations.Array_Bool), @"{'a':[true,false,true]}", new[] { true, false, true });
            Test<float>(nameof(TestOperations.Array_Float), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1f, 2.2f, 42.42f });
            Test<double>(nameof(TestOperations.Array_Double), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1d, 2.2d, 42.42d });
            Test<decimal>(nameof(TestOperations.Array_Decimal), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1m, 2.2m, 42.42m });
        }
        [TestMethod]
        public void OD_MBO_Call_List()
        {
            #region void Test<T>(string methodName, string request, T[] expectedResult)
            void Test<T>(string methodName, string request, T[] expectedResult)
            {
                ODataTest(() =>
                {
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), methodName, request);

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var values = (List<T>)result;
                    Assert.AreEqual(expectedResult.Length, values.Count);
                    for (var i = 0; i < expectedResult.Length; i++)
                        Assert.AreEqual(expectedResult[i], values[i],
                            $"Assert.AreEqual failed. Expected: {expectedResult[i]}. Actual: {values[i]}");
                });
            }
            #endregion

            Test<string>(nameof(TestOperations.List_String), @"{'a':['xxx', 'yyy', 'zzz']}", new[] { "xxx", "yyy", "zzz" });
            Test<int>(nameof(TestOperations.List_Int), @"{'a':[1,2,42]}", new[] { 1, 2, 42 });
            Test<long>(nameof(TestOperations.List_Long), @"{'a':[1,2,42]}", new[] { 1L, 2L, 42L });
            Test<bool>(nameof(TestOperations.List_Bool), @"{'a':[true,false,true]}", new[] { true, false, true });
            Test<float>(nameof(TestOperations.List_Float), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1f, 2.2f, 42.42f });
            Test<double>(nameof(TestOperations.List_Double), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1d, 2.2d, 42.42d });
            Test<decimal>(nameof(TestOperations.List_Decimal), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1m, 2.2m, 42.42m });
        }
        [TestMethod]
        public void OD_MBO_Call_Enumerable()
        {
            #region void Test<T>(string methodName, string request, T[] expectedResult)
            void Test<T>(string methodName, string request, T[] expectedResult)
            {
                ODataTest(() =>
                {
                    OperationCallingContext context;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), methodName, request);

                    // ACTION
                    object result;
                    using (new OperationInspectorSwindler(new AllowEverything()))
                        result = OperationCenter.Invoke(context);

                    // ASSERT
                    var values = ((IEnumerable<T>)result).ToArray();
                    Assert.AreEqual(expectedResult.Length, values.Length);
                    for (var i = 0; i < expectedResult.Length; i++)
                        Assert.AreEqual(expectedResult[i], values[i],
                            $"Assert.AreEqual failed. Expected: {expectedResult[i]}. Actual: {values[i]}");
                });
            }
            #endregion

            Test<string>(nameof(TestOperations.Enumerable_String), @"{'a':['xxx', 'yyy', 'zzz']}", new[] { "xxx", "yyy", "zzz" });
            Test<int>(nameof(TestOperations.Enumerable_Int), @"{'a':[1,2,42]}", new[] { 1, 2, 42 });
            Test<long>(nameof(TestOperations.Enumerable_Long), @"{'a':[1,2,42]}", new[] { 1L, 2L, 42L });
            Test<bool>(nameof(TestOperations.Enumerable_Bool), @"{'a':[true,false,true]}", new[] { true, false, true });
            Test<float>(nameof(TestOperations.Enumerable_Float), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1f, 2.2f, 42.42f });
            Test<double>(nameof(TestOperations.Enumerable_Double), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1d, 2.2d, 42.42d });
            Test<decimal>(nameof(TestOperations.Enumerable_Decimal), @"{'a':[1.1,2.2,42.42]}", new[] { 1.1m, 2.2m, 42.42m });
        }

        [TestMethod]
        public void OD_MBO_Call_EnumerableError()
        {
            #region void Test<T>(string methodName, string request)
            bool Test<T>(string methodName, string request)
            {
                var testResult = false;
                ODataTest(() =>
                {
                    try
                    {
                        OperationCallingContext context;
                        using (new OperationInspectorSwindler(new AllowEverything()))
                            context = OperationCenter.GetMethodByRequest(GetContent(null, "User"), methodName, request);

                        object result;
                        using (new OperationInspectorSwindler(new AllowEverything()))
                            result = OperationCenter.Invoke(context);

                        testResult = true;
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                });
                return testResult;
            }

            #endregion

            Assert.IsTrue(Test<string>(nameof(TestOperations.Enumerable_String), @"{'a':['xxx', true, 'zzz']}"));
            Assert.IsTrue(Test<int>(nameof(TestOperations.Enumerable_Int), @"{'a':[1,2,'42']}"));
            Assert.IsFalse(Test<int>(nameof(TestOperations.Enumerable_Int), @"{'a':[1,2,'xxx']}"));
        }

        /* ====================================================================== ACTION QUERY TESTS */

        [TestMethod]
        public void OD_MBO_Actions_Uri_Root()
        {
            ODataTest(() =>
            {
                var content = Content.Create(Repository.Root);
                var expected = "/odata.svc/('Root')/Operation1";

                var actual = OperationMethodStorage.GenerateUri(content, "Operation1");

                Assert.AreEqual(expected, actual);
            });
        }
        [TestMethod]
        public void OD_MBO_Actions_Uri()
        {
            ODataTest(() =>
            {
                var content = Content.Create(Repository.ImsFolder);
                var expected = "/odata.svc/Root('IMS')/Operation1";

                var actual = OperationMethodStorage.GenerateUri(content, "Operation1");

                Assert.AreEqual(expected, actual);
            });
        }

        [TestMethod]
        public void OD_MBO_Actions_Lists()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("op", "Content content, string a", null),
                        new Attribute[] { new ODataAction() });

                    // ACTION-1: metadata
                    var response = ODataGetAsync("/OData.svc/Root('IMS')", "?$select=Id")
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    // ASSERT-1
                    var entity = GetEntity(response);
                    var operations1 = entity.MetadataActions.Union(entity.MetadataFunctions).Where(x=>x.Name == "op").ToArray();
                    Assert.AreEqual(1, operations1.Length);

                    // ACTION-2: Actions expanded field
                    response = ODataGetAsync("/OData.svc/Root('IMS')", "?metadata=no&$expand=Actions&$select=Id,Actions")
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    entity = GetEntity(response);
                    var operations2 = entity.Actions.Where(x => x.Name == "op").ToArray();
                    Assert.AreEqual(1, operations2.Length);

                    // ACTION-3: Actions field only
                    response = ODataGetAsync("/OData.svc/Root('IMS')/Actions", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    // ASSERT-3
                    entity = GetEntity(response);
                    operations2 = entity.Actions.Where(x => x.Name == "op").ToArray();
                    Assert.AreEqual(1, operations2.Length);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Actions_ActionProperties_ActionField()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(new TestMethodInfo("op0", "Content content", null),
                        new Attribute[] { new ODataAction(), });
                    AddMethod(new TestMethodInfo("op1", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), });
                    AddMethod(new TestMethodInfo("op2", "Content content, string b, int c", null),
                        new Attribute[] { new ODataFunction(), });

                    // ACTION
                    var response = ODataGetAsync("/OData.svc/Root('IMS')/Actions", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    // ASSERT
                    var entity = GetEntity(response);
                    var operations = entity.Actions
                        .Where(x=>x.Name.StartsWith("op"))
                        .OrderBy(x => x.Name)
                        .ToArray();

                    Assert.AreEqual(3, operations.Length);

                    Assert.AreEqual("op0", operations[0].Name);
                    Assert.AreEqual("", string.Join(",", operations[0].ActionParameters));
                    Assert.AreEqual("/odata.svc/Root('IMS')/op0", operations[0].Url);
                    Assert.AreEqual(true, operations[0].IsODataAction);
                    Assert.AreEqual(false, operations[0].Forbidden);

                    Assert.AreEqual("op1", operations[1].Name);
                    Assert.AreEqual("a", string.Join(",", operations[1].ActionParameters));
                    Assert.AreEqual("/odata.svc/Root('IMS')/op1", operations[1].Url);
                    Assert.AreEqual(true, operations[1].IsODataAction);
                    Assert.AreEqual(false, operations[1].Forbidden);

                    Assert.AreEqual("op2", operations[2].Name);
                    Assert.AreEqual("b,c", string.Join(",", operations[2].ActionParameters));
                    Assert.AreEqual("/odata.svc/Root('IMS')/op2", operations[2].Url);
                    Assert.AreEqual(true, operations[2].IsODataAction);
                    Assert.AreEqual(false, operations[2].Forbidden);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_Actions_ActionProperties_Metadata()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    AddMethod(new TestMethodInfo("op0", "Content content", null),
                        new Attribute[] { new ODataAction(), });
                    AddMethod(new TestMethodInfo("op1", "Content content, string a", null),
                        new Attribute[] { new ODataFunction(), });
                    AddMethod(new TestMethodInfo("op2", "Content content, string b, int c", null),
                        new Attribute[] { new ODataFunction(), });

                    // ACTION
                    var response = ODataGetAsync("/OData.svc/Root('IMS')", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    // ASSERT
                    var entity = GetEntity(response);
                    var operations = entity.MetadataActions
                        .Union(entity.MetadataFunctions)
                        .Where(x => x.Name.StartsWith("op"))
                        .OrderBy(x => x.Name)
                        .ToArray();

                    Assert.AreEqual(3, operations.Length);

                    Assert.AreEqual("op0", operations[0].Name);
                    Assert.AreEqual("", string.Join(",", operations[0].Parameters.Select(x => x.Name)));
                    Assert.AreEqual("", string.Join(",", operations[0].Parameters.Select(x => x.Type)));
                    Assert.AreEqual("/odata.svc/Root('IMS')/op0", operations[0].Target);
                    Assert.AreEqual(false, operations[0].Forbidden);

                    Assert.AreEqual("op1", operations[1].Name);
                    Assert.AreEqual("a", string.Join(",", operations[1].Parameters.Select(x => x.Name)));
                    Assert.AreEqual("string", string.Join(",", operations[1].Parameters.Select(x => x.Type)));
                    Assert.AreEqual("/odata.svc/Root('IMS')/op1", operations[1].Target);
                    Assert.AreEqual(false, operations[1].Forbidden);

                    Assert.AreEqual("op2", operations[2].Name);
                    Assert.AreEqual("b,c", string.Join(",", operations[2].Parameters.Select(x => x.Name)));
                    Assert.AreEqual("string,int", string.Join(",", operations[2].Parameters.Select(x => x.Type)));
                    Assert.AreEqual("/odata.svc/Root('IMS')/op2", operations[2].Target);
                    Assert.AreEqual(false, operations[2].Forbidden);
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Actions_FilteredByContentType()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new ContentTypeAttribute("GenericContent"), });
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new ContentTypeAttribute("Folder"), });
                    var m2 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new ContentTypeAttribute("Domains"), });
                    var m3 = AddMethod(new TestMethodInfo("fv3", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new ContentTypeAttribute("File"), });

                    // ACTION
                    var response = ODataGetAsync("/OData.svc/Root('IMS')/Actions", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    // ASSERT
                    var entity = GetEntity(response);
                    var operationNames = entity.Actions
                        .Select(x => x.Name)
                        .OrderBy(x => x)
                        .ToArray();
                    Assert.IsTrue(operationNames.Contains("fv0"));
                    Assert.IsTrue(operationNames.Contains("fv1"));
                    Assert.IsTrue(operationNames.Contains("fv2"));
                    Assert.IsFalse(operationNames.Contains("fv3"));
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Actions_Authorization_Membership()
        {
            ODataTest(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", null),
                        new Attribute[] {new ODataAction(), new SnAuthorizeAttribute {Role = "Administrators"}});
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", null),
                        new Attribute[] {new ODataAction(), new SnAuthorizeAttribute {Role = "Developers"}});
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", null),
                        new Attribute[] {new ODataAction(), new SnAuthorizeAttribute {Role = "Developers,Administrators"}});
                    var m4 = AddMethod(new TestMethodInfo("fv3", "Content content, string a", null),
                        new Attribute[] {new ODataAction(), new SnAuthorizeAttribute {Role = "Developers,UnknownGroup42"}});

                    using (new CurrentUserBlock(User.Administrator))
                    {
                        // ACTION
                        var response = ODataGetAsync("/OData.svc/Root('IMS')/Actions", "")
                            .ConfigureAwait(false).GetAwaiter().GetResult();

                        // ASSERT
                        var entity = GetEntity(response);
                        var operationNames = entity.Actions
                            .Select(x => x.Name)
                            .OrderBy(x => x)
                            .ToArray();
                        Assert.IsTrue(operationNames.Contains("fv0"));
                        Assert.IsFalse(operationNames.Contains("fv1"));
                        Assert.IsTrue(operationNames.Contains("fv2"));
                        Assert.IsFalse(operationNames.Contains("fv3"));
                    }
                }
            });
        }

        [TestMethod]
        public void OD_MBO_Actions_Authorization_Permission()
        {
            string GetAllowedActionNames(string nodeName)
            {
                var response = ODataGetAsync($"/OData.svc/Root('{nodeName}')/Actions", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                if (response.StatusCode == 404)
                    return "";

                var entity = GetEntity(response);
                var operationNames = string.Join(", ", entity.Actions.Select(x => $"{x.Name}:{!x.Forbidden}").ToArray());
                return operationNames;
            }

            IsolatedODataTestAsync(() =>
            {
                using (new CleanOperationCenterBlock())
                {
                    var m0 = AddMethod(new TestMethodInfo("fv0", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new SnAuthorizeAttribute { Permission = "See" } });
                    var m1 = AddMethod(new TestMethodInfo("fv1", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new SnAuthorizeAttribute { Permission = "Open" } });
                    var m3 = AddMethod(new TestMethodInfo("fv2", "Content content, string a", null),
                        new Attribute[] { new ODataAction(), new SnAuthorizeAttribute { Permission = "Save" } });

                    var user = User.Visitor;

                    var nodes = Enumerable.Range(0, 4).Select(x =>
                    {
                        var folder = new Folder(Repository.Root) {Name = $"Folder{x}"};
                        folder.Save();
                        return folder;
                    }).ToArray();

                    SecurityHandler.CreateAclEditor()
                        .Allow(nodes[1].Id, user.Id, false, PermissionType.See)
                        .Allow(nodes[2].Id, user.Id, false, PermissionType.Open)
                        .Allow(nodes[3].Id, user.Id, false, PermissionType.Save)
                        .Apply();

                    using (new CurrentUserBlock(User.Visitor))
                    {
                        Assert.AreEqual("", GetAllowedActionNames(nodes[0].Name));
                        Assert.AreEqual("fv0:True, fv1:False, fv2:False", GetAllowedActionNames(nodes[1].Name));
                        Assert.AreEqual("fv0:True, fv1:True, fv2:False", GetAllowedActionNames(nodes[2].Name));
                        Assert.AreEqual("fv0:True, fv1:True, fv2:True", GetAllowedActionNames(nodes[3].Name));
                    }

                    return System.Threading.Tasks.Task.CompletedTask;
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();
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
        private class CurrentUserBlock : IDisposable
        {
            private readonly IUser _backup;
            public CurrentUserBlock(IUser user)
            {
                _backup = User.Current;
                User.Current = user;
            }
            public void Dispose()
            {
                User.Current = _backup;
            }
        }

        private class AllowPermissionBlock : IDisposable
        {
            private int _entityId;
            private int _identityId;
            private bool _localOnly;
            PermissionType[] _permissions;
            public AllowPermissionBlock(int entityId, int identityId, bool localOnly, params PermissionType[] permissions)
            {
                _entityId = entityId;
                _identityId = identityId;
                _localOnly = localOnly;
                _permissions = permissions;

                SecurityHandler.CreateAclEditor()
                    .Allow(entityId, identityId, localOnly, permissions)
                    .Apply();
            }
            public void Dispose()
            {
                SecurityHandler.CreateAclEditor()
                    .ClearPermission(_entityId, _identityId, _localOnly, _permissions)
                    .Apply();
            }
        }

        private OperationInfo AddMethod(MethodInfo method)
        {
            return OperationCenter.AddMethod(method);
        }
        private OperationInfo AddMethod(TestMethodInfo method, Attribute[] attributes = null)
        {
            return OperationCenter.AddMethod(method, attributes ?? _defaultAttributes);
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

            public override bool CheckPolicies(string[] policies, OperationCallingContext context)
            {
                _sb.AppendLine($"CheckPolicies: {GetRealUserId(User.Current)}, {string.Join(",", policies)}");
                return true;
            }
            public override bool CheckByPermissions(Content content, string[] permissions)
            {
                _sb.AppendLine($"CheckByPermissions: {content.Id}, {GetRealUserId(User.Current)}, {string.Join(",", permissions)}");
                return true;
            }
            public override bool CheckByRoles(string[] expectedRoles, IEnumerable<string> actualRoles = null)
            {
                _sb.AppendLine($"CheckByRoles: {GetRealUserId(User.Current)}, {string.Join(",", expectedRoles)}");
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
