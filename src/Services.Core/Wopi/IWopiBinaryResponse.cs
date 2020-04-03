using System.IO;

namespace SenseNet.Services.Core.Wopi
{
    internal interface IWopiBinaryResponse
    {
        string FileName { get; }
        Stream GetResponseStream();
    }
}
