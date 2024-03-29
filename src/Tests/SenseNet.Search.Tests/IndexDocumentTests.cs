﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Indexing;
using SenseNet.Testing;
using SenseNet.Tests;
using SenseNet.Tests.Core;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class IndexDocumentTests : TestBase
    {
        [TestMethod, TestCategory("IR"), TestCategory("Services")]
        public void IndexDoc_Security_CannotAddPassword_CSrv()
        {
            var passwordFieldName = "Password";
            var passwordField = new IndexField(passwordFieldName, "password123",
                IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No);
            var indexDoc = new IndexDocument {passwordField};
            Assert.IsFalse(indexDoc.Any(f => f.Name == passwordFieldName));
            Assert.IsNull(indexDoc.GetStringValue(passwordFieldName));

            Assert.IsFalse(indexDoc.Fields.ContainsKey(passwordFieldName));
        }
        [TestMethod, TestCategory("IR")]
        public void IndexDoc_Security_CannotAddPasswordHash()
        {
            var passwordHashFieldName = "PasswordHash";
            var passwordHashField = new IndexField(passwordHashFieldName, "31275491872354956198543",
                IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No);
            var indexDoc = new IndexDocument {passwordHashField};
            Assert.IsFalse(indexDoc.Any(f => f.Name == passwordHashFieldName));
            Assert.IsNull(indexDoc.GetStringValue(passwordHashFieldName));

            Assert.IsFalse(indexDoc.Fields.ContainsKey(passwordHashFieldName));
        }
    }
}
