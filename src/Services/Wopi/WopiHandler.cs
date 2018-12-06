using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Services.Wopi
{
    /// <summary>
    /// An <see cref="IHttpHandler"/> implementation to process the OData requests.
    /// </summary>
    public class WopiHandler : IHttpHandler
    {
        /// <inheritdoc select="summary" />
        /// <remarks>Returns with false in this implementation.</remarks>
        public bool IsReusable => false;

        /// <inheritdoc />
        /// <remarks>Processes the WOPI web request.</remarks>
        public void ProcessRequest(HttpContext context)
        {
            var portalContext = (PortalContext)context.Items[PortalContext.CONTEXT_ITEM_KEY];
            var response = GetResponse(portalContext);

            throw new NotImplementedException(); //UNDONE: not implemented: ProcessRequest
        }

        internal WopiResponse GetResponse(PortalContext portalContext)
        {
            var wopiReq = portalContext.WopiRequest;
            switch (wopiReq.RequestType)
            {
                case WopiRequestType.CheckFileInfo:
                case WopiRequestType.PutRelativeFile:
                case WopiRequestType.Lock:
                case WopiRequestType.Unlock:
                case WopiRequestType.RefreshLock:
                case WopiRequestType.UnlockAndRelock:
                case WopiRequestType.DeleteFile:
                case WopiRequestType.RenameFile:
                    throw new NotImplementedException(); //UNDONE: not implemented: GetResponse #1
                case WopiRequestType.GetFile:
                    return ProcessGetFileRequest((GetFileRequest)wopiReq, portalContext);
                case WopiRequestType.PutFile:
                case WopiRequestType.CheckContainerInfo:
                case WopiRequestType.CreateChildContainer:
                case WopiRequestType.CreateChildFile:
                case WopiRequestType.DeleteContainer:
                case WopiRequestType.EnumerateAncestors:
                case WopiRequestType.EnumerateChildren:
                case WopiRequestType.RenameContainer:
                case WopiRequestType.CheckEcosystem:
                case WopiRequestType.GetFileWopiSrc:
                case WopiRequestType.GetRootContainer:
                case WopiRequestType.Bootstrap:
                case WopiRequestType.GetNewAccessToken:
                    throw new NotImplementedException(); //UNDONE: not implemented: GetResponse #2
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private WopiResponse ProcessGetFileRequest(GetFileRequest wopiReq, PortalContext portalContext)
        {
            if (!int.TryParse(wopiReq.FileId, out var contentId))
                return new WopiResponse {Status = HttpStatusCode.NotFound};
            if (!(Node.LoadNode(contentId) is File file))
                return new WopiResponse {Status = HttpStatusCode.NotFound};
            if(!IsPreconditionOk(wopiReq, file))
                return new WopiResponse { Status = HttpStatusCode.PreconditionFailed };
            return new WopiResponse { Status = HttpStatusCode.OK, File = file };
        }
        private bool IsPreconditionOk(GetFileRequest wopiReq, File file)
        {
            if (wopiReq.MaxExpectedSize == null)
                return true;

            var bigLength = file.Binary.Size;
            if (bigLength > int.MaxValue)
                return false;

            var length = Convert.ToInt32(bigLength);
            return wopiReq.MaxExpectedSize.Value >= length;
        }
    }
}
