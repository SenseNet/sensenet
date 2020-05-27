using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.OData;
using SenseNet.ODataTests.Accessors;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataArrayTests : ODataTestBase
    {
        public struct TestItem
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

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                return this.Value.Equals(((TestItem) obj).Value);
            }

            public override string ToString()
            {
                return "#" + Value;
            }
        }

        public class TestItemArray : ODataArray<TestItem>
        {
            public TestItemArray(IEnumerable<TestItem> collection) : base(collection) { }
            public TestItemArray(string commaSeparated) : base(commaSeparated) { }
            public TestItemArray(object[] items) : base(items) { }

            public override TestItem Parse(string inputValue)
            {
                return new TestItem(inputValue);
            }
            public override TestItem Convert(object inputValue)
            {
                return new TestItem(inputValue.ToString());
            }
        }

        [TestMethod]
        public void OD_OdataArray_Creation_string()
        {
            var strings = new[] { "item1", "item2", "item3" };
            var array = new ODataArray<string>((IEnumerable<string>)strings);

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
        public void OD_OdataArray_Parse_decimal_US()
        {
            using (CultureHack("en-US"))
            {
                var numbers = new[] { 1.1m, 2.1m, 42.1m };
                var array = new ODataArray<decimal>("1.1, 2.1, 42.1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_decimal_HU()
        {
            using (CultureHack("hu-HU"))
            {
                var numbers = new[] { 1.1m, 2.1m, 42.1m };
                var array = new ODataArray<decimal>("1.1, 2.1, 42.1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_decimal_HU_Comma()
        {
            using (CultureHack("hu"))
            {
                var numbers = new[] { 1.1m, 2.1m, 42.1m };
                var array = new ODataArray<decimal>("; 1,1; 2,1; 42,1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_float_US()
        {
            using (CultureHack("en-US"))
            {
                var numbers = new[] { 1.1f, 2.1f, 42.1f };
                var array = new ODataArray<float>("1.1, 2.1, 42.1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_float_HU()
        {
            using (CultureHack("hu-HU"))
            {
                var numbers = new[] { 1.1f, 2.1f, 42.1f };
                var array = new ODataArray<float>("1.1, 2.1, 42.1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_float_HU_Comma()
        {
            using (CultureHack("hu"))
            {
                var numbers = new[] { 1.1f, 2.1f, 42.1f };
                var array = new ODataArray<float>("; 1,1; 2,1; 42,1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_double_US()
        {
            using (CultureHack("en-US"))
            {
                var numbers = new[] { 1.1d, 2.1d, 42.1d };
                var array = new ODataArray<double>("1.1, 2.1, 42.1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_double_HU()
        {
            using (CultureHack("hu-HU"))
            {
                var numbers = new[] { 1.1d, 2.1d, 42.1d };
                var array = new ODataArray<double>("1.1, 2.1, 42.1");
                AssertSequenceEqual(numbers, array);
            }
        }
        [TestMethod]
        public void OD_OdataArray_Parse_double_HU_Comma()
        {
            using (CultureHack("hu"))
            {
                var numbers = new[] { 1.1d, 2.1d, 42.1d };
                var array = new ODataArray<double>("; 1,1; 2,1; 42,1");
                AssertSequenceEqual(numbers, array);
            }
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
            // Incompetent type. Needs TestItemArray
            var array = new ODataArray<TestItem>("#1.1, #2.1, #42.1");
            AssertSequenceEqual(numbers, array.Select(x => x.Value));
        }

        [TestMethod]
        public void OD_OdataArray_Parse_Separator()
        {
            var words = new[] {"word1", "word2"};
            // Comma is the default but can be specified.
            AssertSequenceEqual(words, new ODataArray<string>("word1,word2"));
            AssertSequenceEqual(words, new ODataArray<string>(",word1,word2"));
            AssertSequenceEqual(words, new ODataArray<string>(";word1;word2"));
            AssertSequenceEqual(words, new ODataArray<string>(":word1:word2"));
            AssertSequenceEqual(words, new ODataArray<string>("|word1|word2"));

            words = new[] { "", "word1", "", "word2", "" };
            // First char is a separator so leading one empty string needs two characters.
            AssertSequenceEqual(words, new ODataArray<string>(",,word1,,word2,"));
            AssertSequenceEqual(words, new ODataArray<string>(";;word1;;word2;"));
            AssertSequenceEqual(words, new ODataArray<string>("::word1::word2:"));
            AssertSequenceEqual(words, new ODataArray<string>("||word1||word2|"));
        }

        /* =============================================================== Tools */

        private IDisposable CultureHack(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            return new Swindler<CultureInfo>(culture,
                () => Thread.CurrentThread.CurrentCulture,
                (c) => Thread.CurrentThread.CurrentCulture = c);
        }

    }
}
