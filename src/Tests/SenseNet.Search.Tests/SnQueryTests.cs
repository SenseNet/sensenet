using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser;
using SenseNet.Search.Tests.Implementations;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class SnQueryTests : TestBase
    {
        readonly Dictionary<string, IPerFieldIndexingInfo> _indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                //{"_Text", new TestPerfieldIndexingInfoString()},
                {"#Field1", new TestPerfieldIndexingInfoString()},
                {"Field1", new TestPerfieldIndexingInfoString()},
                {"Field2", new TestPerfieldIndexingInfoString()},
                {"Field3", new TestPerfieldIndexingInfoString()},
                {"F1", new TestPerfieldIndexingInfoString()},
                {"F2", new TestPerfieldIndexingInfoString()},
                {"F3", new TestPerfieldIndexingInfoString()},
                {"F4", new TestPerfieldIndexingInfoString()},
                {"f1", new TestPerfieldIndexingInfoString()},
                {"f2", new TestPerfieldIndexingInfoString()},
                {"f3", new TestPerfieldIndexingInfoString()},
                {"f4", new TestPerfieldIndexingInfoString()},
                {"f5", new TestPerfieldIndexingInfoString()},
                {"f6", new TestPerfieldIndexingInfoString()},
                {"mod_date", new TestPerfieldIndexingInfoInt()},
                {"title", new TestPerfieldIndexingInfoString()},
                {"Name", new TestPerfieldIndexingInfoString()},
                {"Id", new TestPerfieldIndexingInfoInt()},
                {"LongField1", new TestPerfieldIndexingInfoLong()},
                {"SingleField1", new TestPerfieldIndexingInfoSingle()},
                {"DoubleField1", new TestPerfieldIndexingInfoDouble()},
                {"IsSystemContent", new TestPerfieldIndexingInfoBool()},
                {"EnableLifespan", new TestPerfieldIndexingInfoBool()},
                {"ValidFrom", new TestPerfieldIndexingInfoDateTime()},
                {"ValidTill", new TestPerfieldIndexingInfoDateTime()},
            };

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Result_Int()
        {
            var intResults = new Dictionary<string, QueryResult<int>> { { "asdf", new QueryResult<int>(new[] { 1, 2, 3 }, 4) } };
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(intResults, null));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory", new EverythingAllowedPermissionFilterFactory()))
            {
                var queryText = "asdf";

                var result = SnQuery.Query(queryText, context);

                var expected = string.Join(", ", intResults[queryText].Hits.Select(x => x.ToString()).ToArray());
                var actual = string.Join(", ", result.Hits.Select(x => x.ToString()).ToArray());
                Assert.AreEqual(actual, expected);
                Assert.AreEqual(result.TotalCount, intResults[queryText].TotalCount);
            }
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Result_String()
        {
            var stringResults = new Dictionary<string, QueryResult<string>>
            {
                {"asdf", new QueryResult<string>(new[] {"1", "2", "3"}, 4)}
            };

            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(null, stringResults));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory", new EverythingAllowedPermissionFilterFactory()))
            {
                var queryText = "asdf";

                var result = SnQuery.QueryAndProject(queryText, context);

                var expected = string.Join(", ", stringResults[queryText].Hits);
                var actual = string.Join(", ", result.Hits);
                Assert.AreEqual(actual, expected);
                Assert.AreEqual(result.TotalCount, stringResults[queryText].TotalCount);
            }
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_DisabledDisabled()
        {
            var queryText = "Name:MyDocument.doc";
            var expected = "Name:mydocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Disabled, FilterStatus.Disabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);
            Assert.AreEqual(expected, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_DisabledEnabled()
        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Disabled, FilterStatus.Enabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            // get times from the query
            var regex = new Regex("\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}.\\d{4}");
            var matches = regex.Matches(queryTextAfter);

            // check that the query contains 3 time and the first 2 are equal. 
            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual(matches[0].Value, matches[1].Value);

            // check query time is close to now
            var queryTimeString = matches[0].Value;
            var queryTime = DateTime.Parse(queryTimeString);
            Assert.IsTrue(DateTime.Now - queryTime < TimeSpan.FromSeconds(10));

            // check global format of the query
            var expectedQueryText = $"+Name:mydocument.doc +(EnableLifespan:no (+ValidFrom:<'{queryTimeString}' +(ValidTill:>'{queryTimeString}' ValidTill:'0001-01-01 00:00:00.0000')))";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_EnabledDisabled()
        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Enabled, FilterStatus.Disabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            var expectedQueryText = "+Name:mydocument.doc +IsSystemContent:no";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_EnabledEnabled()
        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Enabled, FilterStatus.Enabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            // get times from the query
            var regex = new Regex("\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}.\\d{4}");
            var matches = regex.Matches(queryTextAfter);

            // check that the query contains 3 time and the first 2 are equal. 
            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual(matches[0].Value, matches[1].Value);

            // check query time is close to now
            var queryTimeString = matches[0].Value;
            var queryTime = DateTime.Parse(queryTimeString);
            Assert.IsTrue(DateTime.Now-queryTime < TimeSpan.FromSeconds(10));

            // check global format of the query
            var expectedQueryText = $"+Name:mydocument.doc +IsSystemContent:no +(EnableLifespan:no (+ValidFrom:<'{queryTimeString}' +(ValidTill:>'{queryTimeString}' ValidTill:'0001-01-01 00:00:00.0000')))";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_DefaultDefault()

        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Enabled, FilterStatus.Enabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            // get times from the query
            var regex = new Regex("\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}.\\d{4}");
            var matches = regex.Matches(queryTextAfter);

            // check that the query contains 3 time and the first 2 are equal. 
            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual(matches[0].Value, matches[1].Value);

            // check query time is close to now
            var queryTimeString = matches[0].Value;
            var queryTime = DateTime.Parse(queryTimeString);
            Assert.IsTrue(DateTime.Now - queryTime < TimeSpan.FromSeconds(10));

            // check global format of the query
            var expectedQueryText = $"+Name:mydocument.doc +IsSystemContent:no +(EnableLifespan:no (+ValidFrom:<'{queryTimeString}' +(ValidTill:>'{queryTimeString}' ValidTill:'0001-01-01 00:00:00.0000')))";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Classify_UsedFieldNames()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfoInt() },
                {"Name", new TestPerfieldIndexingInfoString() },
                {"Field1", new TestPerfieldIndexingInfoString() },
                {"Field2", new TestPerfieldIndexingInfoString() }
            };
            var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
            var parser = new CqlParser();
            var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2) +(Field1:asdf)";
            var expected = "Field1, Field2, Id, Name";

            var snQuery = parser.Parse(queryText, queryContext);
            var info = SnQueryClassifier.Classify(snQuery);

            var actual = string.Join(", ", info.QueryFieldNames.OrderBy(x => x).ToArray());
            Assert.AreEqual(expected, actual);
        }

        private Tuple<SnQuery, string> CreateQueryAndPrepare(string queryText, FilterStatus autoFilters, FilterStatus lifespanFilter)
        {
            var parser = new CqlParser();
            var context = new TestQueryContext(QuerySettings.AdminSettings, 1, _indexingInfo);
            var query = new SnQuery
            {
                Querytext = queryText,
                QueryTree = parser.Parse(queryText, context).QueryTree,
                EnableAutofilters = autoFilters,
                EnableLifespanFilter = lifespanFilter
            };
            SnQuery.PrepareQuery(query, context);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(query.QueryTree);

            return new Tuple<SnQuery, string>(query, visitor.Output);
        }
    }
}
