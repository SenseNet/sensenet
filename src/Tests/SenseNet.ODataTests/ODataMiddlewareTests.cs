using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.OData;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataMiddlewareTests : ODataTestBase
    {
        private class TestMiddleware
        {
            public TestMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            private readonly RequestDelegate _next;
            public async Task InvokeAsync(HttpContext context)
            {
                context.Response.Headers.Add("Header1", "HeaderValue1");
                if (_next != null)
                    await _next(context).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task OD_Middleware()
        {
            await ODataTestAsync(async () =>
            {
                // ARRANGE

                // Create HttpContext 
                var httpContext = new DefaultHttpContext();
                var request = httpContext.Request;
                request.Path = "/OData.svc/Root('IMS')/Name/$value";
                request.QueryString = QueryString.Empty;
                request.Method = "GET";
                var responseStream = httpContext.Response.Body = new MemoryStream();

                // ACTION: Simulate the aspnet framework
                // instantiate the OData with the next chain member
                var odata = new ODataMiddleware(new TestMiddleware(null).InvokeAsync);
                // call the first of the chain
                await odata.InvokeAsync(httpContext);

                // ASSERT
                // check response
                string text;
                responseStream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(responseStream))
                    text = reader.ReadToEnd();
                Assert.AreEqual("IMS", text);
                // check additional header
                var header = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Header1");
                Assert.IsNotNull(header);
                Assert.AreEqual("HeaderValue1", header.Value.ToString());
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Middleware_null()
        {
            await ODataTestAsync(async () =>
            {
                // ARRANGE

                // Create HttpContext 
                var httpContext = new DefaultHttpContext();
                var request = httpContext.Request;
                request.Path = "/OData.svc/Root('IMS')/Name/$value";
                request.QueryString = QueryString.Empty;
                request.Method = "GET";
                var responseStream = httpContext.Response.Body = new MemoryStream();

                // ACTION: Simulate the aspnet framework
                var odata = new ODataMiddleware(null);
                // call the first of the chain
                await odata.InvokeAsync(httpContext);

                // ASSERT
                // check response
                string text;
                responseStream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(responseStream))
                    text = reader.ReadToEnd();
                Assert.AreEqual("IMS", text);
                // there is no additional headers
                var header = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Header1");
                Assert.IsNull(header.Key);
                Assert.AreEqual(0, header.Value.Count);
            }).ConfigureAwait(false);
        }
    }
}
