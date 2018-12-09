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
    public class GetFileResponse : WopiResponse
    {
        [JsonIgnore]
        internal File File { get; set; }

        public Stream GetResponseStream()
        {
            if (File == null)
                return null;
            return File.Binary.GetStream();
        }
    }
}
