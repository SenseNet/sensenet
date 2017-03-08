using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;

namespace SenseNet.Services.WebDav
{
    public class UnLock : IHttpMethod
    {
        private WebDavHandler _handler;

        public UnLock(WebDavHandler handler)
        {
            _handler = handler;
        }

        public void HandleMethod()
        {
            if (Webdav.AutoCheckoutFiles)
            {
                var gc = Node.LoadNode(_handler.GlobalPath) as GenericContent;
                if (gc != null && gc.Locked && gc.LockedById == User.Current.Id)
                    gc.CheckIn();
            }

            _handler.Context.Response.Clear();
            _handler.Context.Response.StatusCode = 204;
        }
    }
}
