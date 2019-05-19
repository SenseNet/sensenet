using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.Search.Indexing;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentTypeTests : TestBase
    {
        [TestMethod]
        [TestCategory("ContentType")]
        public void ContentType_Analyzers()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName1 = "Field1";
            var fieldName2 = "Field2";
            var analyzerValue1 = IndexFieldAnalyzer.Whitespace;
            var analyzerValue2 = IndexFieldAnalyzer.Standard;

            Test(() =>
            {
                var analyzersBefore = SearchManager.SearchEngine.GetAnalyzers();

                /**/ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{contentTypeName}' parentType='GenericContent'
         handler='{typeof(GenericContent).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='{fieldName1}' type='ShortText'><Indexing><Analyzer>{analyzerValue1}</Analyzer></Indexing></Field>
        <Field name='{fieldName2}' type='ShortText'><Indexing><Analyzer>{analyzerValue2}</Analyzer></Indexing></Field>
    </Fields>
</ContentType>
");
                ContentType.GetByName(contentTypeName); // starts the contenttype system

                var analyzersAfter = SearchManager.SearchEngine.GetAnalyzers();

                Assert.IsFalse(analyzersBefore.ContainsKey(fieldName1));
                Assert.IsFalse(analyzersBefore.ContainsKey(fieldName2));

                Assert.IsTrue(analyzersAfter.ContainsKey(fieldName1));
                Assert.IsTrue(analyzersAfter[fieldName1] == analyzerValue1);
                Assert.IsTrue(analyzersAfter.ContainsKey(fieldName2));
                Assert.IsTrue(analyzersAfter[fieldName2] == analyzerValue2);
            });
        }

        [TestMethod]
        [TestCategory("ContentType")]
        [ExpectedException(typeof(ContentRegistrationException))]
        public void ContentType_WrongAnalyzer()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var fieldName = "Field1";
            var analyzerValue = "Lucene.Net.Analysis.KeywordAnalyzer";
            Test(() =>
            {
                var searchEngine = SearchManager.SearchEngine as InMemorySearchEngine;
                Assert.IsNotNull(searchEngine);

                /**/ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{contentTypeName}' parentType='GenericContent'
         handler='{typeof(GenericContent).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='{fieldName}' type='ShortText'>
            <Indexing>
                <Analyzer>{analyzerValue}</Analyzer>
            </Indexing>
        </Field>
    </Fields>
</ContentType>
");
                    Assert.Fail("ContentRegistrationException was not thrown.");
                }
                catch (ContentRegistrationException e)
                {
                    message = e.Message;
                }

                Assert.IsNotNull(message);
                Assert.IsTrue(message.Contains("Invalid analyzer"));
                Assert.IsTrue(message.Contains(contentTypeName));
                Assert.IsTrue(message.Contains(fieldName));
                Assert.IsTrue(message.Contains(analyzerValue));
            });
        }
    }
}
