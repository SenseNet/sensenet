using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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
            throw new NotImplementedException(); //UNDONE: not implemented: ProcessRequest
        }

        /// <summary>
        /// Processes the WOPI web request. Designed for test purposes.
        /// </summary>
        /// <param name="context">An <see cref="HttpContext" /> object that provides references to the intrinsic server objects (for example, <see langword="Request" />, <see langword="Response" />, <see langword="Session" />, and <see langword="Server" />) used to service HTTP requests. </param>
        /// <param name="inputStream">Request stream containing the posted JSON object.</param>
        internal WopiResponse ProcessRequest(HttpContext context, Stream inputStream)
        {
            throw new NotImplementedException(); //UNDONE: not implemented: ProcessRequest
        }

    }
}
