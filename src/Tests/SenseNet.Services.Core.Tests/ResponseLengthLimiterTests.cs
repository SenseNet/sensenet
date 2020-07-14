using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Testing;
using SenseNet.Tests;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class ResponseLengthLimiterTests : TestBase
    {
        [TestMethod]
        public void ResponseLengthLimiter_Greater()
        {
            using (GetSwindler())
            {
                Test((builder) => { builder.UseResponseLengthLimiter(9); }, () =>
                    {
                        var expected = "Response-1";
                        string actual;
                        try
                        {
                            actual = GetResponse((httpContext) =>
                            {
                                httpContext.Response.WriteLimitedAsync(expected)
                                    .ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (ApplicationException e)
                        {
                            actual = e.Message;
                        }

                        Assert.AreEqual("Response limit exceeded.", actual);
                    }
                );
            }
        }
        [TestMethod]
        public void ResponseLengthLimiter_Equals()
        {
            using (GetSwindler())
            {
                Test((builder) => { builder.UseResponseLengthLimiter(10); }, () =>
                {
                    var expected = "Response-1";
                    var actual = GetResponse((httpContext) =>
                    {
                        httpContext.Response.WriteLimitedAsync(expected)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    });
                    Assert.AreEqual(expected, actual);
                });
            }
        }
        [TestMethod]
        public void ResponseLengthLimiter_Lower()
        {
            using (GetSwindler())
            {
                Test((builder) => { builder.UseResponseLengthLimiter(11); }, () =>
                {
                    var expected = "Response-1";
                    var actual = GetResponse((httpContext) =>
                    {
                        httpContext.Response.WriteLimitedAsync(expected)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    });
                    Assert.AreEqual(expected, actual);
                });
            }
        }
        [TestMethod]
        public void ResponseLengthLimiter_WriteMoreTimes()
        {
            using (GetSwindler())
            {
                Test((builder) => { builder.UseResponseLengthLimiter(29); }, () =>
                    {
                        var expected = "Response-1";
                        var limiter = Providers.Instance.GetProvider<IResponseLengthLimiter>();

                        string actual;
                        try
                        {
                            actual = GetResponse((httpContext) =>
                            {
                                var response = httpContext.Response;
                                Assert.AreEqual(0L, limiter.GetCurrentLength(httpContext));

                                response.WriteLimitedAsync(expected).ConfigureAwait(false).GetAwaiter().GetResult();
                                Assert.AreEqual(10L, limiter.GetCurrentLength(httpContext));

                                response.WriteLimitedAsync(expected).ConfigureAwait(false).GetAwaiter().GetResult();
                                Assert.AreEqual(20L, limiter.GetCurrentLength(httpContext));

                                response.WriteLimitedAsync(expected).ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (ApplicationException e)
                        {
                            actual = e.Message;
                        }

                        Assert.AreEqual("Response limit exceeded.", actual);
                    }
                );
            }
        }

        private static string GetResponse(Action<HttpContext> writeAction)
        {
            var httpContext = CreateHttpContext("/example,com", "?a=b");
            httpContext.Response.Body = new MemoryStream();

            writeAction(httpContext);

            var responseOutput = httpContext.Response.Body;
            responseOutput.Seek(0, SeekOrigin.Begin);
            string output;
            using (var reader = new StreamReader(responseOutput))
                output = reader.ReadToEndAsync()
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            return output;
        }
        internal static HttpContext CreateHttpContext(string resource, string queryString)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }

        private IDisposable GetSwindler()
        {
            return new Swindler<IResponseLengthLimiter>(null,
                () => Providers.Instance.GetProvider<IResponseLengthLimiter>(),
                (x) => Providers.Instance.SetProvider(typeof(IResponseLengthLimiter), x)
            );
        }

    }
}
