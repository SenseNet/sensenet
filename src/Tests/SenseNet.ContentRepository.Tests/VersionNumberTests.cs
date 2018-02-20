using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    /// <summary>
    ///This is a test class for SenseNet.ContentRepository.Storage.VersionNumber and is intended
    ///to contain all SenseNet.ContentRepository.Storage.VersionNumber Unit Tests
    ///</summary>
    [TestClass()]
    public class VersionNumberTest : TestBase
    {
        [TestMethod]
        public void VersionNumber_Constructor()
        {
            //-- teljes értelmezési tartomány

            VersionNumber target = new VersionNumber(123, 456);
            Assert.IsTrue(target.Major == 123 && target.Minor == 456);
        }
        [TestMethod]
        public void VersionNumber_Clone()
        {
            int major = 1;
            int minor = 2;

            VersionNumber source = new VersionNumber(major, minor);
            VersionNumber target = source.Clone();

            Assert.IsFalse(Object.ReferenceEquals(source, target), "VersionNumber.Clone #1");
            Assert.IsTrue(source == target, "VersionNumber.Clone #2");
        }
        [TestMethod]
        public void VersionNumber_CompareTo()
        {
            //-- invalid objektumra nincs teszt

            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);

            CompareToTestAssert(v1, v2, -1);
            CompareToTestAssert(v2, v3, 0);
            CompareToTestAssert(v3, v1, 1);
            CompareToTestAssert(v1, v4, -1);
            CompareToTestAssert(v3, v4, -1);
            CompareToTestAssert(v4, v1, 1);
            CompareToTestAssert(v4, v1, 1);

        }
        private void CompareToTestAssert(VersionNumber v1, VersionNumber v2, int expectedValue)
        {
            Assert.AreEqual(v1.CompareTo(v2), expectedValue,
                String.Format("VersionNumber.CompareTo: [{0}].CompareTo([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1.CompareTo(v2), expectedValue));
        }
        [TestMethod]
        public void VersionNumber_Equals()
        {
            //-- invalid objektumra nincs teszt

            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            EqualsTestAssert(v1, v2, false);
            EqualsTestAssert(v2, v1, false);
            EqualsTestAssert(v2, v3, true);
            EqualsTestAssert(v3, v2, true);

            EqualsTestAssert(v4, v1, false);
            EqualsTestAssert(v4, v2, false);
            EqualsTestAssert(v4, v3, false);
            EqualsTestAssert(v4, v1, false);
            EqualsTestAssert(v4, v2, false);
            EqualsTestAssert(v4, v3, false);

            EqualsTestAssert(v4, v5, true);
            EqualsTestAssert(v5, v4, true);
        }
        private void EqualsTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1.Equals(v2), expectedValue,
                String.Format("VersionNumber.Equals: [{0}].Equals([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1.Equals(v2), expectedValue));
        }
        [TestMethod]
        public void VersionNumber_Inequality()
        {
            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            InequalityTestAssert(v1, v2, true);
            InequalityTestAssert(v2, v1, true);

            InequalityTestAssert(v1, v4, true);
            InequalityTestAssert(v2, v4, true);
            InequalityTestAssert(v3, v4, true);
            InequalityTestAssert(v4, v1, true);
            InequalityTestAssert(v4, v2, true);
            InequalityTestAssert(v4, v3, true);

            InequalityTestAssert(v2, v3, false);
            InequalityTestAssert(v3, v2, false);
            InequalityTestAssert(v4, v5, false);
            InequalityTestAssert(v5, v4, false);
        }
        private void InequalityTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1 != v2, expectedValue,
                String.Format("VersionNumber.InequalityTest: [{0}] != ([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1 != v2, expectedValue));
        }
        [TestMethod]
        public void VersionNumber_Equality()
        {
            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            EqualityTestAssert(v1, v2, false);
            EqualityTestAssert(v2, v1, false);
            EqualityTestAssert(v1, v4, false);
            EqualityTestAssert(v2, v4, false);
            EqualityTestAssert(v3, v4, false);
            EqualityTestAssert(v4, v1, false);
            EqualityTestAssert(v4, v2, false);
            EqualityTestAssert(v4, v3, false);

            EqualityTestAssert(v2, v3, true);
            EqualityTestAssert(v3, v2, true);
            EqualityTestAssert(v4, v5, true);
            EqualityTestAssert(v5, v4, true);
        }
        private void EqualityTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1 == v2, expectedValue,
                String.Format("VersionNumber.EqualityTest: [{0}] == ([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1 == v2, expectedValue));
        }
        [TestMethod]
        public void VersionNumber_LessThan()
        {
            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            LessThanTestAssert(v1, v2, true);
            LessThanTestAssert(v2, v1, false);

            LessThanTestAssert(v1, v4, true);
            LessThanTestAssert(v2, v4, true);
            LessThanTestAssert(v3, v4, true);
            LessThanTestAssert(v4, v1, false);
            LessThanTestAssert(v4, v2, false);
            LessThanTestAssert(v4, v3, false);

            LessThanTestAssert(v2, v3, false);
            LessThanTestAssert(v3, v2, false);
            LessThanTestAssert(v4, v5, false);
            LessThanTestAssert(v5, v4, false);
        }
        private void LessThanTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1 < v2, expectedValue,
                String.Format("VersionNumber.LessThanTest: [{0}] < ([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1 < v2, expectedValue));
        }
        [TestMethod]
        public void VersionNumber_GreaterThan()
        {
            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            GreaterThanTestAssert(v1, v2, false);
            GreaterThanTestAssert(v2, v1, true);

            GreaterThanTestAssert(v1, v4, false);
            GreaterThanTestAssert(v2, v4, false);
            GreaterThanTestAssert(v3, v4, false);
            GreaterThanTestAssert(v4, v1, true);
            GreaterThanTestAssert(v4, v2, true);
            GreaterThanTestAssert(v4, v3, true);

            GreaterThanTestAssert(v2, v3, false);
            GreaterThanTestAssert(v3, v2, false);
            GreaterThanTestAssert(v4, v5, false);
            GreaterThanTestAssert(v5, v4, false);
        }
        private void GreaterThanTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1 > v2, expectedValue,
                String.Format("VersionNumber.LessThanTest: [{0}] > ([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1 > v2, expectedValue));
        }
        [TestMethod]
        public void VersionNumber_LessThanOrEqual()
        {
            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            LessThanOrEqualTestAssert(v1, v2, true);
            LessThanOrEqualTestAssert(v2, v1, false);

            LessThanOrEqualTestAssert(v1, v4, true);
            LessThanOrEqualTestAssert(v2, v4, true);
            LessThanOrEqualTestAssert(v3, v4, true);
            LessThanOrEqualTestAssert(v4, v1, false);
            LessThanOrEqualTestAssert(v4, v2, false);
            LessThanOrEqualTestAssert(v4, v3, false);

            LessThanOrEqualTestAssert(v2, v3, true);
            LessThanOrEqualTestAssert(v3, v2, true);
            LessThanOrEqualTestAssert(v4, v5, true);
            LessThanOrEqualTestAssert(v5, v4, true);
        }
        private void LessThanOrEqualTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1 <= v2, expectedValue,
                String.Format("VersionNumber.LessThanOrEqualTest: [{0}] <= ([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1 <= v2, expectedValue));
        }
        [TestMethod]
        public void VersionNumber_GreaterThanOrEqual()
        {
            VersionNumber v1 = new VersionNumber(1, 1);
            VersionNumber v2 = new VersionNumber(1, 2);
            VersionNumber v3 = new VersionNumber(1, 2);
            VersionNumber v4 = new VersionNumber(2, 1);
            VersionNumber v5 = new VersionNumber(2, 1);

            GreaterThanOrEqualTestAssert(v1, v2, false);
            GreaterThanOrEqualTestAssert(v2, v1, true);

            GreaterThanOrEqualTestAssert(v1, v4, false);
            GreaterThanOrEqualTestAssert(v2, v4, false);
            GreaterThanOrEqualTestAssert(v3, v4, false);
            GreaterThanOrEqualTestAssert(v4, v1, true);
            GreaterThanOrEqualTestAssert(v4, v2, true);
            GreaterThanOrEqualTestAssert(v4, v3, true);

            GreaterThanOrEqualTestAssert(v2, v3, true);
            GreaterThanOrEqualTestAssert(v3, v2, true);
            GreaterThanOrEqualTestAssert(v4, v5, true);
            GreaterThanOrEqualTestAssert(v5, v4, true);
        }
        private void GreaterThanOrEqualTestAssert(VersionNumber v1, VersionNumber v2, bool expectedValue)
        {
            Assert.AreEqual(v1 >= v2, expectedValue,
                String.Format("VersionNumber.GreaterThanOrEqualTest: [{0}] >= ([{1}]) returned {2}. Expected: {3}",
                v1.ToString(), v2.ToString(), v1 >= v2, expectedValue));
        }

        [TestMethod]
        public void VersionNumber_EqualsNull()
        {
            VersionNumber v10 = new VersionNumber(1, 0);
            VersionNumber nullVersion = null;
            Assert.IsTrue(v10 != nullVersion);
            Assert.IsTrue(nullVersion != v10);
            v10 = null;
            Assert.IsTrue(v10 == nullVersion);
            Assert.IsTrue(nullVersion == v10);

        }

        [TestMethod]
        public void VersionNumber_Parsing()
        {
            Test(() =>
            {
                var msg = string.Empty;
                try
                {
                    var x = VersionNumber.Parse("asdf");
                }
                catch (Exception e)
                {
                    msg = e.Message;
                }
                var expected = SenseNet.ContentRepository.Storage.SR.GetString(SenseNet.ContentRepository.Storage.SR.Exceptions.VersionNumber.InvalidVersionFormat);
                Assert.IsTrue(msg.StartsWith(expected));
            });
        }
        [TestMethod]
        public void VersionNumber_ParsingVersionStatus()
        {
            Test(() =>
            {

                var statusString = "q";
                var msg = string.Empty;
                try
                {
                    var x = VersionNumber.GetVersionStatus(statusString);
                }
                catch (Exception e)
                {
                    msg = e.Message;
                }
                var expected = String.Format(SenseNet.ContentRepository.Storage.SR.GetString(SenseNet.ContentRepository.Storage.SR.Exceptions.VersionNumber.InvalidVersionStatus_1), statusString);

                Assert.AreEqual(expected, msg);
            });
        }
    }
}