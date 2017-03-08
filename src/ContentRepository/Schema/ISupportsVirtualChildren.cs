using System.Security.Cryptography.X509Certificates;

namespace SenseNet.ContentRepository.Schema
{
    public interface ISupportsVirtualChildren
    {
        Content GetChild(string name);
    }
}