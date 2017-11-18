using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser;
using SenseNet.Search.Querying.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class VisitorTests
    {
        private class TestVisitor : SnQueryVisitor
        {
            public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
            {
                if (simplePredicate.Value.ValueAsString != "V2")
                    return simplePredicate;
                return new SimplePredicate(simplePredicate.FieldName, new IndexValue("V2222"));
            }
        }

        private class UnknownPredicate : SnQueryPredicate
        {
            
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Visitor_Rewrite()
        {
            var tree = new LogicalPredicate(
                new []
                {
                    new LogicalClause(new LogicalPredicate(
                        new [] {
                            new LogicalClause(new SimplePredicate("F1", new IndexValue("V1")), Occurence.Should),
                            new LogicalClause(new SimplePredicate("F2", new IndexValue("V2")), Occurence.Should)
                        }), Occurence.Must),
                    new LogicalClause(new LogicalPredicate(
                        new [] {
                            new LogicalClause(new SimplePredicate("F3", new IndexValue("V3")), Occurence.Should),
                            new LogicalClause(new RangePredicate("F4", null, new IndexValue(10), true, true), Occurence.Should), 
                        }), Occurence.Must),
                });

            var visitor = new TestVisitor();
            var rewritten = visitor.Visit(tree);

            var dumper = new SnQueryToStringVisitor();
            dumper.Visit(rewritten);

            Assert.AreEqual("+(F1:V1 F2:V2222) +(F3:V3 F4:<10)", dumper.Output);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Visitor_VisitNull()
        {
            var visitor = new TestVisitor();
            Assert.IsNull(visitor.Visit(null));
        }

        [TestMethod, TestCategory("IR")]
        [ExpectedException(typeof(NotSupportedException))]
        public void SnQuery_Visitor_VisitUnknown()
        {
            var visitor = new TestVisitor();
            visitor.Visit(new UnknownPredicate());
        }
    }
}
