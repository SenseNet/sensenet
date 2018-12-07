using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Wopi
{
    internal class WopiResponse
    {
        public HttpStatusCode Status { get; internal set; }
        public IDictionary<string, string> Headers { get; internal set; }
        internal File File { get; set; }

        public Stream GetResponseStream()
        {
            if (File == null)
                return null;
            return File.Binary.GetStream();
        }
    }
}
