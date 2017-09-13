using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class IndexDocumentTests
    {
        [TestMethod, TestCategory("IR")]
        public void IndexDoc_Security_CannotAddPassword()
        {
            var passwordFieldName = "Password";
            var passwordField = new IndexField(passwordFieldName, "password123",
                IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No);
            var indexDoc = new IndexDocument {passwordField};
            Assert.IsFalse(indexDoc.Any(f => f.Name == passwordFieldName));
            Assert.IsNull(indexDoc.GetStringValue(passwordFieldName));

            var indexDocAcc = new PrivateObject(indexDoc);
            var fields = (Dictionary<string, IndexField>)indexDocAcc.GetFieldOrProperty("_fields");
            Assert.IsFalse(fields.ContainsKey(passwordFieldName));
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

            var indexDocAcc = new PrivateObject(indexDoc);
            var fields = (Dictionary<string, IndexField>)indexDocAcc.GetFieldOrProperty("_fields");
            Assert.IsFalse(fields.ContainsKey(passwordHashFieldName));
        }
    }
}
