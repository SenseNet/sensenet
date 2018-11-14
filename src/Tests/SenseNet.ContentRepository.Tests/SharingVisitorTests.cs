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
using SenseNet.Search.Querying.Parser.Predicates;
// ReSharper disable UnusedVariable

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
            {"Description", new TestPerfieldIndexingInfoString()},

            { "TypeIs", new TestPerfieldIndexingInfoTypeIs() },
            { "InTree", new TestPerfieldIndexingInfoInTree() },

            { "Sharing", new TestPerfieldIndexingInfoSharing() },
            { "SharedWith", new TestPerfieldIndexingInfoSharedWith() },
            { "SharedBy", new TestPerfieldIndexingInfoSharedBy() },
            { "SharingMode", new TestPerfieldIndexingInfoSharingMode() },
            { "SharingLevel", new TestPerfieldIndexingInfoSharingLevel() },

            {"a", new TestPerfieldIndexingInfoString()},
            {"b", new TestPerfieldIndexingInfoString()},
            {"c", new TestPerfieldIndexingInfoString()},
            {"d", new TestPerfieldIndexingInfoString()},
            {"e", new TestPerfieldIndexingInfoString()},
            {"X", new TestPerfieldIndexingInfoString()},
            {"Y", new TestPerfieldIndexingInfoString()},
            {"Z", new TestPerfieldIndexingInfoString()},
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
        public void Sharing_Query_Rewriting1()
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
            var qB1 = 142;
            var qB2 = 143;
            var qC1 = 151;
            var qC2 = 152;
            var qC3 = 153;
            var qD1 = SharingMode.Private;
            var qE1 = SharingLevel.Open;
            var qX1 = "TypeIs:File";

            // expected terms
            var tA1 = SharingDataTokenizer.TokenizeSharingToken(qA1);
            var tB1 = SharingDataTokenizer.TokenizeIdentity(qB1);
            var tB2 = SharingDataTokenizer.TokenizeIdentity(qB2);
            var tC1 = SharingDataTokenizer.TokenizeCreatorId(qC1);
            var tC2 = SharingDataTokenizer.TokenizeCreatorId(qC2);
            var tC3 = SharingDataTokenizer.TokenizeCreatorId(qC3);
            var tD1 = SharingDataTokenizer.TokenizeSharingMode(qD1);
            var tE1 = SharingDataTokenizer.TokenizeSharingLevel(qE1);
            var tX1 = "TypeIs:file";

            // one level "must" only
            RewritingTest($"+{qX1} +{b}:{qB1}", true, $"+{tX1} +{s}:{tB1}");
            RewritingTest($"+{qX1} +{b}:{qB1} +{e}:{qE1}", true, $"+{tX1} +{s}:{tB1},{tE1}");
            RewritingTest($"+{qX1} +{e}:{qE1} +{b}:{qB1} +{c}:{qC1}", true, $"+{tX1} +{s}:{tB1},{tC1},{tE1}");
            RewritingTest($"+{qX1} +{c}:{qC1} +{b}:{qB1} +{e}:{qE1}", true, $"+{tX1} +{s}:{tB1},{tC1},{tE1}");

            // one level "should" only
            RewritingTest($"{b}:{qB1} {b}:{qB2} {d}:{qD1}", true, $"{s}:{tB1} {s}:{tB2} {s}:{tD1}");

            // +x:(_ _) +b:(_ _)
            RewritingTest($"+TypeIs:(File Folder) +{b}:({qB1} {qB2})", true,
                          $"+(TypeIs:file TypeIs:folder) +({s}:{tB1} {s}:{tB2})");

            // ... +d +b:(_ _) --> combine --> ... +(s:b1,d s:b2,d)
            RewritingTest($"+{qX1} +{d}:{qD1} +{b}:({qB1} {qB2})", true,
                          $"+{tX1} +({s}:{tB1},{tD1} {s}:{tB2},{tD1})");

            // ... +a +b:(_ _) +c:(_ _ _) --> combine --> ... +(s:a,b1,c1 s:a,b1,c2 s:a,b1,c3 s:a,b2,c1 s:a,b2,c2 s:a,b2,c3)
            RewritingTest($"+{qX1} +{a}:{qA1} +{b}:({qB1} {qB2}) +{c}:({qC1} {qC2} {qC3})", true,
                          $"+{tX1} +({s}:{tA1},{tB1},{tC1} {s}:{tA1},{tB2},{tC1} {s}:{tA1},{tB1},{tC2} {s}:{tA1},{tB2},{tC2} {s}:{tA1},{tB1},{tC3} {s}:{tA1},{tB2},{tC3})");
        }
        [TestMethod]
        public void Sharing_Query_Rewriting2()
        {
            var s = new[] { "Sharing", "SharedWith", "SharedBy", "SharingMode", "SharingLevel" };

            RewritingTest("-a:a", true, "-a:a");
            RewritingTest($"+{s[0]}:s0", true, $"+{s[0]}:s0");
            RewritingTest($"-a:a +{s[0]}:s0", true, $"-a:a +{s[0]}:s0");
            RewritingTest($"+{s[0]}:Is0 +({s[1]}:s1 {s[1]}:s2)", true, $"+({s[0]}:Ts1,Is0 {s[0]}:Ts2,Is0)");

            RewritingTest("+a:a0 +(b:b1 b:b2)", true, "+a:a0 +(b:b1 b:b2)");
            RewritingTest("+a:a0 +(b:b1 b:b2) -d:d0", true, "+a:a0 +(b:b1 b:b2) -d:d0");
            RewritingTest("+a:a0 +(b:b1 b:b2 -d:d0)", true, "+a:a0 +(b:b1 b:b2 -d:d0)");

            // ranges
            RewritingTest($"Id:<10 +{s[0]}:s0", true, $"Id:<10 +{s[0]}:s0");
            RewritingTest("Id:<10 +SharedWith:<123", false, null);

            // negation
            RewritingTest($"-a:a +b:b +c:c +{s[0]}:s0 +d:d", true, $"-a:a +b:b +c:c +d:d +{s[0]}:s0");
            RewritingTest($"+a:a -b:b +c:c +{s[0]}:s0 +d:d", true, $"+a:a -b:b +c:c +d:d +{s[0]}:s0");
            RewritingTest($"+a:a +b:b -c:c +{s[0]}:s0 +d:d", true, $"+a:a +b:b -c:c +d:d +{s[0]}:s0");
            RewritingTest($"+a:a +b:b +c:c -{s[0]}:s0 +d:d", false, null);
            RewritingTest($"+a:a +b:b +c:c +{s[0]}:s0 -d:d", true, $"+a:a +b:b +c:c -d:d +{s[0]}:s0");

            // mixed level
            RewritingTest($"+a:a0 +(b:b1 b:b2) +({s[0]}:s0 {s[0]}:s1)", true, $"+a:a0 +(b:b1 b:b2) +({s[0]}:s0 {s[0]}:s1)");
            RewritingTest($"+a:a0 +(b:b1 b:b2) +({s[0]}:s0 (+{s[1]}:1 +{s[2]}:42))", true, $"+a:a0 +(b:b1 b:b2) +({s[0]}:s0 {s[0]}:I1,C42)");
            RewritingTest($"+a:a0 +(b:b1 b:b2) +({s[0]}:s0 (+{s[1]}:s1 +{s[1]}:s2 +X:x))", false, null);
            RewritingTest($"+a:a0 +(b:b1 b:b2) +({s[0]}:s0 (+{s[1]}:1 +{s[2]}:42) c:c1)", true, $"+a:a0 +(b:b1 b:b2) +(c:c1 {s[0]}:s0 {s[0]}:I1,C42)");
        }
        private void RewritingTest(string inputQuery, bool isValid, string expected)
        {
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(null, null));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory", new EverythingAllowedPermissionFilterFactory()))
            {
                var queryIn = SnQuery.Parse(inputQuery, context);
                var snQueryAcc = new PrivateType(typeof(SnQuery));
                snQueryAcc.InvokeStatic("PrepareQuery", queryIn, context);

                var hasError = false;
                var visitor = new SharingVisitor();
                SnQuery queryOut = null;
                try
                {
                    queryOut = SnQuery.Create(visitor.Visit(queryIn.QueryTree));
                }
                catch (InvalidContentSharingQueryException)
                {
                    hasError = true;
                }

                Assert.AreNotEqual(isValid, hasError);
                if (!hasError)
                    Assert.AreEqual(expected, queryOut.ToString());
            }
        }

        [TestMethod]
        public void Sharing_Query_Rewriting_FieldNames()
        {
            var inputQuery = "-a:a Sharing:s0 SharedWith:123 SharedBy:s2 SharingMode:s3 SharingLevel:s4";
            var expectedQuery = "-a:a Sharing:s0 Sharing:I123 Sharing:s2 Sharing:s3 Sharing:s4";
            string actualQuery;

            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(null, null));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory", new EverythingAllowedPermissionFilterFactory()))
            {
                var queryIn = SnQuery.Parse(inputQuery, context);
                var snQueryAcc = new PrivateType(typeof(SnQuery));
                snQueryAcc.InvokeStatic("PrepareQuery", queryIn, context);

                var visitor = new SharingVisitor();
                var newTree = visitor.Visit(queryIn.QueryTree);

                var snQuery = SnQuery.Create(newTree);
                actualQuery = snQuery.ToString();
            }

            Assert.AreEqual(expectedQuery, actualQuery);
        }


        [TestMethod]
        public void Sharing_Query_Rewriting_Normalize()
        {
            const string s0 = "Sharing:s0";
            const string s1 = "Sharing:s1";
            const string s2 = "Sharing:s2";
            const string s3 = "Sharing:s3";
            const string s4 = "Sharing:s4";

            LogicalPredicate GetPredicate(Occurence outer, Occurence inner)
            {
                return new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("Sharing", new IndexValue("s0")), outer),
                    new LogicalClause(new LogicalPredicate(new[]
                    {
                        new LogicalClause(new SimplePredicate("Sharing", new IndexValue("s1")), inner),
                    }), outer),
                });
            }

            // inhomogeneous level --> irrelevant terms
            NormalizerVisitorTest($"+{s0} +({s1} +{s2})", $"+{s0} +{s2}");
            NormalizerVisitorTest($"+{s0} (+{s1} {s2})", $"+{s0}");
            NormalizerVisitorTest($"+X:x +{s0} +({s1} +{s2})", $"+X:x +{s0} +{s2}");
            NormalizerVisitorTest($"+X:x +{s0} (+{s1} {s2})", $"+X:x +{s0}");

            // parentheses with one clause
            var twoShould = $"{s0} {s1}";
            var twoMust = $"+{s0} +{s1}";
            NormalizerVisitorTest(GetPredicate(Occurence.Default, Occurence.Default), twoShould);
            NormalizerVisitorTest(GetPredicate(Occurence.Default, Occurence.Should), twoShould);
            NormalizerVisitorTest(GetPredicate(Occurence.Default, Occurence.Must), twoShould);
            NormalizerVisitorTest(GetPredicate(Occurence.Should, Occurence.Default), twoShould);
            NormalizerVisitorTest(GetPredicate(Occurence.Should, Occurence.Should), twoShould);
            NormalizerVisitorTest(GetPredicate(Occurence.Should, Occurence.Must), twoShould);
            NormalizerVisitorTest(GetPredicate(Occurence.Must, Occurence.Default), twoMust);
            NormalizerVisitorTest(GetPredicate(Occurence.Must, Occurence.Should), twoMust);
            NormalizerVisitorTest(GetPredicate(Occurence.Must, Occurence.Must), twoMust);

            // unnecessary parentheses
            NormalizerVisitorTest($"{s0} ({s1} {s2})", $"{s0} {s1} {s2}");
            NormalizerVisitorTest($"+{s0} +(+{s1} +{s2})", $"+{s0} +{s1} +{s2}");
            NormalizerVisitorTest($"+{s0} +(+{s1} +({s2} ({s3} {s4})))", $"+{s0} +{s1} +({s2} {s3} {s4})");
        }
        private void NormalizerVisitorTest(string inputQuery, string expectedQuery)
        {
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(null, null));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory",
                new EverythingAllowedPermissionFilterFactory()))
            {
                var queryIn = SnQuery.Parse(inputQuery, context);
                NormalizerVisitorTest(queryIn, context, expectedQuery);
            }
        }
        private void NormalizerVisitorTest(SnQueryPredicate inputPredicate, string expectedQuery)
        {
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, _indexingInfo, new TestQueryEngine(null, null));
            using (SenseNet.Tests.Tools.Swindle(typeof(SnQuery), "_permissionFilterFactory", new EverythingAllowedPermissionFilterFactory()))
            {
                var queryIn = SnQuery.Create(inputPredicate);
                NormalizerVisitorTest(queryIn, context, expectedQuery);
            }
        }
        private void NormalizerVisitorTest(SnQuery query, TestQueryContext context, string expectedQuery)
        {
            query.EnableAutofilters = FilterStatus.Disabled;
            query.EnableLifespanFilter = FilterStatus.Disabled;

            var snQueryAcc = new PrivateType(typeof(SnQuery));
            snQueryAcc.InvokeStatic("PrepareQuery", query, context);

            var normalizer = new SharingNormalizerVisitor();
            var normalized = normalizer.Visit(query.QueryTree);

            var newQuery = SnQuery.Create(normalized);

            Assert.AreEqual(expectedQuery, newQuery.ToString());
        }


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
            var visitorAcc = new PrivateType(typeof(SharingComposerVisitor));
            var result = (List<List<string>>)visitorAcc.InvokeStatic("CombineValues", input1, input2);

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
            var visitorAcc = new PrivateType(typeof(SharingComposerVisitor));
            var result = (List<List<string>>)visitorAcc.InvokeStatic("CombineValues", input1, input2);

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
            var visitorAcc = new PrivateType(typeof(SharingComposerVisitor));
            var result = (List<List<string>>)visitorAcc.InvokeStatic("CombineValues", input1, input2);

            // ASSERT
            var actual = string.Join(" ", result.Select(x => string.Join("", x)));
            Assert.AreEqual("abdef abg abehi cdef cg cehi", actual);
        }

    }
}
