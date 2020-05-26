using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.OData;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataArrayTests : ODataTestBase
    {
        private struct TestItem
        {
            public int Value { get; }

            public TestItem(int value)
            {
                Value = value;
            }

            public TestItem(string src)
            {
                var numChars = src.Where(char.IsDigit).ToArray();
                Value = int.Parse(new string(numChars));
            }
        }

        private class TestItemArray : ODataArray<TestItem>
        {
            public TestItemArray(IEnumerable<TestItem> collection) : base(collection)
            {
            }
            public TestItemArray(string commaSeparated) : base(commaSeparated)
            {
            }

            public override TestItem Parse(string inputValue)
            {
                return new TestItem(inputValue);
            }
        }

        [TestMethod]
        public void OD_OdataArray_Creation_string()
        {
            var strings = new[] { "item1", "item2", "item3" };
            var array = new ODataArray<string>(strings);

            AssertSequenceEqual(strings, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_int()
        {
            var numbers = new[] { 1, 2, 42 };
            var array = new ODataArray<int>(numbers);
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_long()
        {
            var numbers = new[] { 1L, 2L, 42L };
            var array = new ODataArray<long>(numbers);
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_byte()
        {
            var numbers = new byte[] { 1, 2, 42 };
            var array = new ODataArray<byte>(numbers);
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_bool()
        {
            var predicates = new [] { true, false, true };
            var array = new ODataArray<bool>(predicates);
            AssertSequenceEqual(predicates, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_decimal()
        {
            var numbers = new[] { 1.1m, 2.1m, 42.1m };
            var array = new ODataArray<decimal>(numbers);
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_float()
        {
            var numbers = new[] { 1.1f, 2.1f, 42.1f };
            var array = new ODataArray<float>(numbers);
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_double()
        {
            var numbers = new[] { 1.1d, 2.1d, 42.1d };
            var array = new ODataArray<double>(numbers);
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Creation_TestItem()
        {
            var numbers = new[] { 1, 2, 42 };
            var items = numbers.Select(x => new TestItem(x)).ToArray();
            var array = new ODataArray<TestItem>(items);

            AssertSequenceEqual(numbers, array.Select(x => x.Value));
        }

        [TestMethod]
        public void OD_OdataArray_Parse_string()
        {
            var strings = new[] { "item1", "item2", "item3" };
            var array = new ODataArray<string>("item1, item2, item3");

            AssertSequenceEqual(strings, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_int()
        {
            var numbers = new[] { 1, 2, 42 };
            var array = new ODataArray<int>("1, 2, 42");
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_long()
        {
            var numbers = new[] { 1L, 2L, 42L };
            var array = new ODataArray<long>("1, 2, 42");
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_byte()
        {
            var numbers = new byte[] { 1, 2, 42 };
            var array = new ODataArray<byte>("1, 2, 42");
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_bool()
        {
            var predicates = new[] { true, false, true };
            var array = new ODataArray<bool>("true, false, true");
            AssertSequenceEqual(predicates, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_decimal()
        {
            var numbers = new[] { 1.1m, 2.1m, 42.1m };
            var array = new ODataArray<decimal>("1.1, 2.1, 42.1");
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_float()
        {
            var numbers = new[] { 1.1f, 2.1f, 42.1f };
            var array = new ODataArray<float>("1.1, 2.1, 42.1");
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_double()
        {
            var numbers = new[] { 1.1d, 2.1d, 42.1d };
            var array = new ODataArray<double>("1.1, 2.1, 42.1");
            AssertSequenceEqual(numbers, array);
        }
        [TestMethod]
        public void OD_OdataArray_Parse_TestItem()
        {
            var numbers = new[] { 11, 21, 421 };
            var array = new TestItemArray("#1.1, #2.1, #42.1");
            AssertSequenceEqual(numbers, array.Select(x => x.Value));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void OD_OdataArray_Parse_TestItem_Error()
        {
            var numbers = new[] { 1, 2, 42 };
            var array = new ODataArray<TestItem>("#1.1, #2.1, #42.1");

            AssertSequenceEqual(numbers, array.Select(x => x.Value));
        }

    }
}
