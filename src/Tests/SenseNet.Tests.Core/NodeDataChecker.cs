using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Testing;

namespace SenseNet.Tests.Core
{
    public class NodeDataChecker
    {
        public static void Assert_DynamicPropertiesAreEqualExceptBinaries(NodeData expected, NodeData actual, params string[] excludedProperties)
        {
            // prepare collections
            var expectedProps = (Dictionary<int, object>)(new ObjectAccessor(expected).GetField("dynamicData"));
            foreach (var propName in excludedProperties)
            {
                var pt = PropertyType.GetByName(propName);
                expectedProps.Add(pt.Id, expected.GetDynamicRawData(pt));
            }
            var actualProps = (Dictionary<int, object>)(new ObjectAccessor(actual).GetField("dynamicData"));

            // Compare signatures
            var expectedSignature = expectedProps.Keys.OrderBy(y => y).ToArray();
            var actualSignature = actualProps.Keys.OrderBy(y => y).ToArray();
            var expectedNames = expectedProps.Keys.Select(x => ActiveSchema.PropertyTypes.GetItemById(x).Name).OrderBy(y => y).ToArray();
            var actualNames = actualProps.Keys.Select(x => ActiveSchema.PropertyTypes.GetItemById(x).Name).OrderBy(y => y).ToArray();
            Assert_AreEqual(expectedNames, actualNames, "DynamicPropertySignature");

            // Compare properties
            foreach (var key in expectedSignature)
            {
                var propertyType = NodeTypeManager.Current.PropertyTypes.GetItemById(key);
                var expectedValue = expectedProps[key];
                var actualValue = actualProps[key];
                switch (propertyType.DataType)
                {
                    case DataType.String:
                    case DataType.Text:
                        Assert_AreEqual((string)expectedValue, (string)actualValue, propertyType.Name);
                        break;
                    case DataType.Int:
                        try
                        {
                            Assert_AreEqual((int?)expectedValue, (int?)actualValue, propertyType.Name);
                        }
                        catch (InvalidCastException)
                        {
                            Assert_AreEqual((int)expectedValue, (int)actualValue, propertyType.Name);
                        }
                        break;
                    case DataType.Currency:
                        Assert_AreEqual((decimal?)expectedValue, (decimal?)actualValue, propertyType.Name);
                        break;
                    case DataType.DateTime:
                        Assert_AreEqual((DateTime?)expectedValue, (DateTime?)actualValue, propertyType.Name);
                        break;
                    case DataType.Binary:
                        break;
                    case DataType.Reference:
                        Assert_AreEqual((IEnumerable<int>)expectedValue, (IEnumerable<int>)actualValue, $"ReferenceProperty '{propertyType.Name}'");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void Assert_AreEqual(DateTime? expected, DateTime? actual, string name)
        {
            if (expected == null && actual == null)
                return;
            if (expected == null)
                throw new Exception($"Expected and actual {name} are not equal. Expected: <null>, Actual: {actual}");
            if (actual == null)
                throw new Exception($"Expected and actual {name} are not equal. Expected: {expected}, Actual: <null>");

            var expectedDiff = 500;
            var diff = (actual.Value - expected.Value).TotalMilliseconds;
            if (diff > expectedDiff || diff < -expectedDiff)
                throw new Exception(
                    $"Difference of expected and actual {name} is too big. Expected: {expectedDiff}, Actual: {diff} milliseconds.");
        }
        private static void Assert_AreEqual(string expected, string actual, string name)
        {
            if (expected != actual)
                throw new Exception(
                    $"Expected and actual {name} are not equal. Expected: {expected}, Actual: {actual}");
        }
        private static void Assert_AreEqual(IEnumerable<string> expected, IEnumerable<string> actual, string name)
        {
            var exp = string.Join(",", expected.OrderBy(x => x));
            var act = string.Join(",", actual.OrderBy(x => x));
            if (exp != act)
                throw new Exception(
                    $"Expected and actual {name} are not equal. Expected: {exp}, Actual: {act}");
        }
        private static void Assert_AreEqual(IEnumerable<int> expected, IEnumerable<int> actual, string name)
        {
            if (expected == null && actual == null)
                return;

            var exp = string.Join(",", expected.OrderBy(x => x).Select(x => x.ToString()));
            var act = string.Join(",", actual.OrderBy(x => x).Select(x => x.ToString()));
            if (exp != act)
                throw new Exception(
                    $"Expected and actual {name} are not equal. Expected: {exp}, Actual: {act}");
        }
        private static void Assert_AreEqual(int expected, int actual, string name)
        {
            if (expected != actual)
                throw new Exception(
                    $"Expected and actual {name} are not equal. Expected: {expected}, Actual: {actual}");
        }
        private static void Assert_AreEqual(int? expected, int? actual, string name)
        {
            if (expected == null && actual == null)
                return;
            if (expected != actual)
                throw new Exception(
                    $"Expected and actual {name} are not equal. Expected: {expected}, Actual: {actual}");
        }
        private static void Assert_AreEqual(decimal? expected, decimal? actual, string name)
        {
            if (expected == null && actual == null)
                return;
            if (expected != actual)
                throw new Exception(
                    $"Expected and actual {name} are not equal. Expected: {expected}, Actual: {actual}");
        }
    }
}
