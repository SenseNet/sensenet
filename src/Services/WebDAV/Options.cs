
namespace SenseNet.Services.WebDav
{
	public class Options: IHttpMethod
	{
		private WebDavHandler _handler;
		public Options(WebDavHandler handler)
		{
			_handler = handler;
		}

		public void HandleMethod()
		{
			_handler.Context.Response.StatusCode = 200;
            _handler.Context.Response.AddHeader("MS-Author-Via", "MS-FP/4.0,DAV");
            _handler.Context.Response.AddHeader("DAV", "1,2");
            _handler.Context.Response.AddHeader("Accept-Ranges", "none");
            _handler.Context.Response.AddHeader("Cache-Control", "no-cache");
            _handler.Context.Response.AddHeader("Pragma", "no-cache");
            _handler.Context.Response.AddHeader("Expires", "Thu, 01 Jan 1970 00:00:00 GMT");
            _handler.Context.Response.AddHeader("Content-Length", "0");
            _handler.Context.Response.AddHeader("Allow", "GET, POST, OPTIONS, HEAD, MKCOL, PUT, PROPFIND, PROPPATCH, DELETE, MOVE, COPY, GETLIB, LOCK, UNLOCK");

            _handler.Context.Response.AddHeader("DocumentManagementServer", "Source Control;Version History;");
            _handler.Context.Response.AddHeader("X-MSDAVEXT", "1");
		}
	}
}
