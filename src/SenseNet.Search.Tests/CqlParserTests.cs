using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class CqlParserTests
    {
        [TestMethod]
        public void CqlParser_AstToString_FromOriginalLuceneQueryParserSyntax()
        {
            Test("value", "_Text:value");
            Test("VALUE", "_Text:VALUE");
            Test("Value", "_Text:Value");
            Test("Value1", "_Text:Value1");
            Test("-Value1", "-_Text:Value1");
            Test("+Value1", "+_Text:Value1");
            Test("Value1 -Value2 +Value3 Value4", "_Text:Value1 -_Text:Value2 +_Text:Value3 _Text:Value4");
            Test("Field1:Value1");
            Test("#Field1:Value1");
            Test("-Field1:Value1");
            Test("+Field1:Value1");
            Test("Field1:Value1 Field2:Value2 Field3:Value3");
            Test("F1:V1 -F2:V2 +F3:V3 F4:V4");
            Test("f1:v1 f2:v2");
            Test("f1:v1 f2:v2 (f3:v3 f4:v4 (f5:v5 f6:v6))");
            Test("f1:v1 (f2:v2 (f3:v3 f4:v4))");
            Test("aaa AND +bbb", "+_Text:aaa +_Text:bbb");
            Test("te?t", "_Text:te?t");
            Test("test*", "_Text:test*");
            Test("te*t", "_Text:te*t");
            Test("roam~", "_Text:roam");
            Test("roam~" + SnQuery.DefaultFuzzyValue.ToString(CultureInfo.InvariantCulture), "_Text:roam");
            Test("roam~0.8", "_Text:roam~0.8");
            Test("\"jakarta apache\"~10", "_Text:'jakarta apache'~10");
            Test("mod_date:[20020101 TO 20030101]");
            Test("title:{Aida TO Carmen}");
            Test("jakarta apache", "_Text:jakarta _Text:apache");
            Test("jakarta^4 apache", "_Text:jakarta^4 _Text:apache");
            Test("\"jakarta apache\"^4 \"Apache Lucene\"", "_Text:'jakarta apache'^4 _Text:'Apache Lucene'");
            Test("\"jakarta apache\" jakarta", "_Text:'jakarta apache' _Text:jakarta");
            Test("\"jakarta apache\" OR jakarta", "_Text:'jakarta apache' _Text:jakarta");
            Test("\"jakarta apache\" AND \"Apache Lucene\"", "+_Text:'jakarta apache' +_Text:'Apache Lucene'");
            Test("+jakarta lucene", "+_Text:jakarta _Text:lucene");
            Test("\"jakarta apache\" NOT \"Apache Lucene\"", "_Text:'jakarta apache' -_Text:'Apache Lucene'");
            Test("NOT \"jakarta apache\"", "-_Text:'jakarta apache'");
            Test("\"jakarta apache\" -\"Apache Lucene\"", "_Text:'jakarta apache' -_Text:'Apache Lucene'");
            Test("(jakarta OR apache) AND website", "+(_Text:jakarta _Text:apache) +_Text:website");
            Test("title:(+return +\"pink panther\")", "+title:return +title:'pink panther'");
        }
        [TestMethod]
        public void CqlParser_AstToString_PredicateTypes()
        {
            SnQuery q;
            q = Test("Name:aaa"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Id:1000"); Assert.AreEqual(typeof(LongNumberPredicate), q.QueryTree.GetType());
            q = Test("Value:3.14"); Assert.AreEqual(typeof(DoubleNumberPredicate), q.QueryTree.GetType());
        }

        [TestMethod]
        public void CqlParser_AstToString_CqlExtension_Ranges()
        {
            SnQuery q;
            q = Test("Name:<aaa"); Assert.AreEqual(typeof(TextRange), q.QueryTree.GetType());
            q = Test("Name:>aaa"); Assert.AreEqual(typeof(TextRange), q.QueryTree.GetType());
            q = Test("Name:<=aaa"); Assert.AreEqual(typeof(TextRange), q.QueryTree.GetType());
            q = Test("Name:>=aaa"); Assert.AreEqual(typeof(TextRange), q.QueryTree.GetType());
            q = Test("Id:<1000"); Assert.AreEqual(typeof(LongRange), q.QueryTree.GetType());
            q = Test("Id:>1000"); Assert.AreEqual(typeof(LongRange), q.QueryTree.GetType());
            q = Test("Id:<=1000"); Assert.AreEqual(typeof(LongRange), q.QueryTree.GetType());
            q = Test("Id:>=1000"); Assert.AreEqual(typeof(LongRange), q.QueryTree.GetType());
            q = Test("Value:<3.14");  Assert.AreEqual(typeof(DoubleRange), q.QueryTree.GetType());
            q = Test("Value:>3.14");  Assert.AreEqual(typeof(DoubleRange), q.QueryTree.GetType());
            q = Test("Value:<=3.14"); Assert.AreEqual(typeof(DoubleRange), q.QueryTree.GetType());
            q = Test("Value:>=3.14"); Assert.AreEqual(typeof(DoubleRange), q.QueryTree.GetType());
        }
        [TestMethod]
        public void CqlParser_AstToString_CqlExtension_SpecialChars()
        {
            Test("F1:V1 && F2:V2", "+F1:V1 +F2:V2");
            Test("F1:V1 || F2:V2", "F1:V1 F2:V2");
            Test("F1:V1 && F2:<>V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !F2:V2", "+F1:V1 -F2:V2");
        }
        [TestMethod]
        public void CqlParser_AstToString_CqlExtension_Comments()
        {
            Test("F1:V1 //asdf", "F1:V1");
            Test("+F1:V1 /*asdf*/ +F2:V2 /*qwer*/", "+F1:V1 +F2:V2");
        }
        [TestMethod]
        public void CqlParser_AstToString_CqlExtension_Controls()
        {
            // ".SELECT";
            // ".SKIP";
            // ".TOP";
            // ".SORT";
            // ".REVERSESORT";
            // ".AUTOFILTERS";
            // ".LIFESPAN";
            // ".COUNTONLY";
            // ".QUICK";

            var q = Test("F1:V1");
            Assert.AreEqual(int.MaxValue, q.Top);
            Assert.AreEqual(0, q.Skip);
            Assert.AreEqual(false, q.CountOnly);
            Assert.AreEqual(FilterStatus.Default, q.EnableAutofilters);
            Assert.AreEqual(FilterStatus.Default, q.EnableLifespanFilter);
            Assert.AreEqual(QueryExecutionMode.Default, q.QueryExecutionMode);
            Assert.AreEqual(0, q.Sort.Length);

            q = Test("F1:V1 .TOP:42", "F1:V1"); Assert.AreEqual(42, q.Top);
            q = Test("F1:V1 .SKIP:42", "F1:V1"); Assert.AreEqual(42, q.Skip);
            q = Test("F1:V1 .COUNTONLY", "F1:V1"); Assert.AreEqual(true, q.CountOnly);
            q = Test("F1:V1 .AUTOFILTERS:ON", "F1:V1"); Assert.AreEqual(FilterStatus.Enabled, q.EnableAutofilters);
            q = Test("F1:V1 .AUTOFILTERS:OFF", "F1:V1"); Assert.AreEqual(FilterStatus.Disabled, q.EnableAutofilters);
            q = Test("F1:V1 .LIFESPAN:ON", "F1:V1"); Assert.AreEqual(FilterStatus.Enabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .LIFESPAN:OFF", "F1:V1"); Assert.AreEqual(FilterStatus.Disabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .QUICK", "F1:V1"); Assert.AreEqual(QueryExecutionMode.Quick, q.QueryExecutionMode);

            q = Test("F1:V1 .SORT:F1", "F1:V1"); Assert.AreEqual("F1 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .REVERSESORT:F1", "F1:V1"); Assert.AreEqual("F1 DESC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .SORT:F2", "F1:V1"); Assert.AreEqual("F1 ASC, F2 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .REVERSESORT:F3 .SORT:F2", "F1:V1"); Assert.AreEqual("F1 ASC, F3 DESC, F2 ASC", SortToString(q.Sort));

            TestError("F1:V1 .unknownkeyword", typeof(ParserException));
        }
        [TestMethod]
        public void CqlParser_Errors()
        {
        }

        private SnQuery Test(string queryText, string expected = null)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();

            var snQuery = parser.Parse(queryText, queryContext);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(snQuery.QueryTree);
            var actualResult = visitor.Output;

            Assert.AreEqual(expected ?? queryText, actualResult);
            return snQuery;
        }
        private void TestError(string queryText, Type expectedExceptionType)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();
            Exception  thrownException = null;
            try
            {
                parser.Parse(queryText, queryContext);
            }
            catch (Exception e)
            {
                thrownException = e;
            }
            if (thrownException == null)
                Assert.Fail("Any exception wasn't thrown");
            if (thrownException.GetType() != expectedExceptionType)
                Assert.Fail($"{thrownException.GetType().Name} was thrown but {expectedExceptionType.Name} was expected.");
        }

        private string SortToString(SortInfo[] sortInfo)
        {
            return string.Join(", ", sortInfo.Select(s => $"{s.FieldName} {(s.Reverse ? "DESC" : "ASC")}").ToArray());
        }



        [TestMethod]
        public void CqlParser_UsedFieldNames()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfo_int() },
                {"Name", new TestPerfieldIndexingInfo_string() },
                {"Field1", new TestPerfieldIndexingInfo_string() },
                {"Field2", new TestPerfieldIndexingInfo_string() }
            };
            var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
            var parser = new CqlParser();
            var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2) +(Field1:asdf)";
            var expected = "Field1, Field2, Id, Name";

            var snQuery = parser.Parse(queryText, queryContext);

            var actual = string.Join(", ", snQuery.UsedFieldNames.OrderBy(x => x).ToArray());
            Assert.AreEqual(expected, actual);
        }

    }
}
