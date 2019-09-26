using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Linq;
using SenseNet.OData;
using SenseNet.OData.Parser;
using SenseNet.OData.Responses;
using SenseNet.Tests.Accessors;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataExecutionTests : ODataTestBase
    {
        [TestMethod]
        public void OD_Expr_()
        {
            ODataTest(() =>
            {
                var expectedExpr = Content.All
                    .Where(z => z.Name.Contains("s"));

                // Get regular children response
                var response = ODataGET<ODataChildrenCollectionResponse>(
                    "/OData.svc/Root/IMS/BuiltIn/Portal",
                    "?$filter=isof('User')",
                    out var request);

                // Modify by application's middleware
                response.Source = response.Source
                    .Where(x => x.Name.Contains("s"));

                var sourceExpr = (response.Source is IQueryable<Content> queryable) ? queryable.Expression : null;

                var childrenDef = new ChildrenDefinition {Top = 12};

                // ACTION: Get content query
                var snQuery = SnExpression.BuildQuery(sourceExpr, request.Filter, typeof(Content),
                    request.RepositoryPath, childrenDef, out string elementSelection);

                // ASSERT
                Assert.AreEqual("+(+Name:*s* +InFolder:/root/ims/builtin/portal) +TypeIs:user .TOP:12", snQuery.ToString());
            });
        }
    }
}
