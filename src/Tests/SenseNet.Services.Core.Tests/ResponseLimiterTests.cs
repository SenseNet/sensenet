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
using SenseNet.Tests;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class ResponseLimiterTests : TestBase
    {
        // Abbreviations in the test names
        // FL: file length, RL: response length, FRL: Combined file and response length 

        [TestMethod]
        public void Limiter_FL_Greater()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(9, 9); }, () =>
                {
                    var stream = CreateStream("Response-1");
                    string actual;
                    try
                    {
                        actual = GetResponse(httpContext =>
                        {
                            stream.CopyToLimited(httpContext.Response.Body);
                        });
                    }
                    catch (ApplicationException e)
                    {
                        actual = e.Message;
                    }

                    Assert.AreEqual("File length limit exceeded.", actual);
                }
                );
            }
        }
        [TestMethod]
        public void Limiter_FL_Greater_Buffer()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(9, 9); }, () =>
                {
                    var stream = CreateStream("Response-1");
                    string actual;
                    try
                    {
                        actual = GetResponse((httpContext) =>
                        {
                            stream.CopyToLimited(httpContext.Response.Body, 5);
                        });
                    }
                    catch (ApplicationException e)
                    {
                        actual = e.Message;
                    }

                    Assert.AreEqual("File length limit exceeded.", actual);
                }
                );
            }
        }

        [TestMethod]
        public void Limiter_FL_Greater_Async()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(9, 9); }, () =>
                    {
                        var stream = CreateStream("Response-1");
                        string actual;
                        try
                        {
                            actual = GetResponse(httpContext =>
                            {
                                stream.CopyToLimitedAsync(httpContext.Response.Body)
                                    .ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (ApplicationException e)
                        {
                            actual = e.Message;
                        }

                        Assert.AreEqual("File length limit exceeded.", actual);
                    }
                );
            }
        }
        [TestMethod]
        public void Limiter_FL_Greater_Async_Buffer()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(9, 9); }, () =>
                    {
                        var stream = CreateStream("Response-1");
                        string actual;
                        try
                        {
                            actual = GetResponse((httpContext) =>
                            {
                                stream.CopyToLimitedAsync(httpContext.Response.Body, 5)
                                    .ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (ApplicationException e)
                        {
                            actual = e.Message;
                        }

                        Assert.AreEqual("File length limit exceeded.", actual);
                    }
                );
            }
        }
        [TestMethod]
        public void Limiter_FL_Greater_Async_Buffer_CancellationToken()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(9, 9); }, () =>
                    {
                        var stream = CreateStream("Response-1");
                        string actual;
                        try
                        {
                            actual = GetResponse((httpContext) =>
                            {
                                stream
                                    .CopyToLimitedAsync(httpContext.Response.Body, 5, CancellationToken.None)
                                    .ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (ApplicationException e)
                        {
                            actual = e.Message;
                        }

                        Assert.AreEqual("File length limit exceeded.", actual);
                    }
                );
            }
        }
        [TestMethod]
        public void Limiter_FL_Equals()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(10, 10); }, () =>
                {
                    var expected = "Response-1";
                    var stream = CreateStream(expected);
                    var actual = GetResponse((httpContext) =>
                    {
                        stream.CopyToLimited(httpContext.Response.Body);
                    });
                    Assert.AreEqual(expected, actual);
                });
            }
        }
        [TestMethod]
        public void Limiter_FL_Lower()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(11, 11); }, () =>
                {
                    var expected = "Response-1";
                    var stream = CreateStream(expected);
                    var actual = GetResponse((httpContext) =>
                    {
                        stream.CopyToLimited(httpContext.Response.Body);
                    });
                    Assert.AreEqual(expected, actual);
                });
            }
        }

        [TestMethod]
        public void Limiter_RL_Greater()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(9); }, () =>
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

                        Assert.AreEqual("Response length limit exceeded.", actual);
                    }
                );
            }
        }
        [TestMethod]
        public void Limiter_RL_Equals()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(10); }, () =>
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
        public void Limiter_RL_Lower()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(11); }, () =>
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
        public void Limiter_RL_WriteMoreTimes()
        {
            using (GetResetResponseLimiterSwindler())
            {
                Test((builder) => { builder.UseResponseLimiter(29); }, () =>
                    {
                        var expected = "Response-1";
                        var limiter = Providers.Instance.GetProvider<IResponseLimiter>();

                        string actual;
                        try
                        {
                            actual = GetResponse((httpContext) =>
                            {
                                var response = httpContext.Response;
                                Assert.AreEqual(0L, limiter.GetCurrentResponseLength(httpContext));

                                response.WriteLimitedAsync(expected).ConfigureAwait(false).GetAwaiter().GetResult();
                                Assert.AreEqual(10L, limiter.GetCurrentResponseLength(httpContext));

                                response.WriteLimitedAsync(expected).ConfigureAwait(false).GetAwaiter().GetResult();
                                Assert.AreEqual(20L, limiter.GetCurrentResponseLength(httpContext));

                                response.WriteLimitedAsync(expected).ConfigureAwait(false).GetAwaiter().GetResult();
                            });
                        }
                        catch (ApplicationException e)
                        {
                            actual = e.Message;
                        }

                        Assert.AreEqual("Response length limit exceeded.", actual);
                    }
                );
            }
        }

        [TestMethod]
        public void Limiter_FRL_Greater()
        {
            Assert.Fail("Not implemented");
        }

        /* =========================================================================== TOOLS */

        private Stream CreateStream(string payLoad)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(payLoad));
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

        private IDisposable GetResetResponseLimiterSwindler()
        {
            return new Swindler<IResponseLimiter>(null,
                () => Providers.Instance.GetProvider<IResponseLimiter>(),
                (x) => Providers.Instance.SetProvider(typeof(IResponseLimiter), x)
            );
        }

    }
}
