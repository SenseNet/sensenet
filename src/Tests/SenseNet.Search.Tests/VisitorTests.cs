using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class VisitorTests
    {
        private class TestVisitor : SnQueryVisitor
        {
            public override SnQueryPredicate VisitTextPredicate(TextPredicate textPredicate)
            {
                if (textPredicate.Value != "V2")
                    return textPredicate;
                return new TextPredicate(textPredicate.FieldName, "V2222");
            }
        }

        private class UnknownPredicate : SnQueryPredicate
        {
            
        }

        [TestMethod]
        public void Search_Visitor_Rewrite()
        {
            var tree = new LogicalPredicate(
                new []
                {
                    new LogicalClause(new LogicalPredicate(
                        new [] {
                            new LogicalClause(new TextPredicate("F1", "V1"), Occurence.Should),
                            new LogicalClause(new TextPredicate("F2", "V2"), Occurence.Should)
                        }), Occurence.Must),
                    new LogicalClause(new LogicalPredicate(
                        new [] {
                            new LogicalClause(new TextPredicate("F3", "V3"), Occurence.Should),
                            new LogicalClause(new RangePredicate("F4", null, "10", true, true), Occurence.Should), 
                        }), Occurence.Must),
                });

            var visitor = new TestVisitor();
            var rewritten = visitor.Visit(tree);

            var dumper = new SnQueryToStringVisitor();
            dumper.Visit(rewritten);

            Assert.AreEqual("+(F1:V1 F2:V2222) +(F3:V3 F4:<10)", dumper.Output);
        }

        [TestMethod]
        public void Search_Visitor_VisitNull()
        {
            var visitor = new TestVisitor();
            Assert.IsNull(visitor.Visit(null));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Search_Visitor_VisitUnknown()
        {
            var visitor = new TestVisitor();
            visitor.Visit(new UnknownPredicate());
        }
    }
}
