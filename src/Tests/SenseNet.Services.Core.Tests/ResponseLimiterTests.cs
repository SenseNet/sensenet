using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class ResponseLimiterTests : TestBase
    {
        [TestMethod]
        public void Limiter_FileLength()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(10, 10); }, () =>
                    {
                        var responses = new string[3];
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                ResponseLimiter.AssertFileLength(i + 9);
                                responses[i] = "Ok.";
                            }
                            catch (ApplicationException e)
                            {
                                responses[i] = e.Message;
                            }

                        }
                        var actual = string.Join(" ", responses);
                        Assert.AreEqual("Ok. Ok. File length limit exceeded.", actual);
                    }
                );
            }
        }
        [TestMethod]
        public void Limiter_ResponseLength()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(10, 10); }, () =>
                    {
                        var responses = new string[3];
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                ResponseLimiter.AssertResponseLength(i + 9);
                                responses[i] = "Ok.";
                            }
                            catch (ApplicationException e)
                            {
                                responses[i] = e.Message;
                            }

                        }
                        var actual = string.Join(" ", responses);
                        Assert.AreEqual("Ok. Ok. Response length limit exceeded.", actual);
                    }
                );
            }
        }

        [TestMethod]
        public void Limiter_FileLength_Upgrade()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(10, 10); }, () =>
                {
                    ResponseLimiter.ModifyFileLengthLimit(20);
                    var responses = new string[3];
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            ResponseLimiter.AssertFileLength(i + 19);
                            responses[i] = "Ok.";
                        }
                        catch (ApplicationException e)
                        {
                            responses[i] = e.Message;
                        }

                    }
                    var actual = string.Join(" ", responses);
                    Assert.AreEqual("Ok. Ok. File length limit exceeded.", actual);
                }
                );
            }
        }
        [TestMethod]
        public void Limiter_ResponseLength_Upgrade()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(10, 10); }, () =>
                {
                    ResponseLimiter.ModifyResponseLengthLimit(20);
                    var responses = new string[3];
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            ResponseLimiter.AssertResponseLength(i + 19);
                            responses[i] = "Ok.";
                        }
                        catch (ApplicationException e)
                        {
                            responses[i] = e.Message;
                        }

                    }
                    var actual = string.Join(" ", responses);
                    Assert.AreEqual("Ok. Ok. Response length limit exceeded.", actual);
                }
                );
            }
        }


        [TestMethod]
        public void Limiter_ResponseLength_AssertMore()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(29, 10); }, () =>
                    {
                        var responses = new string[4];
                        var httpContext = CreateHttpContext();
                        var response = httpContext.Response;
                        try
                        {
                            ResponseLimiter.AssertResponseLength(response, 10);
                            responses[0] = ResponseLimiter.GetCurrentResponseLength(response).ToString();
                            ResponseLimiter.AssertResponseLength(response, 10);
                            responses[1] = ResponseLimiter.GetCurrentResponseLength(response).ToString();
                            ResponseLimiter.AssertResponseLength(httpContext.Response, 10);
                            Assert.Fail("ApplicationException was not thrown");
                        }
                        catch (ApplicationException e)
                        {
                            responses[2] = ResponseLimiter.GetCurrentResponseLength(response).ToString();
                            responses[3] = e.Message;
                        }

                        var actual = string.Join(" ", responses);
                        Assert.AreEqual("10 20 20 Response length limit exceeded.", actual);
                    }
                );
            }
        }

        /* =========================================================================== TOOLS */

        internal static HttpContext CreateHttpContext()
        {
            var resource = "/example,com";
            var queryString = "?a=b";
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }

        private IDisposable GetResetResponseLimiterSwindler()
        {
            return new Swindler<IResponseLimiter>(null,
                () => Providers.Instance.GetProvider<IResponseLimiter>(),
                (x) => Providers.Instance.SetProvider(typeof(IResponseLimiter), x)
            );
        }

    }
}
