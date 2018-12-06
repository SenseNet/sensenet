using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Services.Wopi
{
    public class GetFileRequest : WopiRequest
    {
        public string FileId { get; }
        public int? MaxExpectedSize { get; }
        public GetFileRequest(string fileId, int? maxExpectedSize) : base(WopiRequestType.GetFile)
        {
            FileId = fileId;
            MaxExpectedSize = maxExpectedSize;
        }
    }
}
