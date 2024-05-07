using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;
using SenseNet.ContentRepository.Linq;
using SenseNet.Search.Querying.Parser.Predicates;
using SenseNet.Tests.Core;

namespace SenseNet.Search.Tests;

[TestClass]
public class BooleanOptimizerTests : TestBase
{
    [TestMethod]
    public void BooleanOptimizer_00_Should_ShouldShould_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Should),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Should),
            }), Occurence.Should)
        });
        Assert.AreEqual("((F:1 F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(F:1 F:2)", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_01_Should_MustMust_NotChanged()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Must),
            }), Occurence.Should)
        });
        Assert.AreEqual("((+F:1 +F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("((+F:1 +F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_02_Should_MustNot_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.Should)
        });
        Assert.AreEqual("((+F:1 -F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("((+F:1 -F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_03_Should_NotNot_NotChanged()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.Should)
        });
        Assert.AreEqual("((-F:1 -F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("((-F:1 -F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_04_Not_NotChanged()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
            }), Occurence.Should),
        });
        Assert.AreEqual("((-F:1))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("((-F:1))", optimized.ToString(), "optimized");
    }

    [TestMethod]
    public void BooleanOptimizer_10_Must_ShouldShould_()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Should),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Should),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(F:1 F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("(+(F:1 F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_11_Must_MustMust_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Must),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(+F:1 +F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(+F:1 +F:2)", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_12_Must_MustNot_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(+F:1 -F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(+F:1 -F:2)", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_13_Must_NotNot_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(-F:1 -F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(-F:1 -F:2)", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_14_Must_Not_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(-F:1))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(-F:1)", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_15_Must_Not_Must_Not_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
            }), Occurence.Must),
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(-F:1) +(-F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(-F:1 -F:2)", optimized.ToString(), "optimized");
    }

    [TestMethod]
    public void BooleanOptimizer_20_Not_ShouldShould_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Should),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Should),
            }), Occurence.MustNot)
        });
        Assert.AreEqual("(-(F:1 F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(-F:1 -F:2)", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_21_Not_MustMust_NotChanged()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Must),
            }), Occurence.MustNot)
        });
        Assert.AreEqual("(-(+F:1 +F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("(-(+F:1 +F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_22_Not_MustNot_ReduceLevel()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.MustNot)
        });
        Assert.AreEqual("(-(+F:1 -F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("(-(+F:1 -F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_23_Not_NotNot_NotChanged()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
                new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.MustNot),
            }), Occurence.MustNot)
        });
        Assert.AreEqual("(-(-F:1 -F:2))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("(-(-F:1 -F:2))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_24_Not_Not_NotChanged()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.MustNot),
            }), Occurence.MustNot)
        });
        Assert.AreEqual("(-(-F:1))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreSame(predicate, optimized);
        Assert.AreEqual("(-(-F:1))", optimized.ToString(), "optimized");
    }

    [TestMethod]
    public void BooleanOptimizer_30_Recursive()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Must),
                }), Occurence.Must),
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("3")), Occurence.Must),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("4")), Occurence.Must),
                }), Occurence.Must),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(+(+F:1 +F:2) +(+F:3 +F:4)))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(+F:1 +F:2 +F:3 +F:4)", optimized.ToString(), "optimized");
    }

    [TestMethod]
    public void BooleanOptimizer_40_Complex_1()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Should),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Should),
                }), Occurence.Should),
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("3")), Occurence.Must),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("4")), Occurence.Should),
                }), Occurence.Must),
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("5")), Occurence.MustNot),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("6")), Occurence.Should),
                }), Occurence.Must),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+((F:1 F:2) +(+F:3 F:4) +(-F:5 F:6)))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(+(F:1 F:2 +(+F:3 F:4) +(-F:5 F:6)))", optimized.ToString(), "optimized");
    }
    [TestMethod]
    public void BooleanOptimizer_40_Complex_2()
    {
        var predicate = new LogicalPredicate(new[]
        {
            new LogicalClause(new LogicalPredicate(new[]
            {
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("1")), Occurence.Must),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("2")), Occurence.Must),
                }), Occurence.Must),
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("3")), Occurence.MustNot),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("4")), Occurence.Must),
                }), Occurence.Must),
                new LogicalClause(new LogicalPredicate(new[]
                {
                    new LogicalClause(new SimplePredicate("F", new IndexValue("5")), Occurence.MustNot),
                    new LogicalClause(new SimplePredicate("F", new IndexValue("6")), Occurence.MustNot),
                }), Occurence.Must),
            }), Occurence.Must)
        });
        Assert.AreEqual("(+(+(+F:1 +F:2) +(-F:3 +F:4) +(-F:5 -F:6)))", predicate.ToString(), "initial predicate");

        // ACT
        var visitor = new OptimizeBooleansVisitor();
        var optimized = visitor.Visit(predicate);

        // ASSERT
        Assert.AreNotSame(predicate, optimized);
        Assert.AreEqual("(+F:1 +F:2 -F:3 +F:4 -F:5 -F:6)", optimized.ToString(), "optimized");
    }
}