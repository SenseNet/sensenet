using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Services.Wopi;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Tests
{
    [TestClass]
    public class WopiTests : TestBase
    {
        /* --------------------------------------------------------- GetLock */

        [TestMethod]
        public void Wopi_Req_GetLock()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "GET_LOCK"},
                        });
                    var req = CheckAndGetRequest<GetLockRequest>(pc, WopiRequestType.GetLock);
                    Assert.AreEqual("123", req.FileId);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_GetLock_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "GET_LOCK"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- Lock */

        [TestMethod]
        public void Wopi_Req_Lock()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "LOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                    var req = CheckAndGetRequest<LockRequest>(pc, WopiRequestType.Lock);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("LCK-42", req.Lock);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_Lock_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "LOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_Lock_MissingParam()
        {
            WopiErrorTest(HttpStatusCode.BadRequest, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "LOCK"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- RefreshLock */

        [TestMethod]
        public void Wopi_Req_RefreshLock()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "REFRESH_LOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                    var req = CheckAndGetRequest<RefreshLockRequest>(pc, WopiRequestType.RefreshLock);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("LCK-42", req.Lock);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_RefreshLock_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "REFRESH_LOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_RefreshLock_MissingParam()
        {
            WopiErrorTest(HttpStatusCode.BadRequest, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "REFRESH_LOCK"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- Unlock */

        [TestMethod]
        public void Wopi_Req_Unlock()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "UNLOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                    var req = CheckAndGetRequest<UnlockRequest>(pc, WopiRequestType.Unlock);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("LCK-42", req.Lock);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_Unlock_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "UNLOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_Unlock_MissingParam()
        {
            WopiErrorTest(HttpStatusCode.BadRequest, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "UNLOCK"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- UnlockAndRelock */


        [TestMethod]
        public void Wopi_Req_UnlockAndRelock()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "LOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                            new[] { "X-WOPI-OldLock", "LCK-41"},
                        });
                    var req = CheckAndGetRequest<UnlockAndRelockRequest>(pc, WopiRequestType.UnlockAndRelock);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("LCK-42", req.Lock);
                    Assert.AreEqual("LCK-41", req.OldLock);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_UnlockAndRelock_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "REFRESH_LOCK"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                            new[] { "X-WOPI-OldLock", "LCK-41"},
                        });
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_UnlockAndRelock_MissingParam()
        {
            WopiErrorTest(HttpStatusCode.BadRequest, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "REFRESH_LOCK"},
                            new[] { "X-WOPI-OldLock", "LCK-41"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- CheckFileInfo */

        [TestMethod]
        public void Wopi_Req_CheckFileInfo()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-SessionContext", "SessionContext-1"},
                        });
                    var req = CheckAndGetRequest<CheckFileInfoRequest>(pc, WopiRequestType.CheckFileInfo);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("SessionContext-1", req.SessionContext);

                    // TEST: without SessionContext
                    pc = CreatePortalContext("GET", "/wopi/files/124", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] {"Header1", "value1"},
                        });
                    req = CheckAndGetRequest<CheckFileInfoRequest>(pc, WopiRequestType.CheckFileInfo);
                    Assert.AreEqual("124", req.FileId);
                    Assert.IsNull(req.SessionContext);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_CheckFileInfo_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-SessionContext", "SessionContext-1"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- GetFile */

        [TestMethod]
        public void Wopi_Req_GetFile()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] {"X-WOPI-MaxExpectedSize", "9999"},
                        });
                    var req = CheckAndGetRequest<GetFileRequest>(pc, WopiRequestType.GetFile);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual(9999, req.MaxExpectedSize);

                    // TEST: without MaxExpectedSize
                    pc = CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] {"Header1", "value1"},
                        });
                    req = CheckAndGetRequest<GetFileRequest>(pc, WopiRequestType.GetFile);
                    Assert.IsNull(req.MaxExpectedSize);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_GetFile_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    CreatePortalContext("POST", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] {"X-WOPI-MaxExpectedSize", "9999"},
                        });
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_GetFile_InvalidMaxSize()
        {
            WopiErrorTest(HttpStatusCode.BadRequest, () =>
            {
                using (var output = new StringWriter())
                {
                    CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] {"X-WOPI-MaxExpectedSize", "nine-tausend"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- PutFile */

        [TestMethod]
        public void Wopi_Req_PutFile()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123/contents", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "PUT"},
                            new[] {"X-WOPI-Lock", "LCK-42"},
                        });
                    var req = CheckAndGetRequest<PutFileRequest>(pc, WopiRequestType.PutFile);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("LCK-42", req.Lock);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_PutFile_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "PUT"},
                            new[] {"X-WOPI-Lock ", "LCK-42"},
                        });
                }
            });
        }

        /* --------------------------------------------------------- PutRelativeFile */

        [TestMethod]
        public void Wopi_Req_PutRelativeFile()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    // suggested target
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "PUT_RELATIVE"},
                            new[] {"X-WOPI-SuggestedTarget", ".newextension"},
                            new[] {"X-WOPI-Size", "1234"},
                            new[] {"X-WOPI-FileConversion", ""},
                        });
                    var req = CheckAndGetRequest<PutRelativeFileRequest>(pc, WopiRequestType.PutRelativeFile);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual(".newextension", req.SuggestedTarget);
                    Assert.AreEqual(1234, req.Size);
                    Assert.AreEqual("", req.FileConversion);

                    // relative target
                    pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "PUT_RELATIVE"},
                            new[] {"X-WOPI-RelativeTarget", "newfilename.newextension"},
                            new[] {"X-WOPI-OverwriteRelativeTarget", "true"},
                            new[] {"X-WOPI-Size", "1234"},
                            new[] {"X-WOPI-FileConversion", ""},
                        });
                    req = CheckAndGetRequest<PutRelativeFileRequest>(pc, WopiRequestType.PutRelativeFile);
                    Assert.AreEqual("123", req.FileId);
                    Assert.AreEqual("newfilename.newextension", req.RelativeTarget);
                    Assert.AreEqual(true, req.OverwriteRelativeTarget);
                    Assert.AreEqual(1234, req.Size);
                    Assert.AreEqual("", req.FileConversion);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_PutRelativeFile_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "PUT_RELATIVE"},
                            new[] {"X-WOPI-SuggestedTarget", ".newextension"},
                            new[] {"X-WOPI-RelativeTarget", "newfilename.newextension"},
                            new[] {"X-WOPI-OverwriteRelativeTarget", "true"},
                            new[] {"X-WOPI-Size", "1234"},
                            new[] {"X-WOPI-FileConversion", ""},
                        });
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_PutRelativeFile_BadTarget()
        {
            WopiErrorTest(HttpStatusCode.BadRequest, () =>
            {
                using (var output = new StringWriter())
                {
                    // suggested target
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "PUT_RELATIVE"},
                            new[] {"X-WOPI-SuggestedTarget", ".newextension"},
                            new[] {"X-WOPI-RelativeTarget", "newfilename.newextension"},
                            new[] {"X-WOPI-OverwriteRelativeTarget", "true"},
                            new[] {"X-WOPI-Size", "1234"},
                            new[] {"X-WOPI-FileConversion", ""},
                        });
                }
            });
        }

        /* --------------------------------------------------------- DeleteFile */

        [TestMethod]
        public void Wopi_Req_DeleteFile()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "DELETE"},
                        });
                    var req = CheckAndGetRequest<DeleteFileRequest>(pc, WopiRequestType.DeleteFile);
                    Assert.AreEqual("123", req.FileId);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_DeleteFile_BadMethod()
        {
            WopiErrorTest(HttpStatusCode.MethodNotAllowed, () =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[]
                        {
                            new[] {"X-WOPI-Override", "DELETE"},
                        });
                }
            });
        }

        /* ======================================================================================= */

        /* --------------------------------------------------------- GetLock */

        /* --------------------------------------------------------- Lock */

        [TestMethod]
        public void Wopi_Proc_Lock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.Status);
                var actualLock = SharedLock.GetLock(file.Id);
                Assert.AreEqual(expectedLock, actualLock);
            });
        }
        //UNDONE: Missing test: Lock errors

        /* --------------------------------------------------------- RefreshLock */

        [TestMethod]
        public void Wopi_Proc_RefreshLock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, expectedLock);
                var dataProvider = (InMemoryDataProvider) DataProvider.Current;
                var sharedLockRow = dataProvider.DB.SharedLocks.First(x => x.ContentId == file.Id);
                sharedLockRow.CreationDate = DateTime.UtcNow.AddMinutes(-10.0d);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.Status);
                Assert.IsTrue((DateTime.UtcNow - sharedLockRow.CreationDate).TotalSeconds < 1);
            });
        }
        //UNDONE: Missing test: RefreshLock errors

        /* --------------------------------------------------------- Unlock */

        /* --------------------------------------------------------- UnlockAndRelock */

        /* --------------------------------------------------------- CheckFileInfo */

        /* --------------------------------------------------------- GetFile */

        [TestMethod]
        public void Wopi_Proc_GetFile()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "9999"},
                });

                Assert.AreEqual(HttpStatusCode.OK, response.Status);
                Assert.AreEqual("filecontent1", RepositoryTools.GetStreamString(response.GetResponseStream()));
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetFile_NotFound()
        {
            WopiTest(site =>
            {
                var response = WopiGet($"/wopi/files/{site.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "9999"},
                });

                Assert.AreEqual(HttpStatusCode.NotFound, response.Status);
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetFile_TooBig()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "3"}, // shorter than file content
                });

                Assert.AreEqual(HttpStatusCode.PreconditionFailed, response.Status);
            });
        }

        /* --------------------------------------------------------- PutFile */

        /* --------------------------------------------------------- PutRelativeFile */

        /* --------------------------------------------------------- DeleteFile */

        /* ======================================================================================= */

        private static readonly string DefaultAccessToken = "__DefaultAccessToken__";
        private static readonly string DefaultAccessTokenParameter = "access_token=" + DefaultAccessToken;

        private WopiResponse WopiGet(string resource, string queryString, string[][] headers)
        {
            return GetWopiResponse("GET", resource, queryString, headers, null);
        }
        private WopiResponse WopiPost(string resource, string queryString, string[][] headers, Stream requestStream)
        {
            return GetWopiResponse("POST", resource, queryString, headers, requestStream);
        }
        private WopiResponse GetWopiResponse(string httpMethod, string resource, string queryString, string[][] headers, Stream requestStream)
        {
            using (var output = new System.IO.StringWriter())
                return new WopiHandler().GetResponse(
                   CreatePortalContext(httpMethod, resource, queryString, output, headers));
        }

        private const string TestSiteName = "WopiTestSite";
        private static string TestSitePath => RepositoryPath.Combine("/Root/Sites", TestSiteName);
        private static Site CreateTestSite()
        {
            var sites = Node.Load<Folder>("/Root/Sites");
            if (sites == null)
            {
                sites = new Folder(Repository.Root, "Sites") {Name = "Sites"};
                sites.Save();
            }

            var site = new Site(sites) { Name = TestSiteName, UrlList = new Dictionary<string, string> { { "localhost", "None" } } };
            site.AllowChildType("File");
            site.Save();

            return site;
        }
        private File CreateTestFile(Node parent, string name, string fileContent)
        {
            var file = new File(parent) { Name = name ?? Guid.NewGuid().ToString() };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent ?? Guid.NewGuid().ToString()));
            file.Save();
            return file;
        }

        private static PortalContext CreatePortalContext(string httpMethod, string pagePath, string queryString, System.IO.TextWriter output, string[][] headers)
        {
            var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, queryString, output, "localhost", headers, httpMethod);
            var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
            HttpContext.Current = simulatedHttpContext;
            var portalContext = PortalContext.Create(simulatedHttpContext);
            return portalContext;
        }

        private T CheckAndGetRequest<T>(PortalContext pc, WopiRequestType requestType) where T : WopiRequest
        {
            Assert.IsTrue(pc.IsWopiRequest);
            var wopiReq = pc.WopiRequest;
            Assert.IsNotNull(wopiReq);
            Assert.AreEqual(requestType, wopiReq.RequestType);
            Assert.IsInstanceOfType(wopiReq, typeof(T));
            return (T)wopiReq;
        }

        /* =================================================== */

        private static RepositoryInstance _repository;

        [ClassInitialize]
        public static void InitializeRepositoryInstance(TestContext context)
        {
            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            _repository = Repository.Start(builder);
        }
        [ClassCleanup]
        public static void ShutDownRepository()
        {
            _repository?.Dispose();
        }

        private void WopiErrorTest(HttpStatusCode expectedStatusCode, Action callback)
        {
            try
            {
                WopiTest(callback);
                Assert.Fail("The expected exception was not thrown.");
            }
            catch (InvalidWopiRequestException e)
            {
                Assert.AreEqual(expectedStatusCode, e.StatusCode);
            }
        }

        private void WopiTest(Action callback)
        {
            WopiTestPrivate(callback, null);
        }
        private void WopiTest(Action<Site> callback)
        {
            WopiTestPrivate(null, callback);
        }

        private void WopiTestPrivate(Action callback1, Action<Site> callback2)
        {
            using (new SystemAccount())
            {
                SharedLock.RemoveAllLocks();
                var site = CreateTestSite();
                try
                {
                    callback1?.Invoke();
                    callback2?.Invoke(site);
                }
                finally
                {
                    site.ForceDelete();
                }
            }
        }
    }
}
