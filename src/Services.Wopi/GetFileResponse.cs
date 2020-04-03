using System.IO;
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
            //UNDONE: use the binary provider here
            return File?.Binary.GetStream();
        }
    }
}
