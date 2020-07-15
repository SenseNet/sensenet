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

        /* =========================================================================== TOOLS */

        private IDisposable GetResetResponseLimiterSwindler()
        {
            return new Swindler<IResponseLimiter>(null,
                () => Providers.Instance.GetProvider<IResponseLimiter>(),
                (x) => Providers.Instance.SetProvider(typeof(IResponseLimiter), x)
            );
        }

    }
}
