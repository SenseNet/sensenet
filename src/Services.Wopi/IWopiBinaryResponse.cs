using System.IO;

namespace SenseNet.Services.Wopi
{
    internal interface IWopiBinaryResponse
    {
        string FileName { get; }
        Stream GetResponseStream();
    }
}
