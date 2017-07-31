using System;
using System.Globalization;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Lucene29;
using SenseNet.Search.Parser;
using SenseNet.Search.Tests.Implementations;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class SnSarchToLucene29CompilerTests
    {
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_01()
        {
            Test("value", "_Text:value");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_02()
        {
            Test("VALUE", "_Text:value");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_03()
        {
            Test("Value", "_Text:value");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_04()
        {
            Test("Value1", "_Text:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_05()
        {
            Test("-Value1", "-_Text:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_06()
        {
            Test("+Value1", "+_Text:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_07()
        {
            Test("Value1 -Value2 +Value3 Value4", "_Text:value1 -_Text:value2 +_Text:value3 _Text:value4");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_08()
        {
            Test("Field1:Value1", "Field1:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_09()
        {
            Test("#Field1:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_10()
        {
            Test("-Field1:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_11()
        {
            Test("+Field1:value1");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_12()
        {
            Test("Field1:value1 Field2:value2 Field3:value3");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_13()
        {
            Test("F1:v1 -F2:v2 +F3:v3 F4:v4");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_14()
        {
            Test("f1:v1 f2:v2");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_15()
        {
            Test("f1:v1 f2:v2 (f3:v3 f4:v4 (f5:v5 f6:v6))");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_16()
        {
            Test("f1:v1 (f2:v2 (f3:v3 f4:v4))");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_17()
        {
            Test("aaa AND +bbb", "+_Text:aaa +_Text:bbb");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_18()
        {
            Test("te?t", "_Text:te?t");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_19()
        {
            Test("test*", "_Text:test*");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_20()
        {
            Test("te*t", "_Text:te*t");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_21()
        {
            Test("roam~", "_Text:roam~0.5");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_22()
        {
            Test("roam~" + SnQuery.DefaultFuzzyValue.ToString(CultureInfo.InvariantCulture), "_Text:roam~0.5");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_23()
        {
            Test("roam~0.8", "_Text:roam~0.8");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_24()
        {
            Test("\"jakarta apache\"~10", "_Text:\"jakarta apache\"~10");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_25()
        {
            Test("mod_date:[20020101 TO 20030101]");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_26()
        {
            Test("title:{Aida TO Carmen}", "title:{aida TO carmen}");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_27()
        {
            Test("jakarta apache", "_Text:jakarta _Text:apache");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_28()
        {
            Test("jakarta^4 apache", "_Text:jakarta^4 _Text:apache");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_29()
        {
            Test("\"jakarta apache\"^4 \"Apache Lucene\"", "_Text:\"jakarta apache\"^4 _Text:\"apache lucene\"");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_30()
        {
            Test("\"jakarta apache\" jakarta", "_Text:\"jakarta apache\" _Text:jakarta");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_31()
        {
            Test("\"jakarta apache\" OR jakarta", "_Text:\"jakarta apache\" _Text:jakarta");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_32()
        {
            Test("\"jakarta apache\" AND \"Apache Lucene\"", "+_Text:\"jakarta apache\" +_Text:\"apache lucene\"");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_33()
        {
            Test("+jakarta lucene", "+_Text:jakarta _Text:lucene");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_34()
        {
            Test("\"jakarta apache\" NOT \"Apache Lucene\"", "_Text:\"jakarta apache\" -_Text:\"apache lucene\"");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_35()
        {
            Test("NOT \"jakarta apache\"", "-_Text:\"jakarta apache\"");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_36()
        {
            Test("\"jakarta apache\" -\"Apache Lucene\"", "_Text:\"jakarta apache\" -_Text:\"apache lucene\"");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_37()
        {
            Test("(jakarta OR apache) AND website", "+(_Text:jakarta _Text:apache) +_Text:website");
        }
        [TestMethod]
        public void Search_Compiler_Luc29_OriginalTest_38()
        {
            Test("title:(+return +\"pink panther\")", "+title:return +title:\"pink panther\"");
        }

        [TestMethod]
        public void Search_Compiler_Luc29__CqlExtension_Ranges_Text()
        {
            Query q;
            q = Test("Name:<aaa"); Assert.IsInstanceOfType(q, typeof(TermRangeQuery));
            q = Test("Name:>aaa"); Assert.IsInstanceOfType(q, typeof(TermRangeQuery));
            q = Test("Name:<=aaa"); Assert.IsInstanceOfType(q, typeof(TermRangeQuery));
            q = Test("Name:>=aaa"); Assert.IsInstanceOfType(q, typeof(TermRangeQuery));
        }
        [TestMethod]
        public void Search_Compiler_Luc29__CqlExtension_Ranges_Int()
        {
            CheckNumericRange(Test("Id:<1000"), typeof(int));
            CheckNumericRange(Test("Id:>1000"), typeof(int));
            CheckNumericRange(Test("Id:<=1000"), typeof(int));
            CheckNumericRange(Test("Id:>=1000"), typeof(int));
        }
        [TestMethod]
        public void Search_Compiler_Luc29__CqlExtension_Ranges_Long()
        {
            CheckNumericRange(Test("LongField1:<1000000"), typeof(long));
            CheckNumericRange(Test("LongField1:>1000000"), typeof(long));
            CheckNumericRange(Test("LongField1:<=1000000"), typeof(long));
            CheckNumericRange(Test("LongField1:>=1000000"), typeof(long));
        }
        [TestMethod]
        public void Search_Compiler_Luc29__CqlExtension_Ranges_15()
        {
            CheckNumericRange(Test("SingleField1:<3.14"), typeof(float));
            CheckNumericRange(Test("SingleField1:>3.14"), typeof(float));
            CheckNumericRange(Test("SingleField1:<=3.14"), typeof(float));
            CheckNumericRange(Test("SingleField1:>=3.14"), typeof(float));
        }
        [TestMethod]
        public void Search_Compiler_Luc29__CqlExtension_Ranges_17()
        {
            CheckNumericRange(Test("DoubleField1:<3.1415"), typeof(double));
            CheckNumericRange(Test("DoubleField1:>3.1415"), typeof(double));
            CheckNumericRange(Test("DoubleField1:<=3.1415"), typeof(double));
            CheckNumericRange(Test("DoubleField1:>=3.1415"), typeof(double));
        }

        public Query Test(string queryText, string expected = null)
        {
            expected = expected ?? queryText;

            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"_Text", new TestPerfieldIndexingInfo_string()},
                {"#Field1", new TestPerfieldIndexingInfo_string()},
                {"Field1", new TestPerfieldIndexingInfo_string()},
                {"Field2", new TestPerfieldIndexingInfo_string()},
                {"Field3", new TestPerfieldIndexingInfo_string()},
                {"F1", new TestPerfieldIndexingInfo_string()},
                {"F2", new TestPerfieldIndexingInfo_string()},
                {"F3", new TestPerfieldIndexingInfo_string()},
                {"F4", new TestPerfieldIndexingInfo_string()},
                {"f1", new TestPerfieldIndexingInfo_string()},
                {"f2", new TestPerfieldIndexingInfo_string()},
                {"f3", new TestPerfieldIndexingInfo_string()},
                {"f4", new TestPerfieldIndexingInfo_string()},
                {"f5", new TestPerfieldIndexingInfo_string()},
                {"f6", new TestPerfieldIndexingInfo_string()},
                {"mod_date", new TestPerfieldIndexingInfo_int()},
                {"title", new TestPerfieldIndexingInfo_string()},
                {"Name", new TestPerfieldIndexingInfo_string()},
                {"Id", new TestPerfieldIndexingInfo_int()},
                {"DoubleField1", new TestPerfieldIndexingInfo_double()},
            };

            StorageContext.Search.ContentRepository =
                new TestSearchEngineSupport(indexingInfo);

            var queryContext = new TestQueryContext(QuerySettings.Default, 0, indexingInfo);
            var parser = new CqlParser();
            var snQuery = parser.Parse(queryText, queryContext);

            var compiler = new Lucene29Compiler();
            var lucQuery = compiler.Compile(snQuery, queryContext);

            var lqVisitor = new LucQueryToStringVisitor();
            lqVisitor.Visit(lucQuery.Query);
            var actual = lqVisitor.ToString();

            Assert.AreEqual(expected, actual);

            return lucQuery.Query;
        }
        private void CheckNumericRange(Query q, Type type)
        {
            var nq = q as NumericRangeQuery;
            Assert.IsNotNull(nq);
            var val = (nq.GetMin() ?? nq.GetMax()).GetType();
            Assert.AreEqual(type, (nq.GetMin() ?? nq.GetMax()).GetType());
        }
    }
}
