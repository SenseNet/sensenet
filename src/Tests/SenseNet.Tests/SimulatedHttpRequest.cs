using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.UI;

namespace SenseNet.Tests
{
	/// <summary>
	/// Used to simulate an HttpRequest.
	/// </summary>
	public class SimulatedHttpRequest : SimpleWorkerRequest
	{
	    private string _host;
	    private string[][] _headers;
	    private string _httpMethod;
	    private Stream _inputStream;
	    private byte[] _inputBuffer;

        /// <summary>
        /// Creates a new <see cref="SimulatedHttpRequest"/> instance.
        /// </summary>
        /// <param name="appVirtualDir">App virtual dir.</param>
        /// <param name="appPhysicalDir">App physical dir.</param>
        /// <param name="page">Page.</param>
        /// <param name="query">Query.</param>
        /// <param name="output">Output.</param>
        /// <param name="host">Host.</param>
        public SimulatedHttpRequest(string appVirtualDir, string appPhysicalDir, string page, string query, TextWriter output, string host)
			: base(appVirtualDir, appPhysicalDir, page, query, output)
		{
			if (host == null)
				throw new ArgumentNullException("host", "Host cannot be null nor empty.");
			if (host.Length == 0)
				throw new ArgumentException("Host cannot be empty.");
			_host = host;
		}
	    public SimulatedHttpRequest(string appVirtualDir, string appPhysicalDir, string page, string query, TextWriter output, string host, string[][] headers)
	        : this(appVirtualDir, appPhysicalDir, page, query, output, host)
	    {
	        _headers = AggregateHeaders(headers);
	    }
	    public SimulatedHttpRequest(string appVirtualDir, string appPhysicalDir, string page, string query, TextWriter output, string host, string[][] headers, string httpMethod)
	        : this(appVirtualDir, appPhysicalDir, page, query, output, host)
	    {
	        _headers = AggregateHeaders(headers);
            _httpMethod = httpMethod;
	    }
	    public SimulatedHttpRequest(string appVirtualDir, string appPhysicalDir, string page, string query, TextWriter output, string host, string[][] headers, string httpMethod, Stream inputStream)
	        : this(appVirtualDir, appPhysicalDir, page, query, output, host)
	    {
	        _headers = AggregateHeaders(headers);
	        _httpMethod = httpMethod;
	        _inputStream = inputStream;
	    }

        private string[][] AggregateHeaders(string[][] headers)
	    {
	        var list = new NameValueCollection();
	        foreach (var item in headers)
                list.Add(item[0], item[1]);
	        var result = new string[list.Count][];
	        for (int i = 0; i < list.Count; i++)
	            result[i] = new[] {list.AllKeys[i], list[i]};
	        return result;
	    }

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        /// <returns></returns>
        public override string GetServerName()
		{
			return _host;
		}

		/// <summary>
		/// Maps the path to a filesystem path.
		/// </summary>
		/// <param name="virtualPath">Virtual path.</param>
		/// <returns></returns>
		public override string MapPath(string virtualPath)
		{
			return Path.Combine(this.GetAppPath(), virtualPath);
		}


	    public override string GetUnknownRequestHeader(string name)
	    {
	        var items = _headers.Where(x => name.Equals(x[0], StringComparison.OrdinalIgnoreCase))
	            .Select(x => x[1]).ToArray();
	        return string.Join(", ", items);
	        var item = _headers.FirstOrDefault(x => name.Equals(x[0], StringComparison.OrdinalIgnoreCase));
	        return item?[1];
	    }
	    public override string[][] GetUnknownRequestHeaders()
	    {
	        return _headers;
	    }

	    public override string GetHttpVerbName()
	    {
	        return _httpMethod ?? "GET";
	    }


	    public override byte[] GetPreloadedEntityBody()
	    {
	        if (_inputStream == null)
	            return null;

	        if (_inputBuffer == null)
	        {
	            var length = Convert.ToInt32(_inputStream.Length);
	            var buffer = new byte[length];
	            _inputStream.Read(buffer, 0, length);
	            _inputBuffer = buffer;
	        }
            return _inputBuffer;
	    }

	}
}