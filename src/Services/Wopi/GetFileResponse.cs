using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Wopi
{
    internal class GetFileResponse : WopiResponse, IWopiBinaryResponse
    {
        [JsonIgnore]
        internal File File { get; set; }

        public string FileName => File.Name;

        public Stream GetResponseStream()
        {
            return File?.Binary.GetStream();
        }
    }
}
