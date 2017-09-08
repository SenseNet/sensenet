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
        public void SnQuery_Parser_AstToString_FromOriginalLuceneQueryParserSyntax()
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
        public void SnQuery_Parser_AstToString_AdditionalTests()
        {
            Test("42value", "_Text:42value");
            Test("42v?lue", "_Text:42v?lue");
            Test("42valu*", "_Text:42valu*");
            Test("Name:42aAa", "Name:42aAa");
            Test("Name:42a?a");
            Test("Name:42aa*");
            Test("(Name:aaa Id:2)", "Name:aaa Id:2"); // unnecessary parenthesis
            TestError("Name:\"aaa");
        }
        [TestMethod]
        public void SnQuery_Parser_AstToString_EmptyQueries()
        {
            var empty = SnQuery.EmptyText;
            Test($"+(+F1:{empty} +F2:aaa*) +F3:bbb", "+(+F2:aaa*) +F3:bbb");
            Test($"+(+F1:{empty} +(F2:V2 F3:V3)) +F3:bbb", "+(+(F2:V2 F3:V3)) +F3:bbb");
            Test($"+(+F1:{empty} +F2:{empty}) +F3:bbb", "+F3:bbb");

            Test($"F1:[{empty} TO max]", "F1:<=max");
            Test($"F1:[min TO {empty}]", "F1:>=min");
            Test($"F1:[{empty} TO ]", "");
            Test($"F1:[ TO {empty}]", "");
            Test($"F1:[\"{empty}\" TO max]", "F1:<=max");
            Test($"F1:[min TO \"{empty}\"]", "F1:>=min");

            TestError($"F1:[{empty} TO {empty}]");
        }

        [TestMethod]
        public void SnQuery_Parser_AstToString_PredicateTypes()
        {
            SnQuery q;
            q = Test("Name:aaa"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Id:1000"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
            q = Test("Value:3.14"); Assert.AreEqual(typeof(TextPredicate), q.QueryTree.GetType());
        }

        [TestMethod]
        public void SnQuery_Parser_AstToString_CqlExtension_Ranges()
        {
            SnQuery q;
            q = Test("Name:<aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:>aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:<=aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Name:>=aaa"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:<1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:>1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:<=1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Id:>=1000"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:<3.14");  Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:>3.14");  Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:<=3.14"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
            q = Test("Value:>=3.14"); Assert.AreEqual(typeof(RangePredicate), q.QueryTree.GetType());
        }
        [TestMethod]
        public void SnQuery_Parser_AstToString_CqlExtension_SpecialChars()
        {
            Test("F1:(V1 OR V2)", "F1:V1 F1:V2");
            Test("F1:(V1 AND V2)", "+F1:V1 +F1:V2");
            Test("F1:(+V1 +V2 -V3)", "+F1:V1 +F1:V2 -F1:V3");
            Test("F1:(+V1 -(V2 V3))", "+F1:V1 -(F1:V2 F1:V3)");
            Test("F1:(+V1 !V2)", "+F1:V1 -F1:V2");

            Test("F1:V1 && F2:V2", "+F1:V1 +F2:V2");
            Test("F1:V1 || F2:V2", "F1:V1 F2:V2");
            Test("F1:V1 && F2:<>V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !F2:V2", "+F1:V1 -F2:V2");
            Test("F1:V1 && !(F2:V2 || F3:V3)", "+F1:V1 -(F2:V2 F3:V3)");

            Test("+Id:<1000\n+Name:a*", "+Id:<1000 +Name:a*");
            Test("Name:\\*", "Name:*");
            Test("Name:a<a");
            Test("Name:a>a");
            Test("Name<aaa", "_Text:Name<aaa");
            Test("Name>aaa", "_Text:Name>aaa");
            Test("Name:/root");
            Test("Number:42.15.78", "Number:42.15. _Text:78");

            Test("Aspect.Field1:aaa");
            Test("Aspect1.Field1:aaa");

            TestError("42.Field1:aaa");
            TestError("Name:a* |? Id:<1000");
            TestError("Name:a* &? Id:<1000");
            TestError("\"Name\":aaa");
            TestError("'Name':aaa");
            TestError("Name:\"aaa\\");
            TestError("Name:\"aaa\\\"");
            TestError("Name:<>:");
        }
        [TestMethod]
        public void SnQuery_Parser_AstToString_CqlExtension_Comments()
        {
            Test("F1:V1 //asdf", "F1:V1");
            Test("+F1:V1 /*asdf*/ +F2:V2 /*qwer*/", "+F1:V1 +F2:V2");

            Test("Name:/* \n */aaa", "Name:aaa");
            Test("+Name:aaa //comment\n+Id:<42", "+Name:aaa +Id:<42");
            Test("+Name:aaa//comment\n+Id:<42", "+Name:aaa//comment +Id:<42");
            Test("+Name:\"aaa\"//comment\n+Id:<42", "+Name:aaa +Id:<42");
            Test("Name:aaa /*unterminatedblockcomment", "Name:aaa");
        }
        [TestMethod]
        public void SnQuery_Parser_AstToString_CqlExtension_Controls()
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
            Assert.AreEqual(null, q.Projection);
            Assert.AreEqual(0, q.Sort.Length);

            q = Test("F1:V1 .TOP:42", "F1:V1"); Assert.AreEqual(42, q.Top);
            q = Test("F1:V1 .SKIP:42", "F1:V1"); Assert.AreEqual(42, q.Skip);
            q = Test("F1:V1 .COUNTONLY", "F1:V1"); Assert.AreEqual(true, q.CountOnly);
            q = Test("F1:V1 .AUTOFILTERS:ON", "F1:V1"); Assert.AreEqual(FilterStatus.Enabled, q.EnableAutofilters);
            q = Test("F1:V1 .AUTOFILTERS:OFF", "F1:V1"); Assert.AreEqual(FilterStatus.Disabled, q.EnableAutofilters);
            q = Test("F1:V1 .LIFESPAN:ON", "F1:V1"); Assert.AreEqual(FilterStatus.Enabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .LIFESPAN:OFF", "F1:V1"); Assert.AreEqual(FilterStatus.Disabled, q.EnableLifespanFilter);
            q = Test("F1:V1 .QUICK", "F1:V1"); Assert.AreEqual(QueryExecutionMode.Quick, q.QueryExecutionMode);
            q = Test("F1:V1 .SELECT:Name", "F1:V1"); Assert.AreEqual("Name", q.Projection);

            q = Test("F1:V1 .SORT:F1", "F1:V1"); Assert.AreEqual("F1 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .REVERSESORT:F1", "F1:V1"); Assert.AreEqual("F1 DESC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .SORT:F2", "F1:V1"); Assert.AreEqual("F1 ASC, F2 ASC", SortToString(q.Sort));
            q = Test("F1:V1 .SORT:F1 .REVERSESORT:F3 .SORT:F2", "F1:V1"); Assert.AreEqual("F1 ASC, F3 DESC, F2 ASC", SortToString(q.Sort));

            TestError("F1:V1 .UNKNOWNKEYWORD");
            TestError("F1:V1 .TOP");
            TestError("F1:V1 .TOP:");
            TestError("F1:V1 .TOP:aaa");
            TestError("F1:V1 .SKIP");
            TestError("F1:V1 .SKIP:");
            TestError("F1:V1 .SKIP:aaa");
            TestError("F1:V1 .COUNTONLY:");
            TestError("F1:V1 .COUNTONLY:aaa");
            TestError("F1:V1 .COUNTONLY:42");
            TestError("F1:V1 .COUNTONLY:ON");
            TestError("F1:V1 .AUTOFILTERS");
            TestError("F1:V1 .AUTOFILTERS:");
            TestError("F1:V1 .AUTOFILTERS:42");
            TestError("F1:V1 .LIFESPAN");
            TestError("F1:V1 .LIFESPAN:");
            TestError("F1:V1 .LIFESPAN:42");
            TestError("F1:V1 .QUICK:");
            TestError("F1:V1 .QUICK:aaa");
            TestError("F1:V1 .QUICK:42");
            TestError("F1:V1 .QUICK:ON");
            TestError("F1:V1 .SORT");
            TestError("F1:V1 .SORT:");
            TestError("F1:V1 .SORT:42");
            TestError("F1:V1 .SELECT");
            TestError("F1:V1 .SELECT:");
            TestError("F1:V1 .SELECT:123");
        }
        [TestMethod]
        public void SnQuery_Parser_AstToString_CqlErrors()
        {
            TestError("");
            TestError("()");
            TestError("+(+(Id:1 Id:2) +Name:<b");
            TestError("Id:(1 2 3");
            TestError("Password:asdf");
            TestError("PasswordHash:asdf");
            TestError("Id::1");
            TestError("Id:[10 to 15]");
            TestError("Id:[10 TO 15");
            TestError("Id:[ TO ]");
            TestError("_Text:\"aaa bbb\"~");
            TestError("Name:aaa~1.5");
            TestError("Name:aaa^x");
            //UNDONE: Nullref exception in this test: Test("Name:()");
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
        private void TestError(string queryText)
        {
            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();
            Exception thrownException = null;
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
        }

        private string SortToString(SortInfo[] sortInfo)
        {
            return string.Join(", ", sortInfo.Select(s => $"{s.FieldName} {(s.Reverse ? "DESC" : "ASC")}").ToArray());
        }

        [TestMethod]
        public void SnQuery_Parser_AggregateSettingsTest()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfoInt() }
            };
            // tuple values:
            // Item1: QuerySettings
            // Item2: query text postfix
            // Item3: expected Top
            // Item4: expected Skip
            // Item5: expected EnableAutofilters
            // Item6: expected EnableLifespanFilter
            // Item7: expected QueryExecutionMode
            var settings = new List<Tuple<QuerySettings, string, int, int, FilterStatus, FilterStatus, QueryExecutionMode>>
            {
                Tuple.Create(new QuerySettings(), " .TOP:0", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .TOP:5", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, "", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:0", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 0}, " .TOP:10", 10, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 5}, " .TOP:10", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:5", 5, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SKIP:0", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SKIP:1", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 0}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 0}, " .SKIP:1", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 1}, " .SKIP:0", int.MaxValue, 1, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 10}, " .SKIP:5", int.MaxValue, 10, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Skip = 5}, " .SKIP:10", int.MaxValue, 5, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .AUTOFILTERS:ON", int.MaxValue, 0, FilterStatus.Enabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Enabled}, "", int.MaxValue, 0, FilterStatus.Enabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Disabled}, " .AUTOFILTERS:ON", int.MaxValue, 0, FilterStatus.Disabled, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .LIFESPAN:ON", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Enabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Enabled}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Enabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {EnableLifespanFilter = FilterStatus.Disabled}, " .LIFESPAN:ON", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Disabled, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings() , " .QUICK", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Quick),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Default}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Quick}, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Quick),
                Tuple.Create(new QuerySettings {QueryExecutionMode = QueryExecutionMode.Strict}, " .QUICK", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Strict),
                Tuple.Create(new QuerySettings {Sort = new List<SortInfo> {new SortInfo ("Id") } }, "", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings (), " .SORT:Id", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings {Sort = new List<SortInfo> {new SortInfo("Id") } }, " .SORT:Name", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default),
                Tuple.Create(new QuerySettings(), " .SORT:Name .TOP:0 .SORT:DisplayName", int.MaxValue, 0, FilterStatus.Default, FilterStatus.Default, QueryExecutionMode.Default)
            };
            var expectedSortInfo = new List<IEnumerable<SortInfo>>();
            for (int i = 0; i < settings.Count - 4; i++)
            {
                expectedSortInfo.Add(null);
            }
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Id")});
            expectedSortInfo.Add(new List<SortInfo> {new SortInfo("Name"), new SortInfo("DisplayName")});

            var parser = new CqlParser();
            var queryText = "+Id:<1000";
            foreach (var setting in settings)
            {
                var queryContext = new TestQueryContext(setting.Item1, 0, indexingInfo);
                var inputQueryText = queryText + setting.Item2;    
                var expectedResultText = queryText;

                var snQuery = parser.Parse(inputQueryText, queryContext);

                var visitor = new SnQueryToStringVisitor();
                visitor.Visit(snQuery.QueryTree);
                var actualResultText = visitor.Output;

                Assert.AreEqual(expectedResultText, actualResultText);
                Assert.AreEqual(setting.Item3, snQuery.Top);
                Assert.AreEqual(setting.Item4, snQuery.Skip);
                Assert.AreEqual(setting.Item5, snQuery.EnableAutofilters);
                Assert.AreEqual(setting.Item6, snQuery.EnableLifespanFilter);
                Assert.AreEqual(setting.Item7, snQuery.QueryExecutionMode);
                var sortIndex =  settings.IndexOf(setting);
                Assert.IsTrue((!snQuery.Sort.Any() && expectedSortInfo[sortIndex] == null) || expectedSortInfo[sortIndex].Count() == snQuery.Sort.Length);
            }
        }

        //UNDONE: Move this test to QueryClassifier tests
        //[TestMethod]
        //public void SnQuery_Parser_UsedFieldNames()
        //{
        //    var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
        //    {
        //        {"Id", new TestPerfieldIndexingInfo_int() },
        //        {"Name", new TestPerfieldIndexingInfo_string() },
        //        {"Field1", new TestPerfieldIndexingInfo_string() },
        //        {"Field2", new TestPerfieldIndexingInfo_string() }
        //    };
        //    var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
        //    var parser = new CqlParser();
        //    var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2) +(Field1:asdf)";
        //    var expected = "Field1, Field2, Id, Name";

        //    var snQuery = parser.Parse(queryText, queryContext);

        //    var actual = string.Join(", ", snQuery.QueryFieldNames.OrderBy(x => x).ToArray());
        //    Assert.AreEqual(expected, actual);
        //}

    }
}
