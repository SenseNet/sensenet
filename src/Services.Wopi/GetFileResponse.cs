using Newtonsoft.Json;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.Services.Wopi
{
    internal class GetFileResponse : WopiResponse, IWopiBinaryResponse
    {
        [JsonIgnore]
        public File File { get; set; }

        public string FileName => File.Name;
    }
}
