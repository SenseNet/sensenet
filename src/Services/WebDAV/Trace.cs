
namespace SenseNet.Services.WebDav
{
    public class Trace : IHttpMethod
    {
        private WebDavHandler _handler;
        public Trace(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            _handler.Context.Response.ContentType = "message/http";
            _handler.Context.Response.Write("TRACE / HTTP/1.1\r\n");
            _handler.Context.Response.Write("Accept: */*\r\n");
            _handler.Context.Response.Write("Host:");
            _handler.Context.Response.Write(_handler.Host);
            _handler.Context.Response.Write("\r\n");
        }

        #endregion
    }
}
