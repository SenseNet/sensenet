using System.IO;

namespace SenseNet.Services.Wopi
{
    public interface IWopiBinaryResponse
    {
        string FileName { get; }
        Stream GetResponseStream();
    }
}
