using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.Search;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class PredicationEngineTests : TestBase
    {
        //readonly Dictionary<string, IPerFieldIndexingInfo> _indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
        //{
        //    //{"_Text", new TestPerfieldIndexingInfoString()},
        //    {"#Field1", new TestPerfieldIndexingInfoString()},
        //    {"Field1", new TestPerfieldIndexingInfoString()},
        //    {"Field2", new TestPerfieldIndexingInfoString()},
        //    {"Field3", new TestPerfieldIndexingInfoString()},
        //    {"F1", new TestPerfieldIndexingInfoString()},
        //    {"F2", new TestPerfieldIndexingInfoString()},
        //    {"F3", new TestPerfieldIndexingInfoString()},
        //    {"F4", new TestPerfieldIndexingInfoString()},
        //    {"f1", new TestPerfieldIndexingInfoString()},
        //    {"f2", new TestPerfieldIndexingInfoString()},
        //    {"f3", new TestPerfieldIndexingInfoString()},
        //    {"f4", new TestPerfieldIndexingInfoString()},
        //    {"f5", new TestPerfieldIndexingInfoString()},
        //    {"f6", new TestPerfieldIndexingInfoString()},
        //    {"mod_date", new TestPerfieldIndexingInfoInt()},
        //    {"title", new TestPerfieldIndexingInfoString()},
        //    {"Name", new TestPerfieldIndexingInfoString()},
        //    {"Id", new TestPerfieldIndexingInfoInt()},
        //    {"LongField1", new TestPerfieldIndexingInfoLong()},
        //    {"SingleField1", new TestPerfieldIndexingInfoSingle()},
        //    {"DoubleField1", new TestPerfieldIndexingInfoDouble()},
        //    {"IsSystemContent", new TestPerfieldIndexingInfoBool()},
        //    {"EnableLifespan", new TestPerfieldIndexingInfoBool()},
        //    {"ValidFrom", new TestPerfieldIndexingInfoDateTime()},
        //    {"ValidTill", new TestPerfieldIndexingInfoDateTime()},
        //};

        private static readonly string CTD = $@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='PredicationEngineTestNode' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
	<Fields>
		<Field name='String1' type='ShortText' />
		<Field name='Int1' type='Integer' />
		<Field name='DateTime1' type='DateTime' />
		<Field name='Currency1' type='Currency' />
		<Field name='LongText1' type='LongText' />
		<Field name='Reference1' type='Reference' />
		<Field name='Binary1' type='Binary' />
	</Fields>
</ContentType>
";


        [TestMethod]
        public void PredicationEngine_Predications()
        {
            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CTD);
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var content = Content.CreateNew("PredicationEngineTestNode", root, "PredicationEngineTestNode1");
                content.Index = 42;
                content["DateTime1"] = new DateTime(1234, 5, 6);
                content["Currency1"] = 42.42;
                content.Save();

                var prE = new PredicationEngine(content);

                // equality
                Assert.IsTrue(prE.IsTrue("+Name:predicationENGINEtestnode1"));
                Assert.IsFalse(prE.IsTrue("-Name:predicationENGINEtestnode1"));
                Assert.IsFalse(prE.IsTrue("+Name:anothername"));
                Assert.IsTrue(prE.IsTrue("+Index:42"));
                Assert.IsFalse(prE.IsTrue("-Index:42"));
                Assert.IsFalse(prE.IsTrue("+Index:41"));
                Assert.IsFalse(prE.IsTrue("+Index:43"));
                //Assert.IsTrue(prE.IsTrue("-Index:43"));
                Assert.IsTrue(prE.IsTrue("+DateTime1:'1234-05-06'"));
                Assert.IsFalse(prE.IsTrue("-DateTime1:'1234-05-06'"));
                Assert.IsFalse(prE.IsTrue("+DateTime1:'1234-05-05'"));
                //Assert.IsTrue(prE.IsTrue("-DateTime1:'1234-05-05'"));
                Assert.IsFalse(prE.IsTrue("+DateTime1:'1234-05-07'"));
                Assert.IsTrue(prE.IsTrue("+Currency1:42.42"));
                Assert.IsFalse(prE.IsTrue("-Currency1:42.42"));
                Assert.IsFalse(prE.IsTrue("+Currency1:42.41"));
                Assert.IsFalse(prE.IsTrue("+Currency1:42.43"));
                Assert.IsTrue(prE.IsTrue("+IsSystemContent:yes"));
                Assert.IsFalse(prE.IsTrue("+IsSystemContent:no"));

                // wildcard
                Assert.IsTrue(prE.IsTrue("+Name:*engine*"));
                Assert.IsFalse(prE.IsTrue("+Name:*Molokai*"));
                Assert.IsTrue(prE.IsTrue("+Name:*testnode1"));
                Assert.IsFalse(prE.IsTrue("+Name:*Oahu"));
                Assert.IsTrue(prE.IsTrue("+Name:predication*"));
                Assert.IsFalse(prE.IsTrue("+Name:Kauai*"));
                Assert.IsTrue(prE.IsTrue("+Name:predication*testnode1"));
                Assert.IsFalse(prE.IsTrue("+Name:Maui*Lanai"));

                // basic range
                Assert.IsTrue(prE.IsTrue("+Index:>41"));
                Assert.IsFalse(prE.IsTrue("+Index:>42"));
                Assert.IsFalse(prE.IsTrue("+Index:<42"));
                Assert.IsTrue(prE.IsTrue("+Index:<43"));
                Assert.IsTrue(prE.IsTrue("+Index:>=41"));
                Assert.IsTrue(prE.IsTrue("+Index:>=42"));
                Assert.IsTrue(prE.IsTrue("+Index:<=42"));
                Assert.IsTrue(prE.IsTrue("+Index:<=43"));
                Assert.IsTrue(prE.IsTrue("+DateTime1:>'1234-05-05'"));
                Assert.IsTrue(prE.IsTrue("+DateTime1:<'1234-05-07'"));
                Assert.IsTrue(prE.IsTrue("+DateTime1:>'999-05-07'"));
                Assert.IsTrue(prE.IsTrue("+Currency1:>42.41"));
                Assert.IsTrue(prE.IsTrue("+Currency1:<42.43"));
                Assert.IsTrue(prE.IsTrue("+Currency1:>9"));

                // extended range
                Assert.IsTrue(prE.IsTrue("+Index:[40 TO 44]"));
                Assert.IsTrue(prE.IsTrue("+Index:[40 TO 44}"));
                Assert.IsTrue(prE.IsTrue("+Index:{40 TO 44]"));
                Assert.IsTrue(prE.IsTrue("+Index:{40 TO 44}"));

                Assert.IsTrue(prE.IsTrue("+Index:[40 TO 42]"));
                Assert.IsFalse(prE.IsTrue("+Index:[40 TO 42}"));
                Assert.IsTrue(prE.IsTrue("+Index:{40 TO 42]"));
                Assert.IsFalse(prE.IsTrue("+Index:{40 TO 42}"));

                Assert.IsTrue(prE.IsTrue("+Index:[42 TO 44]"));
                Assert.IsTrue(prE.IsTrue("+Index:[42 TO 44}"));
                Assert.IsFalse(prE.IsTrue("+Index:{42 TO 44]"));
                Assert.IsFalse(prE.IsTrue("+Index:{42 TO 44}"));

                Assert.IsFalse(prE.IsTrue("+Index:[0 TO 40]"));
                Assert.IsFalse(prE.IsTrue("+Index:[0 TO 40}"));
                Assert.IsFalse(prE.IsTrue("+Index:{0 TO 40]"));
                Assert.IsFalse(prE.IsTrue("+Index:{0 TO 40}"));

                Assert.IsFalse(prE.IsTrue("+Index:[44 TO 50]"));
                Assert.IsFalse(prE.IsTrue("+Index:[44 TO 50}"));
                Assert.IsFalse(prE.IsTrue("+Index:{44 TO 50]"));
                Assert.IsFalse(prE.IsTrue("+Index:{44 TO 50}"));

                // complex (logical) predicates
                Assert.IsTrue(prE.IsTrue("Index:41 Index:42 Index:43"));
                //Assert.IsFalse(prE.IsTrue("-(Index:41 Index:42 Index:43)")); // useless
                Assert.IsFalse(prE.IsTrue("-(+Index:41 +Index:42 +Index:43)"));
                Assert.IsTrue(prE.IsTrue("Index:(41 42 43)"));
                //Assert.IsFalse(prE.IsTrue("-Index:(41 42 43)")); // useless
                Assert.IsTrue(prE.IsTrue("+DateTime1:'1234-05-06' +(Index:41 Index:42)"));
                Assert.IsFalse(prE.IsTrue("+DateTime1:'1234-05-06' +(Index:41 Index:43)"));

                Assert.IsTrue(prE.IsTrue("DateTime1:'1234-05-06' AND (Index:41 OR Index:42)"));
                Assert.IsTrue(prE.IsTrue("Index:41 OR Index:42 OR Index:43"));

                //Assert.IsTrue(prE.IsTrue("NOT Index:43"));
                Assert.IsFalse(prE.IsTrue("NOT Index:42"));
                Assert.IsFalse(prE.IsTrue("(Index:41 AND Index:42 AND Index:43)"));
                //Assert.IsTrue(prE.IsTrue("NOT (Index:41 AND Index:42 AND Index:43)"));
            });
        }

        [TestMethod]
        public void PredicationEngine_Errors()
        {
            void AssertError(PredicationEngine prE, string predication, Type exceptionType)
            {
                try
                {
                    prE.IsTrue(predication);
                    Assert.Fail($"{exceptionType.Namespace} was not thrown.");
                }
                catch (Exception e)
                {
                    if (e.GetType() != exceptionType)
                        Assert.Fail($"{e.GetType().Name} was thrown. Expected: {exceptionType.Name}.");
                }
            }

            Test(() =>
            {
                ContentTypeInstaller.InstallContentType(CTD);
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var content = Content.CreateNew("PredicationEngineTestNode", root, "PredicationEngineTestNode1");
                content.Index = 42;
                content["Currency1"] = 42.42;
                content["DateTime1"] = new DateTime(1234, 5, 6);
                content.Save();

                var prE = new PredicationEngine(content);

                // equality
                AssertError(prE, "Name:a?b", typeof(NotSupportedException)); // The '?' wildcard is not allowed
                AssertError(prE, "Name:*", typeof(NotSupportedException)); // The only one '*' is not allowed;
            });
        }

        [TestMethod]
        public void PredicationEngine_IndexValue_Comparer()
        {
            // ALIGN
            // reverse order by type and value
            var inputValues = new[]
            {
                // 0, 1, 2 DateTime
                new IndexValue(DateTime.MaxValue), new IndexValue(DateTime.Now), new IndexValue(DateTime.MinValue),
                // 3, 4, 5 Double
                new IndexValue(1.0d), new IndexValue(0.0d), new IndexValue(-1.0d),
                // 6, 7, 8 Float
                new IndexValue(1.0f), new IndexValue(0.0f), new IndexValue(-1.0f),
                // 9, 10, 11 Long
                new IndexValue(1L), new IndexValue(0L), new IndexValue(-1L),
                // 12, 13, 14, Int
                new IndexValue(1), new IndexValue(0), new IndexValue(-1),
                // 15, 16  Bool
                new IndexValue(true), new IndexValue(false),
                // 17, 18 StringArray
                new IndexValue(new []{ "zzz", "aaa"}), new IndexValue(new []{ "aaa", "zzz"}),
                // 19, 20, 21 String
                new IndexValue("1"), new IndexValue("0"), new IndexValue("-1"),
            };

            var expected = Enumerable.Range(0, inputValues.Length)
                .OrderByDescending(x => x)
                .Select(x=> inputValues[x])
                .ToArray();

            // ACTION
            var actual = inputValues.OrderBy(x => x).ToArray();

            // ASSERT
            for (int i = 0; i < actual.Length; i++)
                Assert.AreSame(expected[i], actual[i]);
        }
        [TestMethod]
        [SuppressMessage("ReSharper", "EqualExpressionComparison")]
        public void PredicationEngine_IndexValue_Operators()
        {
            var zero = new IndexValue(0.0f);
            var one = new IndexValue(1.0f);

            Assert.IsTrue(zero == zero);
            Assert.IsFalse(zero < zero);
            Assert.IsTrue(zero <= zero);
            Assert.IsFalse(zero > zero);
            Assert.IsTrue(zero >= zero);

            Assert.IsFalse(zero == one);
            Assert.IsTrue(zero < one);
            Assert.IsTrue(zero <= one);
            Assert.IsFalse(zero > one);
            Assert.IsFalse(zero >= one);

            // single is always less than a double
            var @single = new IndexValue(42.0f);
            var @double = new IndexValue(0.0d);
            Assert.IsFalse(@single == @double);
            Assert.IsTrue(@single < @double);
            Assert.IsTrue(@single <= @double);
            Assert.IsFalse(@single > @double);
            Assert.IsFalse(@single >= @double);

        }
    }
}
/*




*/