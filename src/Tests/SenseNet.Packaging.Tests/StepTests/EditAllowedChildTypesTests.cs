using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Packaging.Steps;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.Packaging.Tests.StepTests
{
    [TestClass]
    public class EditAllowedChildTypesTests : TestBase
    {
        private static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new TypeAccessor(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);
        }

        [TestMethod]
        public void Step_EditAllowedChildTypes_0Orig_0New_0Old()
        {
            var result = EditAllowedChildTypes.GetEditedList(new string[0], null, null);
            Assert.AreEqual(0, result.Length);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_0Orig_1New_0Old()
        {
            var t1 = "Type1";
            var result = EditAllowedChildTypes.GetEditedList(new string[0], t1, null);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(t1, result[0]);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_0Orig_3New_0Old()
        {
            var t = new [] { "Type1", "Type2", "Type3" };
            var tstring = $"{t[0]},{t[1]},{t[2]}";

            var result = EditAllowedChildTypes.GetEditedList(new string[0], tstring, null);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(t[0], result[0]);
            Assert.AreEqual(t[1], result[1]);
            Assert.AreEqual(t[2], result[2]);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_0Orig_8New_0Old_Distinct()
        {
            var t = new [] { "Type1", "Type2", "Type3" };
            var tstring = $"{t[0]},{t[1]},{t[1]},{t[2]},{t[0]},{t[2]},{t[1]},{t[1]}";

            var result = EditAllowedChildTypes.GetEditedList(new string[0], tstring, null);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(t[0], result[0]);
            Assert.AreEqual(t[1], result[1]);
            Assert.AreEqual(t[2], result[2]);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_0Orig_6New_0Old_EmptyAndTrim()
        {
            var t = new [] { "Type1", "Type2", "Type3", "Type4", "Type5", "Type6" };
            var tstring = $"\r\n \t   {t[0]},  {t[1]}  , {t[2]}, \t{t[3]}\t \r\n,,,{t[4]},{t[5]}   \t";

            var result = EditAllowedChildTypes.GetEditedList(new string[0], tstring, null);

            Assert.AreEqual(6, result.Length);
            Assert.AreEqual(t[0], result[0]);
            Assert.AreEqual(t[1], result[1]);
            Assert.AreEqual(t[2], result[2]);
            Assert.AreEqual(t[3], result[3]);
            Assert.AreEqual(t[4], result[4]);
            Assert.AreEqual(t[5], result[5]);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_5Orig_0New_3Old()
        {
            var t = Enumerable.Range(1, 5).Select(i => $"Type{i}").ToArray();
            var origArray = t.Take(5).ToArray();
            var oldString = $"{t[0]},{t[2]},{t[3]}";

            var result = EditAllowedChildTypes.GetEditedList(origArray, null, oldString);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(t[1], result[0]);
            Assert.AreEqual(t[4], result[1]);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_5Orig_3New_2Old()
        {
            var t = Enumerable.Range(1, 8).Select(i => $"Type{i}").ToArray();
            var origArray = t.Take(5).ToArray();
            var newString = $"{t[5]},{t[6]},{t[7]}";
            var oldString = $"{t[0]},{t[2]}";

            var result = EditAllowedChildTypes.GetEditedList(origArray, newString, oldString);

            Assert.AreEqual(6, result.Length);
            Assert.AreEqual(t[1], result[0]);
            Assert.AreEqual(t[3], result[1]);
            Assert.AreEqual(t[4], result[2]);
            Assert.AreEqual(t[5], result[3]);
            Assert.AreEqual(t[6], result[4]);
            Assert.AreEqual(t[7], result[5]);
        }
        [TestMethod]
        public void Step_EditAllowedChildTypes_Empty_3New_2Old_NewOldCollision()
        {
            var t = Enumerable.Range(1, 5).Select(i => $"Type{i}").ToArray();
            var newString = $"{t[0]},{t[1]},{t[2]},{t[3]}";
            var oldString = $"{t[2]},{t[3]},{t[4]}";

            var result = EditAllowedChildTypes.GetEditedList(new string[0], newString, oldString);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(t[0], result[0]);
            Assert.AreEqual(t[1], result[1]);
        }
    }
}
