using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.InMemory;
using SenseNet.OData.Metadata;
using SenseNet.Search.Indexing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;

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

                ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
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
            });
        }

        private enum CheckFieldResult { CtdError, /*SchemaError, FieldExists,*/ NoError }

        [TestMethod]
        [TestCategory("ContentType")]
        public void ContentType_FieldNameBlackList()
        {
            var contentTypeName = System.Reflection.MethodBase.GetCurrentMethod().Name;

            var fieldNames = new[] {"Actions", "Type", "TypeIs", "Children", "InFolder", "InTree",
                "IsSystemContent", "SharedWith", "SharedBy","SharingMode", "SharingLevel"};

            var ctdErrors = new List<string>();
            var noErrors = new List<string>();
            Test(() =>
            {
                foreach (var fieldName in fieldNames)
                {
                    switch (CheckField(contentTypeName, fieldName))
                    {
                        case CheckFieldResult.CtdError: ctdErrors.Add(fieldName); break;
                        case CheckFieldResult.NoError: noErrors.Add(fieldName); break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                Assert.AreEqual(fieldNames.Length, ctdErrors.Count);
            });
        }

        private CheckFieldResult CheckField(string contentTypeName, string fieldName)
        {
            var result = CheckField(contentTypeName, fieldName, "ShortText");
            if (result == CheckFieldResult.NoError)
                result = CheckField(contentTypeName, fieldName, "Integer");
            return result;
        }
        private CheckFieldResult CheckField(string contentTypeName, string fieldName, string fieldType)
        {
            var result = CheckFieldResult.NoError;

            try
            {
                ContentTypeInstaller.InstallContentType($@"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='{contentTypeName}' parentType='GenericContent'
         handler='{typeof(GenericContent).FullName}' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
    <Fields>
        <Field name='{fieldName}' type='{fieldType}'/>
    </Fields>
</ContentType>
");
            }
            catch (Exception e)
            {
                result = CheckFieldResult.CtdError;
            }

            var contentType = ContentType.GetByName(contentTypeName);
            if(contentType != null)
                ContentTypeInstaller.RemoveContentType(contentTypeName);

            return result;
        }
    }
}
