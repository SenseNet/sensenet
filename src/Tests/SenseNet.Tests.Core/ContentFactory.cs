using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Tests.Core
{
    public class ContentFactory
    {
        public User CreateUserAndSave(string name)
        {
            var node = CreateUser(name);
            node.Save();
            return node;
        }
        public User CreateUser(string name)
        {
            return new User(Node.LoadNode("/Root/IMS/Public"))
            {
                Name = name,
                Email = $"{name}@example.com",
                Enabled = true
            };
        }
    }
}
