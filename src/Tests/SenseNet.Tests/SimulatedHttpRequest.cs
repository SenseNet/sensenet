using System;
using System.IO;
using System.Web.Hosting;

namespace SenseNet.Tests
{
	/// <summary>
	/// Used to simulate an HttpRequest.
	/// </summary>
	public class SimulatedHttpRequest : SimpleWorkerRequest
	{
	    private string _host;

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
	}
}