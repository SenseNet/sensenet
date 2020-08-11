using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentQueryTests : TestBase
    {
        [TestMethod]
        public void ContentQuery_AddOrClause()
        {
            void OrTest(string @base, string add, string expected)
            {
                var result = ContentQuery.AddClause(
                        @base, add, LogicalOperator.Or)
                    .Replace(" ", "");
                Assert.AreEqual(expected.Replace(" ", ""), result);
            }
            Test(() =>
            {
                OrTest("Id:1 Id:2 .AUTOFILTERS:OFF", "Id:3", "(Id:1 Id:2) Id:3 .AUTOFILTERS:OFF");
                OrTest("Id:1 Id:2", "Id:3 .AUTOFILTERS:OFF", "(Id:1 Id:2) Id:3 .AUTOFILTERS:OFF");
                OrTest("Id:1 Id:2 .TOP:3 .SKIP:5", "Id:3", "(Id:1 Id:2) Id:3 .TOP:3 .SKIP:5");
                OrTest("Id:1 Id:2 .TOP:3", "Id:3 .QUICK", "(Id:1 Id:2) Id:3 .QUICK .TOP:3");
                OrTest("Id:1 .COUNTONLY", "Id:2 .ALLVERSIONS", "(Id:1) Id:2 .ALLVERSIONS .COUNTONLY");
                OrTest("Id:1 .SELECT:Name", "Id:2", "(Id:1) Id:2 .SELECT:Name");
                OrTest("Id:1 .SORT:Name", "Id:2", "(Id:1) Id:2 .SORT:Name");
                OrTest("Id:1 .REVERSESORT:Name", "Id:2", "(Id:1) Id:2 .REVERSESORT:Name");
                OrTest("Id:1 .LIFESPAN:OFF", "Id:2", "(Id:1) Id:2 .LIFESPAN:OFF");
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
