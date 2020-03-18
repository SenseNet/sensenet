using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema.Metadata;
using SenseNet.OData.Typescript;
using SenseNet.Security;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataMetadataTests : ODataTestBase
    {
        [TestMethod]
        public void OD_Meta_Typescript_Validity()
        {
            ODataTest(() =>
            {
                var schema0 = new Schema(TypescriptGenerationContext.DisabledContentTypeNames);
                var context = new TypescriptGenerationContext();
                var schema1 = new TypescriptTypeCollectorVisitor(context).Visit(schema0);
                var writer = new StringWriter();

                // ACTION
                new TypescriptCtdVisitor(context, writer).Visit(schema1);

                // ASSERT: check the json content even if it is invalid
                var schemaSrc = writer.GetStringBuilder().ToString();

                var lookupStr = "FieldSettings: [";
                var lookupLength = lookupStr.Length;
                var p1 = 0;
                while (true)
                {
                    var p0 = schemaSrc.IndexOf(lookupStr, p1, StringComparison.Ordinal);
                    if (p0 < 0)
                        break;
                    p1 = schemaSrc.IndexOf("}", p0, StringComparison.Ordinal);

                    var src = schemaSrc.Substring(p0 + lookupLength, p1 - p0 - lookupLength);
                    src = src.Trim().TrimStart('{').Trim();
                    var lines = src.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var keys = lines.Select(x => x.Split(':')[0].Trim()).OrderBy(x => x).ToArray();

                    var duplicates = new List<string>();
                    for (var i = 1; i < keys.Length; i++)
                        if(keys[i] == keys[i-1])
                            duplicates.Add(keys[i]);
                    var duplication = string.Join(", ", duplicates);

                    Assert.AreEqual("", duplication);
                }
            });
        }
    }
}
