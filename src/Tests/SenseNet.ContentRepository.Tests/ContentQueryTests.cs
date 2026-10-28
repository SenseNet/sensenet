using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentQueryTests : TestBase
    {
        [DataTestMethod]
        [DataRow("Name:(LeftOnly Both)", "+Index:20 +Name:(RightOnly Both)", "LeftOnly,RightOnly,Both")]
        [DataRow("Name:(LeftOnly Both)", "+Name:RightOnly", "LeftOnly,RightOnly,Both")]
        [DataRow("+Name:LeftOnly", "Name:(RightOnly Both)", "LeftOnly,RightOnly,Both")]
        [DataRow("+Name:LeftOnly", "+Name:RightOnly", "LeftOnly,RightOnly")]
        [DataRow("Name:LeftOnly", "Name:RightOnly Name:Both", "LeftOnly,RightOnly,Both")]
        [DataRow("Name:LeftOnly", "+Index:20 -Name:Both", "LeftOnly,RightOnly")]
        [DataRow("Name:LeftOnly", "+Index:0 +Name:Neither", "LeftOnly")]
        [DataRow("Name:LeftOnly", "((+Name:RightOnly))", "LeftOnly,RightOnly")]
        [DataRow("", "+Name:RightOnly", "RightOnly")]
        public void ContentQuery_AddOrClause_PreservesOperandMatches(string original, string addition, string expectedNames)
        {
            Test(() =>
            {
                var nodes = CreateQueryTestContent();
                var expected = nodes.Where(n => expectedNames.Split(',').Contains(n.Name)).Select(n => n.Id).ToArray();

                foreach (var useInstance in new[] { false, true })
                {
                    var query = CombineOrQuery(original, addition, useInstance);
                    var result = query.Execute();
                    CollectionAssert.AreEquivalent(expected, result.Identifiers.ToArray(),
                        $"Instance API: {useInstance}, query: {query.Text}");
                }
            });
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ContentQuery_AddOrClause_PreservesPagingAndSorting(bool useInstance)
        {
            Test(() =>
            {
                var nodes = CreateQueryTestContent();
                var query = CombineOrQuery("Name:(LeftOnly Both) .TOP:2 .SORT:Name",
                    "+Name:RightOnly .SKIP:1 .AUTOFILTERS:OFF", useInstance);

                var result = query.Execute();

                Assert.AreEqual(3, result.Count);
                CollectionAssert.AreEqual(new[] { nodes[0].Id, nodes[1].Id }, result.Identifiers.ToArray());
            });
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ContentQuery_AddClause_ChainedOrThenAnd(bool useInstance)
        {
            Test(() =>
            {
                var nodes = CreateQueryTestContent();
                var query = CombineOrQuery("+Name:LeftOnly", "+Name:RightOnly", useInstance);
                query = CombineOrQuery(query.Text, "+Name:Both", useInstance);
                if (useInstance)
                    query.AddClause("Index:20", LogicalOperator.And);
                else
                    query.Text = ContentQuery.AddClause(query.Text, "Index:20", LogicalOperator.And);
                query.IsSafe = true;

                var result = query.Execute();

                CollectionAssert.AreEquivalent(new[] { nodes[1].Id, nodes[2].Id }, result.Identifiers.ToArray());
            });
        }

        private static ContentQuery CombineOrQuery(string original, string addition, bool useInstance)
        {
            var query = ContentQuery.CreateQuery(original, QuerySettings.AdminSettings);
            if (useInstance)
                query.AddClause(addition, LogicalOperator.Or);
            else
                query.Text = ContentQuery.AddClause(original, addition, LogicalOperator.Or);
            query.IsSafe = true;
            return query;
        }

        private static SystemFolder[] CreateQueryTestContent()
        {
            var root = new SystemFolder(Repository.Root) { Name = "QueryCompositionTests" };
            root.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            var nodes = new[]
            {
                new SystemFolder(root) { Name = "LeftOnly", Index = 10 },
                new SystemFolder(root) { Name = "RightOnly", Index = 20 },
                new SystemFolder(root) { Name = "Both", Index = 20 },
                new SystemFolder(root) { Name = "Neither", Index = 30 }
            };
            foreach (var node in nodes)
                node.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return nodes;
        }

        [TestMethod]
        public void ContentQuery_AddOrClause()
        {
            void OrTest(string @base, string add, string expected)
            {
                var result = ContentQuery.AddClause(
                        @base, add, LogicalOperator.Or)
                    .Replace(" ", "");
                Assert.AreEqual(expected.Replace(" ", ""), result);

                var query = ContentQuery.CreateQuery(@base);
                query.AddClause(add, LogicalOperator.Or);
                Assert.AreEqual(expected.Replace(" ", ""), query.Text.Replace(" ", ""));
            }
            Test(() =>
            {
                OrTest("Id:1 Id:2 .AUTOFILTERS:OFF", "Id:3", "(Id:1 Id:2) (Id:3) .AUTOFILTERS:OFF");
                OrTest("Id:1 Id:2", "Id:3 .AUTOFILTERS:OFF", "(Id:1 Id:2) (Id:3) .AUTOFILTERS:OFF");
                OrTest("Id:1 Id:2 .TOP:3 .SKIP:5", "Id:3", "(Id:1 Id:2) (Id:3) .TOP:3 .SKIP:5");
                OrTest("Id:1 Id:2 .TOP:3", "Id:3 .QUICK", "(Id:1 Id:2) (Id:3) .TOP:3 .QUICK");
                OrTest("Id:1 .COUNTONLY", "Id:2 .ALLVERSIONS", "(Id:1) (Id:2) .COUNTONLY .ALLVERSIONS");
                OrTest("Id:1 .SELECT:Name", "Id:2", "(Id:1) (Id:2) .SELECT:Name");
                OrTest("Id:1 .SORT:Name", "Id:2", "(Id:1) (Id:2) .SORT:Name");
                OrTest("Id:1 .REVERSESORT:Name", "Id:2", "(Id:1) (Id:2) .REVERSESORT:Name");
                OrTest("Id:1 .LIFESPAN:OFF", "Id:2", "(Id:1) (Id:2) .LIFESPAN:OFF");
            });
        }
        [TestMethod]
        public void ContentQuery_AddAndClause()
        {
            void AndTest(string @base, string add, string expected)
            {
                var result = ContentQuery.AddClause(
                        @base, add, LogicalOperator.And)
                    .Replace(" ", "");
                Assert.AreEqual(expected.Replace(" ", ""), result);
            }
            Test(() =>
            {
                AndTest("+Id:1 +Id:2 .AUTOFILTERS:OFF", "+Id:3", "+(+Id:1 +Id:2) +(+Id:3) .AUTOFILTERS:OFF");
                AndTest("+Id:1 +Id:2", "+Id:3 .AUTOFILTERS:OFF", "+(+Id:1 +Id:2) +(+Id:3) .AUTOFILTERS:OFF");
                AndTest("+Id:1 +Id:2 .TOP:3 .SKIP:5", "+Id:3", "+(+Id:1 +Id:2) +(+Id:3) .TOP:3 .SKIP:5");
                AndTest("+Id:1 +Id:2 .TOP:3", "+Id:3 .QUICK", "+(+Id:1 +Id:2) +(+Id:3) .TOP:3 .QUICK");
                AndTest("+Id:1 .COUNTONLY", "+Id:2 .ALLVERSIONS", "+(+Id:1) +(+Id:2) .COUNTONLY .ALLVERSIONS");
                AndTest("+Id:1 .SELECT:Name", "+Id:2", "+(+Id:1) +(+Id:2) .SELECT:Name");
                AndTest("+Id:1 .SORT:Name", "+Id:2", "+(+Id:1) +(+Id:2) .SORT:Name");
                AndTest("+Id:1 .REVERSESORT:Name", "+Id:2", "+(+Id:1) +(+Id:2) .REVERSESORT:Name");
                AndTest("+Id:1 .LIFESPAN:OFF", "+Id:2", "+(+Id:1) +(+Id:2) .LIFESPAN:OFF");
            });
        }
    }
}
