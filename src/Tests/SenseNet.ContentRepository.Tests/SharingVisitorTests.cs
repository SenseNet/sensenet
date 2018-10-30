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
        public void Sharing_Query_Rewriting_CombineValues1()
        {
            var input1 = new List<List<string>>
            {
                new List<string> { "a" }
            };
            var input2 = new List<List<string>>
            {
                new List<string> { "b" },
                new List<string> { "c" }
            };

            // ACTION
            var sharingVisitorAcc = new PrivateType(typeof(SharingVisitor));
            var result = (List<List<string>>)sharingVisitorAcc.InvokeStatic("CombineValues", input1, input2);

            // ASSERT
            var actual = string.Join(" ", result.Select(x => string.Join("", x)));
            Assert.AreEqual("ab ac", actual);
        }
        [TestMethod]
        public void Sharing_Query_Rewriting_CombineValues2()
        {
            var input1 = new List<List<string>>
            {
                new List<string> { "a" },
                new List<string> { "b" },
            };
            var input2 = new List<List<string>>
            {
                new List<string> { "c" },
                new List<string> { "d" },
                new List<string> { "e" },
            };

            // ACTION
            var sharingVisitorAcc = new PrivateType(typeof(SharingVisitor));
            var result = (List<List<string>>)sharingVisitorAcc.InvokeStatic("CombineValues", input1, input2);

            // ASSERT
            var actual = string.Join(" ", result.Select(x => string.Join("", x)));
            Assert.AreEqual("ac ad ae bc bd be", actual);
        }
        [TestMethod]
        public void Sharing_Query_Rewriting_CombineValues3()
        {
            var input1 = new List<List<string>>
            {
                new List<string> { "a", "b" },
                new List<string> { "c" }
            };
            var input2 = new List<List<string>>
            {
                new List<string> { "d", "e", "f" },
                new List<string> { "g" },
                new List<string> { "e", "h", "i" }
            };

            // ACTION
            var sharingVisitorAcc = new PrivateType(typeof(SharingVisitor));
            var result = (List<List<string>>)sharingVisitorAcc.InvokeStatic("CombineValues", input1, input2);

            // ASSERT
            var actual = string.Join(" ", result.Select(x => string.Join("", x)));
            Assert.AreEqual("abdef abg abehi cdef cg cehi", actual);
        }

        [TestMethod]
        public void Sharing_Query_Rewriting()
        {
            // term names
            var a = "SharedWith";
            var b = "SharedWith";
            var c = "SharedBy";
            var d = "SharingMode";
            var e = "SharingLevel";
            var s = "Sharing";

            // input terms
            var qA1 = "user1@example.com";
            var qA2 = "user2@example.com";
            var qB1 = 142;
            var qB2 = 143;
            var qC1 = 151;
            var qC2 = 152;
            var qC3 = 153;
            var qD1 = SharingMode.Private;
            var qD2 = SharingMode.Authenticated;
            var qE1 = SharingLevel.Open;
            var qE2 = SharingLevel.Edit;
            var qX1 = "TypeIs:File";

            // expected terms
            var tA1 = SharingDataTokenizer.TokenizeSharingToken(qA1);
            var tA2 = SharingDataTokenizer.TokenizeSharingToken(qA2);
            var tB1 = SharingDataTokenizer.TokenizeIdentity(qB1);
            var tB2 = SharingDataTokenizer.TokenizeIdentity(qB2);
            var tC1 = SharingDataTokenizer.TokenizeCreatorId(qC1);
            var tC2 = SharingDataTokenizer.TokenizeCreatorId(qC2);
            var tC3 = SharingDataTokenizer.TokenizeCreatorId(qC3);
            var tD1 = SharingDataTokenizer.TokenizeSharingMode(qD1);
            var tD2 = SharingDataTokenizer.TokenizeSharingMode(qD2);
            var tE1 = SharingDataTokenizer.TokenizeSharingLevel(qE1);
            var tE2 = SharingDataTokenizer.TokenizeSharingLevel(qE2);
            var tX1 = "TypeIs:file";

            // one level "must" only
            RewritingTest($"+{qX1} +{b}:{qB1}", $"+{tX1} +{s}:{tB1}");
            RewritingTest($"+{qX1} +{b}:{qB1} +{e}:{qE1}", $"+{tX1} +{s}:{tB1},{tE1}");
            RewritingTest($"+{qX1} +{e}:{qE1} +{b}:{qB1} +{c}:{qC1}", $"+{tX1} +{s}:{tB1},{tC1},{tE1}");
            RewritingTest($"+{qX1} +{c}:{qC1} +{b}:{qB1} +{e}:{qE1}", $"+{tX1} +{s}:{tB1},{tC1},{tE1}");

            // one level "should" only
            RewritingTest($"{b}:{qB1} {b}:{qB2} {d}:{qD1}", $"{s}:{tB1} {s}:{tB2} {s}:{tD1}");

            // +x:(_ _) +b:(_ _)
            RewritingTest($"+TypeIs:(File Folder) +{b}:({qB1} {qB2})",
                          $"+(TypeIs:file TypeIs:folder) +({s}:{tB1} {s}:{tB2})");

            // ... +d +b:(_ _) --> combine --> ... +(s:b1,d s:b2,d)
            RewritingTest($"+{qX1} +{d}:{qD1} +{b}:({qB1} {qB2})",
                          $"+{tX1} +({s}:{tB1},{tD1} {s}:{tB2},{tD1})");

            // ... +a +b:(_ _) +c:(_ _ _) --> combine --> ... +(s:a,b1,c1 s:a,b1,c2 s:a,b1,c3 s:a,b2,c1 s:a,b2,c2 s:a,b2,c3)
            RewritingTest($"+{qX1} +{a}:{qA1} +{b}:({qB1} {qB2}) +{c}:({qC1} {qC2} {qC3})",
                          $"+{tX1} +({s}:{tA1},{tB1},{tC1} {s}:{tA1},{tB2},{tC1} {s}:{tA1},{tB1},{tC2} {s}:{tA1},{tB2},{tC2} {s}:{tA1},{tB1},{tC3} {s}:{tA1},{tB2},{tC3})");
        }
        private void RewritingTest(string inputQuery, string expectedQuery)
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
