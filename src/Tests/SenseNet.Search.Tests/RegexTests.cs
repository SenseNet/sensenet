using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying.Parser;
using SenseNet.Search.Querying.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class RegexTests : TestBase
    {
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Parser_Regex()
        {
            ParserTest('"', @"/[1a-zA-Z]{3}\\S{1}[\']\\w*/", @"/[1a-zA-Z]{3}\S{1}[']\w*/");
            ParserTest('\'', @"/[2a-zA-Z]{3}\\S{1}[\']\\w*/", @"/[2a-zA-Z]{3}\S{1}[']\w*/");
            ParserTest('"', @"/[1a-zA-Z]{3}\\S{1}[\""]\\w*/", @"/[1a-zA-Z]{3}\S{1}[""]\w*/");
            ParserTest('\'', @"/[2a-zA-Z]{3}\\S{1}[\""]\\w*/", @"/[2a-zA-Z]{3}\S{1}[""]\w*/");

            ParserTest('"', "/[1a-zA-Z]{3}\\\\S{1}[\\']\\\\w*/", @"/[1a-zA-Z]{3}\S{1}[']\w*/");
            ParserTest('\'', "/[2a-zA-Z]{3}\\\\S{1}[\\']\\\\w*/", @"/[2a-zA-Z]{3}\S{1}[']\w*/");
            ParserTest('"', "/[1a-zA-Z]{3}\\\\S{1}[\\\"]\\\\w*/", @"/[1a-zA-Z]{3}\S{1}[""]\w*/");
            ParserTest('\'', "/[2a-zA-Z]{3}\\\\S{1}[\\\"]\\\\w*/", @"/[2a-zA-Z]{3}\S{1}[""]\w*/");
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Parser_Regex_email()
        {
            // Original expectation:
            //     Binary:'/(?i)(?<=^|[^a-z0-9!#$%\&'*+\/=?\^_`{|}~-])[a-z0-9!#$%\&'*+\/=?\^_`{|}~-]{1,256}(?:\.[a-z0-9!#$%\&'*+\/=?\^_`{|}~-]{1,256}){0,256}@(?:[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?(?=$|[^a-z0-9])/'
            ParserTest('"',
                @"(?i)(?<=^|[^a-z0-9!#$%\\&\'*+\\/=?\\^_`{|}~-])[a-z0-9!#$%\\&\'*+\\/=?\\^_`{|}~-]{1,256}(?:\\.[a-z0-9!#$%\\&\'*+\\/=?\\^_`{|}~-]{1,256}){0,256}@(?:[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?(?=$|[^a-z0-9])",
                @"(?i)(?<=^|[^a-z0-9!#$%\&'*+\/=?\^_`{|}~-])[a-z0-9!#$%\&'*+\/=?\^_`{|}~-]{1,256}(?:\.[a-z0-9!#$%\&'*+\/=?\^_`{|}~-]{1,256}){0,256}@(?:[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?(?=$|[^a-z0-9])");
            ParserTest('\'',
                @"(?i)(?<=^|[^a-z0-9!#$%\\&\'*+\\/=?\\^_`{|}~-])[a-z0-9!#$%\\&\'*+\\/=?\\^_`{|}~-]{1,256}(?:\\.[a-z0-9!#$%\\&\'*+\\/=?\\^_`{|}~-]{1,256}){0,256}@(?:[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?(?=$|[^a-z0-9])",
                @"(?i)(?<=^|[^a-z0-9!#$%\&'*+\/=?\^_`{|}~-])[a-z0-9!#$%\&'*+\/=?\^_`{|}~-]{1,256}(?:\.[a-z0-9!#$%\&'*+\/=?\^_`{|}~-]{1,256}){0,256}@(?:[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]{0,256}[a-z0-9])?(?=$|[^a-z0-9])");
        }
        private void ParserTest(char stringDelimiter, string regex, string expected = null)
        {
            expected ??= regex;

            var fieldName = "Name";
            var queryText = $"{fieldName}:{stringDelimiter}{regex}{stringDelimiter}";
            var nameIndexingInfo = new TestPerfieldIndexingInfoString();
            nameIndexingInfo.IndexFieldHandler = new LowerStringIndexHandler();
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo> { { "Name", nameIndexingInfo } };

            // ACTION
            var queryContext = new TestQueryContext(QuerySettings.Default, Identifiers.SystemUserId, indexingInfo);
            var snQuery = new CqlParser().Parse(queryText, queryContext);

            // ASSERT
            Assert.IsInstanceOfType(snQuery.QueryTree, typeof(SimplePredicate));
            var simplePredicate = snQuery.QueryTree as SimplePredicate;
            Assert.IsNotNull(simplePredicate);
            Assert.AreEqual(IndexValueType.String, simplePredicate.Value.Type);
            Assert.AreEqual(expected, simplePredicate.Value.StringValue);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Linq_Regex()
        {
            var regex = "/[a-zA-Z]{3}\\S{1}\\w*/";
            var fieldName = "Name";
            var nameIndexingInfo = new TestPerfieldIndexingInfoString();
            nameIndexingInfo.IndexFieldHandler = new LowerStringIndexHandler();
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo> { { "Name", nameIndexingInfo } };
            var services = new ServiceCollection().BuildServiceProvider();
            Providers.Instance = new Providers(services);
            var contentTypeManager = Activator.CreateInstance(typeof(ContentTypeManager), true);
            Providers.Instance.SetProvider("ContentTypeManager", contentTypeManager);
            var contentTypeManagerAcc = new TypeAccessor(typeof(ContentTypeManager));
            var indexingInfoTable =
                (Dictionary<string, IPerFieldIndexingInfo>)contentTypeManagerAcc.GetStaticField("_indexingInfoTable");
            IPerFieldIndexingInfo backup = null;
            try
            {
                indexingInfoTable.TryGetValue(fieldName, out backup);
                indexingInfoTable[fieldName] = nameIndexingInfo;

                // ACTION
                var queryable = Content.All.Where(c => c.Name == regex);
                var cs = (ContentSet<Content>)queryable.Provider;
                var snQuery = cs.GetCompiledQuery();

                // ASSERT
                Assert.IsInstanceOfType(snQuery.QueryTree, typeof(SimplePredicate));
                var simplePredicate = snQuery.QueryTree as SimplePredicate;
                Assert.IsNotNull(simplePredicate);
                Assert.AreEqual(IndexValueType.String, simplePredicate.Value.Type);
                Assert.AreEqual(regex, simplePredicate.Value.StringValue);
            }
            finally
            {
                if (backup == null)
                    indexingInfoTable.Remove(fieldName);
                else
                    indexingInfoTable[fieldName] = backup;
            }
        }
    }
}
