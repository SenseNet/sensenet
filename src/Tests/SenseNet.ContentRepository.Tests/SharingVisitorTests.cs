using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SharingVisitorTests : TestBase
    {
        #region Infrastructure
        private class TestPerfieldIndexingInfoTypeIs : IPerFieldIndexingInfo
        {
            public IndexFieldAnalyzer Analyzer { get; set; }
            public IFieldIndexHandler IndexFieldHandler { get; set; } = new LowerStringIndexHandler();
            public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
            public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.No;
            public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
            public bool IsInIndex { get; } = true;
            public Type FieldDataType { get; set; } = typeof(NodeType);
        }
        private class TestPerfieldIndexingInfoInTree : IPerFieldIndexingInfo
        {
            public IndexFieldAnalyzer Analyzer { get; set; }
            public IFieldIndexHandler IndexFieldHandler { get; set; } = new InTreeIndexHandler();
            public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
            public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.No;
            public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
            public bool IsInIndex { get; } = true;
            public Type FieldDataType { get; set; } = typeof(SharingData);
        }
        private class TestPerfieldIndexingInfoSharing : IPerFieldIndexingInfo
        {
            public IndexFieldAnalyzer Analyzer { get; set; }
            public virtual IFieldIndexHandler IndexFieldHandler { get; set; } = new SharingIndexHandler();
            public IndexingMode IndexingMode { get; set; } = IndexingMode.NotAnalyzed;
            public IndexStoringMode IndexStoringMode { get; set; } = IndexStoringMode.No;
            public IndexTermVector TermVectorStoringMode { get; set; } = IndexTermVector.No;
            public bool IsInIndex { get; } = true;
            public Type FieldDataType { get; set; } = typeof(SharingData);
        }
        private class TestPerfieldIndexingInfoSharedWith : TestPerfieldIndexingInfoSharing
        {
            public override IFieldIndexHandler IndexFieldHandler { get; set; } = new SharedWithIndexHandler();
        }
        private class TestPerfieldIndexingInfoSharedBy : TestPerfieldIndexingInfoSharing
        {
            public override IFieldIndexHandler IndexFieldHandler { get; set; } = new SharedByIndexHandler();
        }
        private class TestPerfieldIndexingInfoSharingMode : TestPerfieldIndexingInfoSharing
        {
            public override IFieldIndexHandler IndexFieldHandler { get; set; } = new SharingModeIndexHandler();
        }
        private class TestPerfieldIndexingInfoSharingLevel : TestPerfieldIndexingInfoSharing
        {
            public override IFieldIndexHandler IndexFieldHandler { get; set; } = new SharingLevelIndexHandler();
        }

        readonly Dictionary<string, IPerFieldIndexingInfo> _indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
        {
            //{"_Text", new TestPerfieldIndexingInfoString()},
            {"Name", new TestPerfieldIndexingInfoString()},
            {"Id", new TestPerfieldIndexingInfoInt()},

            { "TypeIs", new TestPerfieldIndexingInfoTypeIs() },
            { "InTree", new TestPerfieldIndexingInfoInTree() },

            { "Sharing", new TestPerfieldIndexingInfoSharing() },
            { "SharedWith", new TestPerfieldIndexingInfoSharedWith() },
            { "SharedBy", new TestPerfieldIndexingInfoSharedBy() },
            { "SharingMode", new TestPerfieldIndexingInfoSharingMode() },
            { "SharingLevel", new TestPerfieldIndexingInfoSharingLevel() }
        };

        private class TestQueryContext : IQueryContext
        {
            private readonly IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;

            public QuerySettings Settings { get; }
            public int UserId { get; }
            public IQueryEngine QueryEngine { get; }
            public IMetaQueryEngine MetaQueryEngine { get; }

            public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
            {
                return (_indexingInfo.TryGetValue(fieldName, out var result)) ? result : null;
            }

            public TestQueryContext(QuerySettings settings, int userId, IDictionary<string, IPerFieldIndexingInfo> indexingInfo, IQueryEngine queryEngine = null, IMetaQueryEngine metaQueryEngine = null)
            {
                Settings = settings;
                UserId = userId;
                _indexingInfo = indexingInfo;
                QueryEngine = queryEngine;
                MetaQueryEngine = metaQueryEngine;
            }
        }
        private class TestQueryEngine : IQueryEngine
        {
            private readonly IDictionary<string, QueryResult<int>> _intResults;
            private readonly IDictionary<string, QueryResult<string>> _stringResults;

            public TestQueryEngine(IDictionary<string, QueryResult<int>> intResults, IDictionary<string, QueryResult<string>> stringResults)
            {
                _intResults = intResults;
                _stringResults = stringResults;
            }

            public QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
            {
                if (_intResults.TryGetValue(query.Querytext, out var result))
                    return result;
                return QueryResult<int>.Empty;
            }

            public QueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
            {
                return _stringResults[query.Querytext];
            }
        }
        #endregion

        [TestMethod]
        public void Sharing_Query_Rewriting()
        {
            // subqueries
            var qA1 = "SharedWith:user1@example.com";
            var qA2 = "SharedWith:user2@example.com";
            var qB1 = "SharedWith:142";
            var qB2 = "SharedWith:143";
            var qC1 = "SharedBy:151";
            var qC2 = "SharedBy:152";
            var qD1 = "SharingMode:" + SharingMode.Private;
            var qD2 = "SharingMode:" + SharingMode.Authenticated;
            var qE1 = "SharingLevel:" + SharingLevel.Open;
            var qE2 = "SharingLevel:" + SharingLevel.Edit;

            // expected terms
            var tA1 = SharingDataTokenizer.TokenizeSharingToken("user1@example.com");
            var tA2 = SharingDataTokenizer.TokenizeSharingToken("user2@example.com");
            var tB1 = SharingDataTokenizer.TokenizeIdentity(142);
            var tB2 = SharingDataTokenizer.TokenizeIdentity(143);
            var tC1 = SharingDataTokenizer.TokenizeCreatorId(151);
            var tC2 = SharingDataTokenizer.TokenizeCreatorId(152);
            var tD1 = SharingDataTokenizer.TokenizeSharingMode(SharingMode.Private);
            var tD2 = SharingDataTokenizer.TokenizeSharingMode(SharingMode.Authenticated);
            var tE1 = SharingDataTokenizer.TokenizeSharingLevel(SharingLevel.Open);
            var tE2 = SharingDataTokenizer.TokenizeSharingLevel(SharingLevel.Edit);

            QueryRewritingTest($"+TypeIs:File +{qB1}", $"+TypeIs:file +Sharing:{tB1}");
            QueryRewritingTest($"+TypeIs:File +{qB1} +{qE1}", $"+TypeIs:file +Sharing:{tB1},{tE1}");
            QueryRewritingTest($"+TypeIs:File +{qC1} +{qB1} +{qE1}", $"+TypeIs:file +Sharing:{tB1},{tC1},{tE1}");
        }
        private void QueryRewritingTest(string inputQuery, string expectedQuery)
        {
            //var intResults = new Dictionary<string, QueryResult<int>> { { "asdf", new QueryResult<int>(new[] { 1, 2, 3 }, 4) } };
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(null, null));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory", new EverythingAllowedPermissionFilterFactory()))
            {
                var queryIn = SnQuery.Parse(inputQuery, context);
                var snQueryAcc = new PrivateType(typeof(SnQuery));
                snQueryAcc.InvokeStatic("PrepareQuery", queryIn, context);
                var queryOut = snQueryAcc.InvokeStatic("ApplyVisitors", queryIn);

                var expected = expectedQuery.EndsWith(".AUTOFILTERS:OFF") ? expectedQuery : $"{expectedQuery} .AUTOFILTERS:OFF";
                Assert.AreEqual(expected, queryOut.ToString());
            }
        }
    }
}
