using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Workspaces;
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
                    return CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter, output,
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
                    return CreatePortalContext("POST", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
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
                    return CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
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
                    return CreatePortalContext("GET", "/wopi/files/123/contents", DefaultAccessTokenParameter, output,
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
                    return CreatePortalContext("GET", "/wopi/files/123", DefaultAccessTokenParameter,
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
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
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
            WopiErrorTest(HttpStatusCode.NotImplemented, () =>
            {
                using (var output = new StringWriter())
                    return CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[] {new[] {"X-WOPI-Override", "DELETE"}});
            });
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[] { new[] { "X-WOPI-Override", "DELETE" } });

                    var req = CheckAndGetRequest<BadRequest>(pc, WopiRequestType.NotDefined);
                    Assert.IsTrue(req.Exception is SnNotSupportedException);
                    Assert.AreEqual(HttpStatusCode.NotImplemented, req.StatusCode);
                }
            });
        }
        [TestMethod]
        public void Wopi_Req_RenameFile()
        {
            WopiTest(() =>
            {
                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("POST", "/wopi/files/123", DefaultAccessTokenParameter,
                        output,
                        new[] { new[] { "X-WOPI-Override", "RENAME_FILE" } });

                    var req = CheckAndGetRequest<BadRequest>(pc, WopiRequestType.NotDefined);
                    Assert.IsTrue(req.Exception is SnNotSupportedException);
                }
            });
        }

        /* ======================================================================================= */

        /* --------------------------------------------------------- GetLock */

        [TestMethod]
        public void Wopi_Proc_GetLock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "GET_LOCK"},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-Lock", existingLock);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(existingLock, actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetLock_Unlocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "GET_LOCK"},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-Lock", string.Empty);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.IsNull(actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetLock_InvalidId()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/abc-123", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "GET_LOCK"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetLock_NotFound()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/{site.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "GET_LOCK"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetLock_ExclusivelyLocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);
                file.CheckOut();

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "GET_LOCK"},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-Lock", existingLock);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(existingLock, actualLock);
            });
        }

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

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(expectedLock, actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Lock_ExistingSame()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, expectedLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(expectedLock, actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Lock_ExistingDifferent()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                var existingLock = "LCK_" + Guid.NewGuid();
                Assert.AreNotEqual(existingLock, expectedLock);
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "LockedByAnother");
                AssertHeader(response.Headers, "X-WOPI-Lock", existingLock);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(existingLock, actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Lock_InvalidId()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/abc-123", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Lock_NotFound()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/{site.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Lock_ExclusivelyLocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                file.CheckOut();

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "CheckedOut");
                AssertHeader(response.Headers, "X-WOPI-Lock", "");
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.IsNull(actualLock);
            });
        }

        /* --------------------------------------------------------- RefreshLock */

        [TestMethod]
        public void Wopi_Proc_RefreshLock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();

                SharedLock.Lock(file.Id, expectedLock, CancellationToken.None);

                SetSharedLockCreationDate(file.Id, DateTime.UtcNow.AddMinutes(-10.0d));
                
                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                var refreshedDate = GetSharedLockCreationDate(file.Id);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.IsTrue((DateTime.UtcNow - refreshedDate).TotalSeconds < 1);
            });
        }
        [TestMethod]
        public void Wopi_Proc_RefreshLock_MissingLock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "Unlocked");
                AssertHeader(response.Headers, "X-WOPI-Lock", string.Empty);
            });
        }
        [TestMethod]
        public void Wopi_Proc_RefreshLock_ExistingDifferent()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                var existingLock = "LCK_" + Guid.NewGuid();
                Assert.AreNotEqual(existingLock, expectedLock);
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "LockedByAnother");
                AssertHeader(response.Headers, "X-WOPI-Lock", existingLock);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(existingLock, actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_RefreshLock_InvalidId()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/abc-123", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_RefreshLock_NotFound()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/{site.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_RefreshLock_ExclusivelyLocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();

                SharedLock.Lock(file.Id, expectedLock, CancellationToken.None);
                file.CheckOut();

                SetSharedLockCreationDate(file.Id, DateTime.UtcNow.AddMinutes(-10.0d));

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "REFRESH_LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                var refreshedDate = GetSharedLockCreationDate(file.Id);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.IsTrue((DateTime.UtcNow - refreshedDate).TotalSeconds < 1);
            });
        }

        /* --------------------------------------------------------- Unlock */

        [TestMethod]
        public void Wopi_Proc_Unlock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "UNLOCK"},
                    new[] { "X-WOPI-Lock", existingLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.IsNull(actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Unlock_Unlocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "UNLOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "Unlocked");
                AssertHeader(response.Headers, "X-WOPI-Lock", string.Empty);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.IsNull(actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Unlock_ExistingDifferent()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                var existingLock = "LCK_" + Guid.NewGuid();
                Assert.AreNotEqual(existingLock, expectedLock);
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "UNLOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "LockedByAnother");
                AssertHeader(response.Headers, "X-WOPI-Lock", existingLock);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(actualLock, existingLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Unlock_InvalidId()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/abc-123", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "UNLOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Unlock_NotFound()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/{site.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "UNLOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_Unlock_ExclusivelyLocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);
                file.CheckOut();

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "UNLOCK"},
                    new[] { "X-WOPI-Lock", existingLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.IsNull(actualLock);
            });
        }

        /* --------------------------------------------------------- UnlockAndRelock */

        [TestMethod]
        public void Wopi_Proc_UnlockAndRelock()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                    new[] { "X-WOPI-OldLock", existingLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(expectedLock, actualLock);
            });
        }

        [TestMethod]
        public void Wopi_Proc_UnlockAndRelock_Unlocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                    new[] { "X-WOPI-OldLock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "Unlocked");
                AssertHeader(response.Headers, "X-WOPI-Lock", string.Empty);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.IsNull(actualLock);
            });
        }
        [TestMethod]
        public void Wopi_Proc_UnlockAndRelock_ExistingDifferent()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                    new[] { "X-WOPI-OldLock", "LCK-42"},
                }, null);

                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
                AssertHeader(response.Headers, "X-WOPI-LockFailureReason", "LockedByAnother");
                AssertHeader(response.Headers, "X-WOPI-Lock", existingLock);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(actualLock, existingLock);
            });
        }

        [TestMethod]
        public void Wopi_Proc_UnlockAndRelock_InvalidId()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/abd-123", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                    new[] { "X-WOPI-OldLock", "LCK-43"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_UnlockAndRelock_NotFound()
        {
            WopiTest(site =>
            {
                var response = WopiPost($"/wopi/files/{site.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", "LCK-42"},
                    new[] { "X-WOPI-OldLock", "LCK-43"},
                }, null);

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_UnlockAndRelock_ExclusivelyLocked()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var expectedLock = "LCK_" + Guid.NewGuid();
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);
                file.CheckOut();

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] { "X-WOPI-Override", "LOCK"},
                    new[] { "X-WOPI-Lock", expectedLock},
                    new[] { "X-WOPI-OldLock", existingLock},
                }, null);

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var actualLock = SharedLock.GetLock(file.Id, CancellationToken.None);
                Assert.AreEqual(expectedLock, actualLock);
            });
        }

        /* --------------------------------------------------------- CheckFileInfo */
        [TestMethod]
        public void Wopi_Proc_CheckFileInfo()
        {
            WopiTestWithAdmin(site =>
            {
                var mimeType = "text/plain";
                var fileContent = "filecontent1";
                var file = CreateTestFile(site, "File1.txt", fileContent, mimeType);

                var response = WopiGet($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] {"Header1", "Value1"},
                }) as CheckFileInfoResponse;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                AssertHeader(response.Headers, "ContentType", "application/json");

                Assert.AreEqual("File1.txt", response.BaseFileName);
                Assert.AreEqual("BuiltIn_Admin", response.OwnerId);
                Assert.AreEqual(fileContent.Length + 3, response.Size); // +UTF-8 BOM: 0xEF 0xBB 0xBF
                Assert.AreEqual("BuiltIn_Admin", response.UserId);
                Assert.AreEqual($"{file.Version}.{file.Binary.FileId}", response.Version);
                Assert.AreEqual(0, response.SupportedShareUrlTypes.Length);
                Assert.IsFalse(response.SupportsCobalt);
                Assert.IsFalse(response.SupportsContainers);
                Assert.IsFalse(response.SupportsDeleteFile);
                Assert.IsFalse(response.SupportsEcosystem);
                Assert.IsFalse(response.SupportsExtendedLockLength);
                Assert.IsFalse(response.SupportsFolders);
                Assert.IsFalse(response.SupportsGetFileWopiSrc);
                Assert.IsTrue(response.SupportsGetLock);
                Assert.IsTrue(response.SupportsLocks);
                Assert.IsFalse(response.SupportsRename);
                Assert.IsTrue(response.SupportsUpdate);
                Assert.IsFalse(response.SupportsUserInfo);
                Assert.IsFalse(response.IsAnonymousUser);
                Assert.IsFalse(response.IsEduUser);
                Assert.IsFalse(response.LicenseCheckForEditIsEnabled);
                Assert.AreEqual("Admin", response.UserFriendlyName);
                Assert.IsNull(response.UserInfo);

                if (WopiHandler.IsReadOnlyMode)
                    Assert.IsTrue(response.ReadOnly);
                else
                    Assert.IsFalse(response.ReadOnly);

                Assert.IsFalse(response.RestrictedWebViewOnly);
                Assert.IsTrue(response.UserCanAttend);
                Assert.IsTrue(response.UserCanNotWriteRelative);
                Assert.IsTrue(response.UserCanPresent);
                Assert.IsFalse(response.UserCanRename);

                if (WopiHandler.IsReadOnlyMode)
                    Assert.IsFalse(response.UserCanWrite);
                else
                    Assert.IsTrue(response.UserCanWrite);

                Assert.IsNull(response.CloseUrl);
                Assert.IsNull(response.DownloadUrl);
                Assert.IsNull(response.FileSharingUrl);
                Assert.IsNull(response.FileUrl);
                Assert.IsNull(response.FileVersionUrl);
                Assert.IsNull(response.HostEditUrl);
                Assert.IsNull(response.HostEmbeddedViewUrl);
                Assert.IsNull(response.HostViewUrl);
                Assert.IsNull(response.SignoutUrl);
                Assert.IsNull(response.BreadcrumbBrandName);
                Assert.IsNull(response.BreadcrumbBrandUrl);
                Assert.IsNull(response.BreadcrumbDocName);
                Assert.IsNull(response.BreadcrumbFolderName);
                Assert.IsNull(response.BreadcrumbFolderUrl);
                Assert.IsFalse(response.AllowAdditionalMicrosoftServices);
                Assert.IsFalse(response.AllowErrorReportPrompt);
                Assert.IsFalse(response.AllowExternalMarketplace);
                Assert.IsFalse(response.CloseButtonClosesWindow);
                Assert.IsFalse(response.DisablePrint);
                Assert.IsFalse(response.DisableTranslation);
                Assert.AreEqual(".txt", response.FileExtension);
                Assert.AreEqual(0, response.FileNameMaxLength);
                Assert.AreEqual(response.LastModifiedTime,
                    file.ModificationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                Assert.IsNull(response.SHA256);
                Assert.AreEqual(response.Version, response.UniqueContentId);
            });
        }

        /* --------------------------------------------------------- GetFile */

        [TestMethod]
        public void Wopi_Proc_GetFile()
        {
            WopiTest(site =>
            {
                var mimeType = "text/plain";
                var file = CreateTestFile(site, "File1.txt", "filecontent1", mimeType);

                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "9999"},
                }) as GetFileResponse;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                AssertHeader(response.Headers, "ContentType", mimeType);
                Assert.AreEqual("filecontent1", RepositoryTools.GetStreamString(response.GetResponseStream()));
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetFile_WithoutPrecondition()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");

                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"Header1", "Value-1"}
                });

                var getFileResponse = response as GetFileResponse;
                Assert.IsNotNull(getFileResponse);
                Assert.AreEqual(HttpStatusCode.OK, getFileResponse.StatusCode);
                Assert.AreEqual("filecontent1", RepositoryTools.GetStreamString(getFileResponse.GetResponseStream()));
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetFile_InvalidId()
        {
            WopiTest(site =>
            {
                var response = WopiGet($"/wopi/files/abc-123/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", "9999"},
                });

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
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

                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
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

                Assert.AreEqual(HttpStatusCode.PreconditionFailed, response.StatusCode);
            });
        }
        [TestMethod]
        public void Wopi_Proc_GetFile_GreaterThan2GB()
        {
            WopiTest(site =>
            {
                var size3GB = 3L * 1024L * 1024L * 1024L;
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var binaryAcc = new PrivateObject(file.Binary);
                binaryAcc.SetProperty("Size", size3GB);
                file.Save();
                file = Node.Load<File>(file.Id);
                // check prerequisit
                Assert.AreEqual(size3GB, file.Binary.Size);

                // ACTION
                var response = WopiGet($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-MaxExpectedSize", int.MaxValue.ToString()}, 
                });

                // ASSERT
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, response.StatusCode);
            });
        }

        /* --------------------------------------------------------- PutFile */

        [TestMethod]
        public void Wopi_Proc_PutFile()
        {
            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var existingLock = "LCK_" + Guid.NewGuid();
                SharedLock.Lock(file.Id, existingLock, CancellationToken.None);
                var newContent = "new filecontent2";

                var response = WopiPost($"/wopi/files/{file.Id}/contents", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-Override", "PUT"},
                    new[] {"X-WOPI-Lock", existingLock},
                }, RepositoryTools.GetStreamFromString(newContent));

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                file = Node.Load<File>(file.Id);
                //TODO:WOPI: Test X-WOPI-ItemVersion header: AssertHeader("X-WOPI-ItemVersion", ???);
                Assert.AreEqual(newContent, RepositoryTools.GetStreamString(file.Binary.GetStream()));
            });
        }

        /* --------------------------------------------------------- PutRelativeFile */

        //[TestMethod] /* temporarily inactivated */
        public void Wopi_Proc_PutRelativeFile_SuggestedFullname()
        {
            Assert.Inconclusive();

            WopiTest(site =>
            {
                var file = CreateTestFile(site, "File1.txt", "filecontent1");
                var newContent = "new filecontent2";

                var response = WopiPost($"/wopi/files/{file.Id}", DefaultAccessTokenParameter, new[]
                {
                    new[] {"X-WOPI-Override", "PUT_RELATIVE"},
                    new[] {"X-WOPI-SuggestedTarget", "File2.txt"},
                    new[] {"X-WOPI-Size", newContent.Length.ToString()},
                    new[] {"X-WOPI-FileConversion", ""},
                }, RepositoryTools.GetStreamFromString(newContent)) as PutRelativeFileResponse;

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            });
        }

        /* ======================================================================================= */

        [TestMethod]
        public void Wopi_Resp_CheckFileInfo()
        {
            WopiTestWithAdmin((site) =>
            {
                var fileContent = "filecontent1";
                var file = CreateTestFile(site, "File1.txt", fileContent, "test/plain");

                using (var output = new StringWriter())
                {
                    var pc = CreatePortalContext("GET", $"/wopi/files/{file.Id}", DefaultAccessTokenParameter, output,
                        new[]
                        {
                            new[] { "X-WOPI-SessionContext", "SessionContext-1"},
                        });

                    var handler = new WopiHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, true);

                    var resultstring = output.GetStringBuilder().ToString();
                    Assert.IsNotNull(resultstring);

                    var propertyNames = new[]
                    {
                        "BaseFileName", "OwnerId", "Size", "UserId", "Version", "SupportedShareUrlTypes",
                        "SupportsCobalt", "SupportsContainers", "SupportsDeleteFile", "SupportsEcosystem",
                        "SupportsExtendedLockLength", "SupportsFolders", "SupportsGetFileWopiSrc", "SupportsGetLock",
                        "SupportsLocks", "SupportsRename", "SupportsUpdate", "SupportsUserInfo", "IsAnonymousUser",
                        "IsEduUser", "LicenseCheckForEditIsEnabled", "UserFriendlyName", "UserInfo", "ReadOnly",
                        "RestrictedWebViewOnly", "UserCanAttend", "UserCanNotWriteRelative", "UserCanPresent",
                        "UserCanRename", "UserCanWrite", "CloseUrl", "DownloadUrl", "FileSharingUrl", "FileUrl",
                        "FileVersionUrl", "HostEditUrl", "HostEmbeddedViewUrl", "HostViewUrl", "SignoutUrl",
                        "BreadcrumbBrandName", "BreadcrumbBrandUrl", "BreadcrumbDocName", "BreadcrumbFolderName",
                        "BreadcrumbFolderUrl", "AllowAdditionalMicrosoftServices", "AllowErrorReportPrompt",
                        "AllowExternalMarketplace", "CloseButtonClosesWindow", "DisablePrint", "DisableTranslation",
                        "FileExtension", "FileNameMaxLength", "LastModifiedTime", "SHA256", "UniqueContentId",
                    };

                    foreach (var name in propertyNames)
                        if (!resultstring.Contains($"\"{name}\":"))
                            Assert.Fail($"Response does not contain \"{name}\" property.");

                }
            });
        }

        /* =============================================================================== Actions */

        [TestMethod]
        public void Wopi_Action_Open()
        {
            var parent = CreateTestContent("SystemFolder", Repository.Root).ContentHandler;
            var wopiConfig = new WopiDiscovery
            {
                Zones = new WopiDiscovery.WopiZoneCollection
                {
                    new WopiDiscovery.WopiZone("zone1")
                    {
                        Apps = new WopiDiscovery.WopiAppCollection
                        {
                            new WopiDiscovery.WopiApp("Word", null)
                            {
                                Actions = new WopiDiscovery.WopiActionCollection
                                {
                                    new WopiDiscovery.WopiAction("view", "docx", null, null),
                                    new WopiDiscovery.WopiAction("edit", "docx", null, null),
                                    new WopiDiscovery.WopiAction("view", "doc", null, null)
                                }
                            },
                            new WopiDiscovery.WopiApp("Excel", null)
                            {
                                Actions = new WopiDiscovery.WopiActionCollection
                                {
                                    new WopiDiscovery.WopiAction("view", "xlsx", null, null),
                                    new WopiDiscovery.WopiAction("edit", "xlsx", null, null)
                                }
                            }
                        }
                    }
                }
            };

            WopiDiscovery.AddInstance("test", wopiConfig);

            var file1 = CreateTestContent("File", parent, "File1");
            var file2 = CreateTestContent("File", parent, "File1.txt");
            var file3 = CreateTestContent("File", parent, "File1.docx");
            var file4 = CreateTestContent("File", parent, "File1.xlsx");
            var file5 = CreateTestContent("File", parent, "File1.doc");
            var folder = CreateTestContent("Folder", parent, "Folder1");
            
            void CreateAndAssertWopiAction(Content content, string actionType, bool visibleExpected, bool forbiddenExpected)
            {
                var (forbidden, visible) = WopiOpenAction.InitializeInternal(content, actionType, "test");

                Assert.AreEqual(visibleExpected, visible ?? true, content.Name);
                Assert.AreEqual(forbiddenExpected, forbidden ?? false, content.Name);
            }

            CreateAndAssertWopiAction(file1, "view", false, true);
            CreateAndAssertWopiAction(file2, "view", false, true);
            CreateAndAssertWopiAction(file3, "view", true, false);
            CreateAndAssertWopiAction(file4, "view", true, false);
            CreateAndAssertWopiAction(file5, "view", true, false);
            CreateAndAssertWopiAction(file5, "edit", false, true);
            CreateAndAssertWopiAction(folder, "view", false, true);
        }

        /* ======================================================================================= */

        private static readonly string DefaultAccessToken = "__DefaultAccessToken__";
        private static readonly string DefaultAccessTokenParameter = "access_token=" + DefaultAccessToken;

        private WopiResponse WopiGet(string resource, string queryString, string[][] headers)
        {
            return GetWopiResponse("GET", resource, queryString, headers, null);
        }
        private WopiResponse WopiPost(string resource, string queryString, string[][] headers, Stream inputStream)
        {
            return GetWopiResponse("POST", resource, queryString, headers, inputStream);
        }
        private WopiResponse GetWopiResponse(string httpMethod, string resource, string queryString, string[][] headers, Stream inputStream)
        {
            using (var output = new System.IO.StringWriter())
                return new WopiHandler().GetResponse(
                   CreatePortalContext(httpMethod, resource, queryString, output, headers, inputStream));
        }

        private const string TestSiteName = "WopiTestSite";
        private static string TestSitePath => RepositoryPath.Combine("/Root/Sites", TestSiteName);
        private static Workspace CreateTestSite()
        {
            var sites = Node.Load<Folder>("/Root/Sites");
            if (sites == null)
            {
                sites = new Folder(Repository.Root) {Name = "Sites"};
                sites.Save();
            }

            var site = new Workspace(sites) { Name = TestSiteName };
            site.AllowChildType("File");
            site.Save();

            return site;
        }
        private File CreateTestFile(Node parent, string name, string fileContent, string mimeType = "text/plain")
        {
            var file = new File(parent) { Name = name ?? Guid.NewGuid().ToString() };
            file.Binary.ContentType = mimeType;
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent ?? Guid.NewGuid().ToString()));
            file.Save();
            return file;
        }
        private Content CreateTestContent(string contentType, Node parent, string name = null)
        {
            if (contentType == "File")
                return Content.Create(CreateTestFile(parent, name ?? Guid.NewGuid().ToString(), "filecontent"));

            var content = Content.CreateNew(contentType, parent, name);
            content.Save();

            return content;
        }

        private static PortalContext CreatePortalContext(string httpMethod, string pagePath, string queryString, System.IO.TextWriter output, string[][] headers, Stream inputStream = null)
        {
            var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, queryString, output, "localhost", headers, httpMethod, inputStream);
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
            Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            builder.UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider());

            Indexing.IsOuterSearchEngineEnabled = true;

            _repository = Repository.Start(builder);

            using (new SystemAccount())
            {
                SecurityHandler.CreateAclEditor()
                    .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false, PermissionType.BuiltInPermissionTypes)
                    .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId, false, PermissionType.BuiltInPermissionTypes)
                    .Apply();
            }
        }
        [ClassCleanup]
        public static void ShutDownRepository()
        {
            _repository?.Dispose();
        }

        private void WopiErrorTest(HttpStatusCode expectedStatusCode, Func<PortalContext> callback)
        {
            WopiTest(() =>
            {
                var pc = callback();
                var req = CheckAndGetRequest<BadRequest>(pc, WopiRequestType.NotDefined);
                Assert.AreEqual(expectedStatusCode, req.StatusCode);
            });
        }
        private void WopiTest(Action callback)
        {
            using(new SystemAccount())
                WopiTestPrivate(callback, null);
        }
        private void WopiTest(Action<Workspace> callback)
        {
            using (new SystemAccount())
                WopiTestPrivate(null, callback);
        }
        private void WopiTestWithAdmin(Action<Workspace> callback)
        {
            WopiTestPrivate(null, callback);
        }
        private void WopiTestPrivate(Action callback1, Action<Workspace> callback2)
        {
            SharedLock.RemoveAllLocks(CancellationToken.None);
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

        private void AssertHeader(IDictionary<string, string> headers, string headerName, string expectedValue)
        {
            if (!headers.TryGetValue(headerName, out var actualValue))
                Assert.Fail("Header was not found: " + headerName);
            Assert.AreEqual(expectedValue, actualValue);
        }

        private ISharedLockDataProviderExtension GetDataProvider()
        {
            return DataStore.GetDataProviderExtension<ISharedLockDataProviderExtension>();
        }
        private void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            var provider = DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();
            if (!(provider is InMemoryTestingDataProvider))
                throw new PlatformNotSupportedException();

            provider.SetSharedLockCreationDate(nodeId, value);
        }
        private DateTime GetSharedLockCreationDate(int nodeId)
        {
            var provider = DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();
            if (!(provider is InMemoryTestingDataProvider))
                throw new PlatformNotSupportedException();

            return provider.GetSharedLockCreationDate(nodeId);
        }
    }
}
